// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetroLog;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Services;

namespace SensorbergSDK.Internal
{
    /// <summary>
    /// Manages the layouts and encapsulates both retrieving fresh layouts from the web and
    /// caching them.
    /// </summary>
    public class LayoutManager : ILayoutManager
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<LayoutManager>();
        public const string KeyLayoutHeaders = "layout_headers";
        public const string KeyLayoutContent = "layout_content.cache"; // Cache file
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
        public async Task<bool> VerifyLayoutAsync(bool forceUpdate = false)
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
                        Logger.Debug("Layout changed.");
                    }
                    else
                    {
                        //TODO some thing should happen
                    }
                    LayoutValidityChanged?.Invoke(this, Layout != null);
                }
            }

            return IsLayoutValid;
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

        /// <summary>
        /// Executes the given request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The result state (success or failure).</returns>
        public async Task<RequestResultState> ExecuteRequestAsync(Request request)
        {
            Logger.Debug("LayoutManager.InternalExecuteRequestAsync(): Request ID is " + request.RequestId);
            RequestResultState resultState = RequestResultState.Failed;

            if (request.BeaconEventArgs != null && request.BeaconEventArgs.Beacon != null && await VerifyLayoutAsync(false))
            {
                request.ResolvedActions = Layout.GetResolvedActionsForPidAndEvent(request.BeaconEventArgs.Beacon.Pid, request.BeaconEventArgs.EventType);

                foreach (ResolvedAction resolvedAction in request.ResolvedActions)
                {
                    if (resolvedAction != null && resolvedAction.BeaconAction != null)
                    {
                        resolvedAction.BeaconAction.Id = request.RequestId;
                    }
                }

                resultState = RequestResultState.Success;
            }
            return resultState;
        }


        /// <summary>
        /// Creates a hash string based on the beacon ID1s in the given layout.
        /// </summary>
        /// <param name="layout">The layout containing the beacon ID1s.</param>
        /// <returns>A hash string of the beacon ID1s or null in case of an error.</returns>
        public static string CreateHashOfBeaconId1SInLayout(Layout layout)
        {

            if (layout != null)
            {
                IList<string> beaconId1S = layout.AccountBeaconId1S;

                if (beaconId1S.Count > 0)
                {
                    StringBuilder hash = new StringBuilder(beaconId1S[0]);

                    for (int i = 1; i < beaconId1S.Count; ++i)
                    {
                        var currentUuid = beaconId1S[i];

                        for (int j = 0; j < currentUuid.Length; ++j)
                        {
                            if (hash.Length < j + 1)
                            {
                                hash.Append(currentUuid[j]);
                            }
                            else
                            {
                                char combinationChar = (char) ((hash[j] + currentUuid[j])/2 + 1);

                                string hashToString = hash.ToString();
                                if (j == 0)
                                {
                                    hash = new StringBuilder(combinationChar);
                                    hash.Append(hashToString.Substring(j + 1));
                                }
                                else
                                {
                                    hash = new StringBuilder(hashToString.Substring(0, j));
                                    hash.Append(combinationChar);
                                    if (hash.Length > j + 1)
                                    {
                                        hash.Append(hashToString.Substring(j + 1));
                                    }
                                }
                            }
                        }
                    }
                    return hash.ToString();
                }
            }

            return null;
        }
    }
}
