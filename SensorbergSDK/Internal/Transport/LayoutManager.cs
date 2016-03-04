using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDK.Internal
{
    /// <summary>
    /// Manages the layouts and encapsulates both retrieving fresh layouts from the web and
    /// caching them.
    /// </summary>
    public sealed class LayoutManager
    {
        private const string KeyLayoutHeaders = "layout_headers";
        private const string KeyLayoutContent = "layout_content.cache"; // Cache file
        private const string KeyLayoutRetrievedTime = "layout_retrieved_time";

        /// <summary>
        /// Fired, when the layout becomes valid/invalid.
        /// </summary>
        public event EventHandler<bool> LayoutValidityChanged;

        public Layout Layout
        {
            get { return _layout; }
        }

        private SDKData _dataContext;
        private Layout _layout;
        private ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        private bool _isLayoutValid;
        public bool IsLayoutValid
        {
            get
            {
                return _isLayoutValid;
            }
            private set
            {
                if (_isLayoutValid != value)
                {
                    _isLayoutValid = value;

                    if (LayoutValidityChanged != null)
                    {
                        LayoutValidityChanged(this, _isLayoutValid);
                    }
                }
            }
        }

        private static LayoutManager _instance;
        public static LayoutManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LayoutManager();
                }

                return _instance;
            }
        }

        private LayoutManager()
        {
            _dataContext = SDKData.Instance;
        }

        /// <summary>
        /// Makes sure the layout is up-to-date.
        /// </summary>
        /// <param name="forceUpdate">If true, will update the layout even if valid.</param>
        /// <returns>True, if layout is valid (or was updated), false otherwise.</returns>
        public IAsyncOperation<bool> VerifyLayoutAsync(bool forceUpdate)
        {
            return InternalVerifyLayoutAsync(forceUpdate).AsAsyncOperation<bool>();
        }

        private async Task<bool> InternalVerifyLayoutAsync(bool forceUpdate)
        {
            if (forceUpdate || !CheckLayoutValidity())
            {
                if (!forceUpdate)
                {
                    // Check local storage first
                    _layout = await LoadLayoutFromLocalStorageAsync();
                }

                if (forceUpdate || !CheckLayoutValidity())
                {
                    // Make sure that the existing layout (even if old) is not set to null in case
                    // we fail to load the fresh one from the web.
                    Layout freshLayout = await RetrieveLayoutAsync();

                    if (freshLayout != null)
                    {
                        _layout = freshLayout;
                        Debug.WriteLine("Layout changed.");
                    }
                }
            }

            return CheckLayoutValidity();
        }

        /// <summary>
        /// Invalidates both the current and cached layout.
        /// </summary>
        public IAsyncAction InvalidateLayoutAsync()
        {
            Func<Task> action = async () =>
            {
                _layout = null;
                _localSettings.Values[KeyLayoutHeaders] = null;
                _localSettings.Values[KeyLayoutRetrievedTime] = null;

                try
                {
                    var contentFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(KeyLayoutContent);

                    if (contentFile != null)
                    {
                        await contentFile.DeleteAsync();
                    }
                }
                catch (Exception)
                {
                }
            };

            return action().AsAsyncAction();
        }

        /// <summary>
        /// Checks the layout validity.
        /// </summary>
        /// <returns>True, if layout is valid. False, if invalid.</returns>
        public bool CheckLayoutValidity()
        {
            IsLayoutValid = (_layout != null && _layout.ValidTill >= DateTimeOffset.Now);
            return IsLayoutValid;
        }

        /// <summary>
        /// Executes the given request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The result state (success or failure).</returns>
        public IAsyncOperation<RequestResultState> ExecuteRequestAsync(Request request)
        {
            IAsyncOperation<RequestResultState> resultState = InternalExecuteRequestAsync(request).AsAsyncOperation<RequestResultState>();
            return resultState;
        }

        internal async Task<RequestResultState> InternalExecuteRequestAsync(Request request)
        {
            System.Diagnostics.Debug.WriteLine("LayoutManager.InternalExecuteRequestAsync(): Request ID is " + request.RequestId);
            RequestResultState resultState = RequestResultState.Failed;

            if (request != null && request.BeaconEventArgs != null && request.BeaconEventArgs.Beacon != null)
            {
                if (await VerifyLayoutAsync(false))
                {
                    request.ResolvedActions = _layout.GetResolvedActionsForPidAndEvent(
                        request.BeaconEventArgs.Beacon.Pid, request.BeaconEventArgs.EventType);

                    foreach (ResolvedAction resolvedAction in request.ResolvedActions)
                    {
                        if (resolvedAction != null && resolvedAction.BeaconAction != null)
                        {
                            resolvedAction.BeaconAction.Id = request.RequestId;
                        }
                    }

                    resultState = RequestResultState.Success;
                }
            }

            return resultState;
        }

