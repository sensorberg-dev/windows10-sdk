// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using MetroLog;
using Newtonsoft.Json;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Utils;
using SensorbergSDK.Services;
using HttpClient = Windows.Web.Http.HttpClient;
using HttpMethod = Windows.Web.Http.HttpMethod;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
using HttpResponseMessage = Windows.Web.Http.HttpResponseMessage;

namespace SensorbergSDK.Internal.Services
{
    /// <summary>
    /// Implementation for the Sensorberg Cloud Backend.
    /// <see cref="IApiConnection"/>
    /// </summary>
    public class ApiConnection : IApiConnection
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<ApiConnection>();
        public SdkConfiguration Configuration { get; set; }
        public NetworkResult LastCallResult { get; set; }

        /// <summary>
        /// Sends a layout request to server and returns the HTTP response, if any.
        /// </summary>
        /// <param name="apiId">optional api id, overrides the given id by SDKData.</param>
        /// <returns>A HttpResponseMessage containing the server response or null in case of an error.</returns>
        public async Task<ResponseMessage> RetrieveLayoutResponse(string apiId)
        {
            Logger.Trace("RetrieveLayoutResponse");
            HttpRequestMessage requestMessage = new HttpRequestMessage();
            HttpBaseProtocolFilter baseProtocolFilter = new HttpBaseProtocolFilter();

            baseProtocolFilter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
            baseProtocolFilter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;

            requestMessage.Method = HttpMethod.Get;
            requestMessage.RequestUri = new Uri(Configuration.LayoutUri);

            HttpClient apiConnection = new HttpClient(baseProtocolFilter);
            apiConnection.DefaultRequestHeaders.Add(Constants.XApiKey, string.IsNullOrEmpty(apiId) ? Configuration.ApiKey : apiId);
            apiConnection.DefaultRequestHeaders.Add(Constants.Xiid, SdkData.DeviceId);
            string geoHash = await ServiceManager.LocationService.GetGeoHashedLocation();
            if (!string.IsNullOrEmpty(geoHash))
            {
                apiConnection.DefaultRequestHeaders.Add(Constants.Xgeo, geoHash);
            }
            apiConnection.DefaultRequestHeaders.Add(Constants.AdvertisementIdentifierHeader,
                string.IsNullOrEmpty(SdkData.UserId) ? Windows.System.UserProfile.AdvertisingManager.AdvertisingId : SdkData.UserId);
            HttpResponseMessage responseMessage;
            try
            {
                responseMessage = await apiConnection.SendRequestAsync(requestMessage);
            }
            catch (Exception ex)
            {
                LastCallResult = NetworkResult.NetworkError;
                System.Diagnostics.Debug.WriteLine("LayoutManager.RetrieveLayoutResponseAsync(): Failed to send HTTP request: " + ex.Message);
                return new ResponseMessage() {IsSuccess = false};
            }

            Logger.Trace("RetrieveLayoutResponse HTTPCode: {0}", responseMessage.StatusCode);
            if (responseMessage.IsSuccessStatusCode)
            {
                LastCallResult = NetworkResult.Success;
                return new ResponseMessage()
                {
                    Content = responseMessage.Content.ToString(),
                    Header = responseMessage.Headers.ToString(),
                    StatusCode = responseMessage.StatusCode,
                    IsSuccess = responseMessage.IsSuccessStatusCode
                };
            }
            LastCallResult = NetworkResult.UnknownError;

            return new ResponseMessage() {StatusCode = responseMessage.StatusCode, IsSuccess = responseMessage.IsSuccessStatusCode};
        }


