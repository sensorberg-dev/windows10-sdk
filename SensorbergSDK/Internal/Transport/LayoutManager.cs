using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Internal.Utils;
using SensorbergSDK.Services;

namespace SensorbergSDK.Internal
{
    /// <summary>
    /// Manages the layouts and encapsulates both retrieving fresh layouts from the web and
    /// caching them.
    /// </summary>
    public class LayoutManager : ILayoutManager
    {
        public const string KeyLayoutHeaders = "layout_headers";
        private const string KeyLayoutContent = "layout_content.cache"; // Cache file
        public const string KeyLayoutRetrievedTime = "layout_retrieved_time";

        /// <summary>
        /// Fired, when the layout becomes valid/invalid.
        /// </summary>
        public event EventHandler<bool> LayoutValidityChanged;

        public Layout Layout { get; protected set; }

        /// <summary>
        /// Checks the layout validity.
        /// </summary>
        /// <returns>True, if layout is valid. False, if invalid.</returns>
        public bool IsLayoutValid
        {
            get
            {
                return Layout != null && Layout.ValidTill >= DateTimeOffset.Now;
            }
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
            if (forceUpdate || !IsLayoutValid)
            {
                if (!forceUpdate)
                {
                    // Check local storage first
                    Layout = await ServiceManager.StorageService.LoadLayoutFromLocalStorage();
                }

                if (forceUpdate || !IsLayoutValid)
                {
                    // Make sure that the existing layout (even if old) is not set to null in case
                    // we fail to load the fresh one from the web.
                    LayoutResult freshLayout = await ServiceManager.StorageService.RetrieveLayout();

                    if (freshLayout != null && freshLayout.Result == NetworkResult.Success)
                    {
                        Layout = freshLayout.Layout;
                        Debug.WriteLine("Layout changed.");
                    }
                    else
                    {
                        //TODO some thing should happen
                    }
                }
            }

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

        /// <summary>
        /// Invalidates both the current and cached layout.
        /// </summary>
        public async Task InvalidateLayout()
        {
            Layout = null;
            await ServiceManager.StorageService.InvalidateLayout();
        }

        public ResolvedAction GetAction(string actionId)
        {
            return Layout.ResolvedActions.FirstOrDefault(r => r.BeaconAction.Uuid == actionId);
        }

        internal async Task<RequestResultState> InternalExecuteRequestAsync(Request request)
        {
            System.Diagnostics.Debug.WriteLine("LayoutManager.InternalExecuteRequestAsync(): Request ID is " + request.RequestId);
            RequestResultState resultState = RequestResultState.Failed;

            if (request != null && request.BeaconEventArgs != null && request.BeaconEventArgs.Beacon != null)
            {
                if (await VerifyLayoutAsync(false))
                {
                    request.ResolvedActions = Layout.GetResolvedActionsForPidAndEvent(
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
    }
}