//        /// <summary>
//        /// Sends a layout request to server and returns the HTTP response, if any.
//        /// </summary>
//        /// <param name="ApiKey">The API key.</param>
//        /// <returns>A HttpResponseMessage containing the server response or null in case of an error.</returns>
//        public async Task<HttpResponseMessage> RetrieveLayoutResponseAsync(string apiKey)
//        {
//            HttpRequestMessage requestMessage = new HttpRequestMessage();
//            HttpBaseProtocolFilter baseProtocolFilter = new HttpBaseProtocolFilter();
//
//            baseProtocolFilter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
//            baseProtocolFilter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;
//
//            requestMessage.Method = HttpMethod.Get;
//            requestMessage.RequestUri = new Uri(Constants.LayoutApiUriAsString);
//
//            IApiConnection apiConnection = InstanceManager.newHttpClient(baseProtocolFilter);
//            apiConnection.DefaultRequestHeaders.Add(Constants.XApiKey, apiKey);
//            apiConnection.DefaultRequestHeaders.Add(Constants.Xiid, _dataContext.DeviceId);
//            HttpResponseMessage responseMessage = null;
//
//            try
//            {
//                responseMessage = await apiConnection.SendRequestAsync(requestMessage);
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine("LayoutManager.RetrieveLayoutResponseAsync(): Failed to send HTTP request: " + ex.Message);
//            }
//
//            return responseMessage;
//        }

            /// <summary>
            /// Creates a hash string based on the beacon ID1s in the given layout.
            /// </summary>
            /// <param name="layout">The layout containing the beacon ID1s.</param>
            /// <returns>A hash string of the beacon ID1s or null in case of an error.</returns>
        public static string CreateHashOfBeaconId1sInLayout(Layout layout)
        {
            string hash = null;

            if (layout != null)
            {
                IList<string> beaconId1s = layout.AccountBeaconId1s;

                if (beaconId1s.Count > 0)
                {
                    hash = beaconId1s[0];
                    string currentUuid = string.Empty;

                    for (int i = 1; i < beaconId1s.Count; ++i)
                    {
                        currentUuid = beaconId1s[i];

                        for (int j = 0; j < currentUuid.Length; ++j)
                        {
                            if (hash.Length < j + 1)
                            {
                                hash += currentUuid[j];
                            }
                            else
                            {
                                char combinationChar = (char)(((int)hash[j] + (int)currentUuid[j]) / 2 + 1);

                                if (j == 0)
                                {
                                    hash = combinationChar + hash.Substring(j + 1);
                                }
                                else if (hash.Length > j + 1)
                                {
                                    hash = hash.Substring(0, j) + combinationChar + hash.Substring(j + 1);
                                }
                                else
                                {
                                    hash = hash.Substring(0, j) + combinationChar;
                                }
                            }
                        }
                    }
                }
            }

            return hash;
        }

        /// <summary>
        /// Retrieves the layout from the web.
        /// </summary>
        /// <returns></returns>
        private async Task<Layout> RetrieveLayoutAsync()
        {
            Layout layout = null;
            HttpResponseMessage responseMessage = await SDKManager.InternalInstance.ServiceManager.ApiConnction.RetrieveLayoutResponseAsync(_dataContext);

            if (responseMessage != null && responseMessage.IsSuccessStatusCode)
            {
                string headersAsString = StripLineBreaksAndExcessWhitespaces(responseMessage.Headers.ToString());
                string contentAsString = StripLineBreaksAndExcessWhitespaces(responseMessage.Content.ToString());
                contentAsString = EnsureEncodingIsUTF8(contentAsString);
                DateTimeOffset layoutRetrievedTime = DateTimeOffset.Now;

                if (contentAsString.Length > Constants.MinimumLayoutContentLength)
                {
                    JsonValue content = null;

                    try
                    {
                        content = JsonValue.Parse(contentAsString);
                        layout = Layout.FromJson(headersAsString, content.GetObject(), layoutRetrievedTime);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("LayoutManager.RetrieveLayoutAsync(): Failed to parse layout: " + ex.ToString());
                        layout = null;
                    }
                }

                if (layout != null)
                {
                    // Store the parsed layout
                    await SaveLayoutToLocalStorageAsync(headersAsString, contentAsString, layoutRetrievedTime);
                }
            }

            return layout;
        }

        /// <summary>
        /// Tries to load the layout from the local storage.
        /// </summary>
        /// <returns>A layout instance, if successful. Null, if not found.</returns>
        private async Task<Layout> LoadLayoutFromLocalStorageAsync()
        {
            Layout layout = null;
            string headers = string.Empty;
            string content = string.Empty;
            DateTimeOffset layoutRetrievedTime = DateTimeOffset.Now;

            if (_localSettings.Values.ContainsKey(KeyLayoutHeaders))
            {
                headers = _localSettings.Values[KeyLayoutHeaders].ToString();
            }

            if (_localSettings.Values.ContainsKey(KeyLayoutRetrievedTime))
            {
                layoutRetrievedTime = (DateTimeOffset)_localSettings.Values[KeyLayoutRetrievedTime];
            }

            try
            {
                var contentFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(KeyLayoutContent);

                if (contentFile != null)
                {
                    content = await FileIO.ReadTextAsync(contentFile as IStorageFile);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LayoutManager.LoadLayoutFromLocalStorage(): Failed to load content: " + ex.ToString());
            }

            if (!string.IsNullOrEmpty(content))
            {
                content = EnsureEncodingIsUTF8(content);
                try
                {
                    JsonValue contentAsJsonValue = JsonValue.Parse(content);
                    layout = Layout.FromJson(headers, contentAsJsonValue.GetObject(), layoutRetrievedTime);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("LayoutManager.LoadLayoutFromLocalStorage(): Failed to parse layout: " + ex.ToString());
                }
            }

            if (layout == null)
            {
                // Failed to parse the layout => invalidate it
                await InvalidateLayoutAsync();
            }

            return layout;
        }

        /// <summary>
        /// Saves the strings that make up a layout.
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="content"></param>
        /// <param name="layoutRetrievedTime"></param>
        private async Task SaveLayoutToLocalStorageAsync(string headers, string content, DateTimeOffset layoutRetrievedTime)
        {
            if (await StoreDataAsync(KeyLayoutContent, content))
            {
                _localSettings.Values[KeyLayoutHeaders] = headers;
                _localSettings.Values[KeyLayoutRetrievedTime] = layoutRetrievedTime;
            }
        }

        /// <summary>
        /// Saves the given data to the specified file.
        /// </summary>
        /// <param name="fileName">The file name of the storage file.</param>
        /// <param name="data">The data to save.</param>
        /// <returns>True, if successful. False otherwise.</returns>
        private async Task<bool> StoreDataAsync(string fileName, string data)
        {
            bool success = false;

            try
            {
                var storageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.AppendTextAsync(storageFile, data);
                success = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LayoutManager.StoreDataAsync(): Failed to save content: " + ex.ToString());
            }

            return success;
        }

        private string EnsureEncodingIsUTF8(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        private string StripLineBreaksAndExcessWhitespaces(string str)
        {
            string stripped = str.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
            stripped = Regex.Replace(stripped, @" +", " ");
            stripped = stripped.Trim();
            return stripped;
        }
    }
}