        /// <summary>
        /// Load the Settings from the backend.
        /// </summary>
        /// <returns>Returns a JSON formated string.</returns>
        public async Task<string> LoadSettings()
        {
            try
            {
                Logger.Trace("LoadSettings");
                HttpRequestMessage requestMessage = new HttpRequestMessage();
                HttpBaseProtocolFilter baseProtocolFilter = new HttpBaseProtocolFilter();

                baseProtocolFilter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
                baseProtocolFilter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;

                requestMessage.Method = HttpMethod.Get;
                requestMessage.RequestUri = new Uri(string.Format(Configuration.SettingsUri, Configuration.ApiKey));

                HttpClient httpClient = new HttpClient(baseProtocolFilter);


                var responseMessage = await httpClient.SendRequestAsync(requestMessage);
                Logger.Trace("LoadSettings HTTPCode: {0}", responseMessage.StatusCode);
                LastCallResult = responseMessage.IsSuccessStatusCode ? NetworkResult.Success : NetworkResult.UnknownError;
                return responseMessage?.Content.ToString();
            }
            catch (Exception ex)
            {
                Logger.Error("LoadSettings Error", ex);
                LastCallResult = NetworkResult.NetworkError;
                return null;
            }
        }

        /// <summary>
        /// Sends the given History object to the cloud.
        /// </summary>
        /// <param name="history">History object to send.</param>
        public async Task<ResponseMessage> SendHistory(History history)
        {
            try
            {
                Logger.Trace("SendHistory");
                System.Net.Http.HttpClient apiConnection = new System.Net.Http.HttpClient();
                apiConnection.DefaultRequestHeaders.Add(Constants.XApiKey, Configuration.ApiKey);
                apiConnection.DefaultRequestHeaders.Add(Constants.Xiid, SdkData.DeviceId);
                apiConnection.DefaultRequestHeaders.Add(Constants.AdvertisementIdentifierHeader,
                    string.IsNullOrEmpty(Configuration.UserId) ? Windows.System.UserProfile.AdvertisingManager.AdvertisingId : Configuration.UserId);
                apiConnection.DefaultRequestHeaders.TryAddWithoutValidation(Constants.XUserAgent, UserAgentBuilder.BuildUserAgentJson());
                string serializeObject = JsonConvert.SerializeObject(history);
                var content = new StringContent(serializeObject, Encoding.UTF8, "application/json");

                System.Net.Http.HttpResponseMessage responseMessage = await apiConnection.PostAsync(new Uri(Configuration.LayoutUri), content);

                Logger.Trace("SendHistory HttpCode: {0}", responseMessage.StatusCode);
                if (responseMessage.IsSuccessStatusCode)
                {
                    LastCallResult = NetworkResult.Success;
                    return new ResponseMessage()
                    {
                        Content = responseMessage.Content.ToString(),
                        Header = responseMessage.Headers.ToString(),
                        StatusCode = Convert(responseMessage.StatusCode),
                        IsSuccess = responseMessage.IsSuccessStatusCode
                    };
                }
                LastCallResult = NetworkResult.UnknownError;
                return new ResponseMessage() {StatusCode = Convert(responseMessage.StatusCode), IsSuccess = responseMessage.IsSuccessStatusCode};
            }
            catch (Exception ex)
            {
                Logger.Error("SendHistory Error", ex);
                LastCallResult = NetworkResult.NetworkError;
                return new ResponseMessage() {IsSuccess = false};
            }
        }

