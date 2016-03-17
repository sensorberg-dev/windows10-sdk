﻿using System;
using System.Collections.Generic;
using System.ServiceModel;
using Windows.Web.Http.Headers;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDK
{
    public enum ApiKeyValidationResult
    {
        Valid,
        Invalid,
        NetworkError,
        UnknownError
    };

    public enum NetworkResult
    {
        Success,
        NetworkError,
        AuthenticationFailed,
        ParsingError,
        NoWindowsCampains,
        UnknownError
    };

    public class ApiKeyHelper
    {
        private const string LoginUrl = "https://connect.sensorberg.com/api/user/login";
        private const string ApplicationsUrl = "https://connect.sensorberg.com/api/applications/";
        private const string KeyEmail = "email";
        private const string KeyPassword = "password";
        private const string KeyResponse = "response";
        private const string KeyAuthorizationToken = "authToken";
        private const string KeyApiKey = "apiKey";
        private const string KeyName = "name";
        private const string KeyPlatform = "platform";
        private const string PlatformValueWindows = "windows";

        public string ApiKey
        {
            get;
            set;
        }

        public string ApplicationName
        {
            get;
            set;
        }

        /// <summary>
        /// Checks whether the given API key is valid or not.
        /// </summary>
        /// <param name="apiKey">The API key to validate.</param>
        /// <returns>The validation result.</returns>
        public async Task<ApiKeyValidationResult> ValidateApiKey(string apiKey)
        {
            return await ServiceManager.StorageService.ValidateApiKey(apiKey);
        }

        /// <summary>
        /// Tries to fetch the API key from the server matching the given credentials.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns>The fetching operation result. If successful, the API key is placed in the ApiKey property of this class.</returns>
        public async Task<NetworkResult> FetchApiKeyAsync(string email, string password)
        {
            NetworkResult result = NetworkResult.UnknownError;

            HttpBaseProtocolFilter httpBaseProtocolFilter = new HttpBaseProtocolFilter();
            httpBaseProtocolFilter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
            httpBaseProtocolFilter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;
            HttpClient client = new HttpClient(httpBaseProtocolFilter);

            var keyValues = new List<KeyValuePair<string, string>>();
            keyValues.Add(new KeyValuePair<string, string>(KeyEmail, email));
            keyValues.Add(new KeyValuePair<string, string>(KeyPassword, password));
            HttpFormUrlEncodedContent formContent = new HttpFormUrlEncodedContent(keyValues);

            Uri uri = new Uri(LoginUrl);
            HttpResponseMessage response = null;

            try
            {
                response = await client.PostAsync(uri, formContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ApiKeyHelper.FetchApiKeyAsync(): Network error: " + ex.Message);
                return NetworkResult.NetworkError;
            }

            if (response.StatusCode == HttpStatusCode.Ok)
            {
                string responseAsString = string.Empty;

                try
                {
                    responseAsString = await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ApiKeyHelper.FetchApiKeyAsync(): Network error: " + ex.Message);
                    return NetworkResult.NetworkError;
                }

                JsonValue responseAsJsonValue = null;
                string authToken = string.Empty;

                try
                {
                    responseAsJsonValue = JsonValue.Parse(responseAsString);
                    authToken = string.Empty;

                    if (responseAsJsonValue.ValueType == JsonValueType.Object)
                    {
                        var jsonObject = responseAsJsonValue.GetObject();
                        var responseObject = jsonObject.GetNamedObject(KeyResponse);
                        authToken = responseObject.GetNamedString(KeyAuthorizationToken);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ApiKeyHelper.FetchApiKeyAsync(): Parsing error: " + ex.Message);
                    return NetworkResult.ParsingError;
                }

                if (!string.IsNullOrEmpty(authToken))
                {
                    client = new HttpClient(httpBaseProtocolFilter);
                    uri = new Uri(ApplicationsUrl);
                    client.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue(authToken);

                    try
                    {
                        responseAsString = await client.GetStringAsync(uri);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("ApiKeyHelper.FetchApiKeyAsync(): Network error: " + ex.Message);
                        return NetworkResult.NetworkError;
                    }

                    responseAsJsonValue = JsonValue.Parse(responseAsString);
                    result = NetworkResult.NoWindowsCampains;

                    if (responseAsJsonValue.ValueType == JsonValueType.Array)
                    {
                        var applicationsArray = responseAsJsonValue.GetArray();

                        // We take the first Windows application from the list
                        foreach (JsonValue applicationValue in applicationsArray)
                        {
                            if (applicationValue.ValueType == JsonValueType.Object)
                            {
                                JsonObject applicationObject = applicationValue.GetObject();
                                string apiKey = string.Empty;

                                var apiKeyValue = applicationObject[KeyApiKey];
                                if (apiKeyValue.ValueType == JsonValueType.Null)
                                {
                                    continue;
                                }

                                apiKey = applicationObject.GetNamedString(KeyApiKey);

                                string applicationName = applicationObject.GetNamedString(KeyName);
                                string platform = applicationObject.GetNamedString(KeyPlatform);

                                if (platform.ToLower().Equals(PlatformValueWindows.ToLower()))
                                {
                                    ApiKey = apiKey;
                                    ApplicationName = applicationName;
                                    result = NetworkResult.Success;
                                    break;
                                }
                            }
                        }
                    }
                }
                else // if (!string.IsNullOrEmpty(authToken)) - else
                {
                    result = NetworkResult.AuthenticationFailed;
                }
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                result = NetworkResult.AuthenticationFailed;
            }
            else
            {
                result = NetworkResult.NetworkError;
            }

            return result;
        }
    }
}