        /// <summary>
        /// Helper to convert the status code to the other status code object.
        /// </summary>
        public static HttpStatusCode Convert(System.Net.HttpStatusCode code)
        {
            switch (code)
            {
                case System.Net.HttpStatusCode.Accepted:
                    return HttpStatusCode.Accepted;
                case System.Net.HttpStatusCode.BadGateway:
                    return HttpStatusCode.BadGateway;
                case System.Net.HttpStatusCode.BadRequest:
                    return HttpStatusCode.BadRequest;
                case System.Net.HttpStatusCode.Conflict:
                    return HttpStatusCode.Conflict;
                case System.Net.HttpStatusCode.Continue:
                    return HttpStatusCode.Continue;
                case System.Net.HttpStatusCode.Created:
                    return HttpStatusCode.Created;
                case System.Net.HttpStatusCode.ExpectationFailed:
                    return HttpStatusCode.ExpectationFailed;
                case System.Net.HttpStatusCode.Forbidden:
                    return HttpStatusCode.Forbidden;
                case System.Net.HttpStatusCode.Found:
                    return HttpStatusCode.Found;
                case System.Net.HttpStatusCode.GatewayTimeout:
                    return HttpStatusCode.GatewayTimeout;
                case System.Net.HttpStatusCode.Gone:
                    return HttpStatusCode.Gone;
                case System.Net.HttpStatusCode.HttpVersionNotSupported:
                    return HttpStatusCode.HttpVersionNotSupported;
                case System.Net.HttpStatusCode.InternalServerError:
                    return HttpStatusCode.InternalServerError;
                case System.Net.HttpStatusCode.LengthRequired:
                    return HttpStatusCode.LengthRequired;
                case System.Net.HttpStatusCode.MethodNotAllowed:
                    return HttpStatusCode.MethodNotAllowed;
                case System.Net.HttpStatusCode.Moved:
                    return HttpStatusCode.MovedPermanently;
                case System.Net.HttpStatusCode.NoContent:
                    return HttpStatusCode.NoContent;
                case System.Net.HttpStatusCode.NonAuthoritativeInformation:
                    return HttpStatusCode.NonAuthoritativeInformation;
                case System.Net.HttpStatusCode.NotAcceptable:
                    return HttpStatusCode.NotAcceptable;
                case System.Net.HttpStatusCode.NotFound:
                    return HttpStatusCode.NotFound;
                case System.Net.HttpStatusCode.NotImplemented:
                    return HttpStatusCode.NotImplemented;
                case System.Net.HttpStatusCode.NotModified:
                    return HttpStatusCode.NotModified;
                case System.Net.HttpStatusCode.OK:
                    return HttpStatusCode.Ok;
                case System.Net.HttpStatusCode.PartialContent:
                    return HttpStatusCode.PartialContent;
                case System.Net.HttpStatusCode.PaymentRequired:
                    return HttpStatusCode.PaymentRequired;
                case System.Net.HttpStatusCode.PreconditionFailed:
                    return HttpStatusCode.PreconditionFailed;
                case System.Net.HttpStatusCode.ProxyAuthenticationRequired:
                    return HttpStatusCode.ProxyAuthenticationRequired;
                case System.Net.HttpStatusCode.RedirectKeepVerb:
                    return HttpStatusCode.TemporaryRedirect;
                case System.Net.HttpStatusCode.RedirectMethod:
                    return HttpStatusCode.PermanentRedirect;
                case System.Net.HttpStatusCode.RequestedRangeNotSatisfiable:
                    return HttpStatusCode.RequestedRangeNotSatisfiable;
                case System.Net.HttpStatusCode.RequestEntityTooLarge:
                    return HttpStatusCode.RequestEntityTooLarge;
                case System.Net.HttpStatusCode.RequestTimeout:
                    return HttpStatusCode.RequestTimeout;
                case System.Net.HttpStatusCode.RequestUriTooLong:
                    return HttpStatusCode.RequestUriTooLong;
                case System.Net.HttpStatusCode.ResetContent:
                    return HttpStatusCode.ResetContent;
                case System.Net.HttpStatusCode.ServiceUnavailable:
                    return HttpStatusCode.ServiceUnavailable;
                case System.Net.HttpStatusCode.SwitchingProtocols:
                    return HttpStatusCode.SwitchingProtocols;
                case System.Net.HttpStatusCode.Unauthorized:
                    return HttpStatusCode.Unauthorized;
                case System.Net.HttpStatusCode.UnsupportedMediaType:
                    return HttpStatusCode.UnsupportedMediaType;
                case System.Net.HttpStatusCode.UpgradeRequired:
                    return HttpStatusCode.UpgradeRequired;
                case System.Net.HttpStatusCode.UseProxy:
                    return HttpStatusCode.UseProxy;
                default:
                    return HttpStatusCode.BadRequest;
            }
        }
    }
}