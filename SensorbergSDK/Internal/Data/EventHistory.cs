using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Internal.Utils;
using SensorbergSDK.Services;

namespace SensorbergSDK.Internal
{
    /// <summary>
    /// Event storage. It stores all past beacon events and actions associated with the events.
    /// </summary>
    public sealed class EventHistory
    {
        private AutoResetEvent _asyncWaiter;

        public EventHistory()
        {
            _asyncWaiter = new AutoResetEvent(true);
        }

        /// <summary>
        /// If sendOnlyOnce is true for resolved action, fuction will check from the history if the
        /// action is already presented for the user.
        /// </summary>
        /// <param name="resolvedAction"></param>
        /// <returns>True ,if action type is SendOnlyOnce, and it has been shown already. Otherwise false.</returns>
        public async Task<bool> CheckSendOnlyOnceAsync(ResolvedAction resolvedAction)
        {
            bool sendonlyOnce = false;

            if (resolvedAction.SendOnlyOnce)
            {
                try
                {
                    _asyncWaiter.WaitOne();
                    DBHistoryAction dbHistoryAction = await ServiceManager.StorageService.GetAction(resolvedAction.BeaconAction.Uuid);

                    if (dbHistoryAction != null)
                    {
                        sendonlyOnce = true;
                    }

                }
                finally
                {
                    _asyncWaiter.Set();
                }
            }

            return sendonlyOnce;
        }

        /// <summary>
        /// If supressionTime is set for the action, fuction will check from the history if the
        /// action is already presented during the supression time.
        /// </summary>
        /// <param name="resolvedAction"></param>
        /// <returns>True only if action should be supressed.</returns>
        public async Task<bool> ShouldSupressAsync(ResolvedAction resolvedAction)
        {
            bool suppress = false;

            if (resolvedAction.SupressionTime > 0)
            {
                try
                {
                    _asyncWaiter.WaitOne();
                    IList<DBHistoryAction> dbHistoryActions = await ServiceManager.StorageService.GetActions(resolvedAction.BeaconAction.Uuid);

                    if (dbHistoryActions != null)
                    {
                        foreach (var dbHistoryAction in dbHistoryActions)
                        {
                            var action_timestamp = dbHistoryAction.dt.AddSeconds(resolvedAction.SupressionTime);

                            if (action_timestamp > DateTimeOffset.Now)
                            {
                                suppress = true;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    _asyncWaiter.Set();
                }
            }

            return suppress;
        }

        /// <summary>
        /// Stores a beacon event to the database.
        /// </summary>
        /// <param name="eventArgs"></param>
        public IAsyncAction SaveBeaconEventAsync(BeaconEventArgs eventArgs)
        {
            return ServiceManager.StorageService.SaveHistoryEvent(eventArgs.Beacon.Pid, eventArgs.Timestamp, eventArgs.EventType).AsAsyncAction();
        }

        /// <summary>
        /// Stores a resolved and executed action to the database.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <param name="beaconAction"></param>
        public IAsyncAction SaveExecutedResolvedActionAsync(ResolvedActionsEventArgs eventArgs, BeaconAction beaconAction)
        {
            return ServiceManager.StorageService.SaveHistoryAction(
                beaconAction.Uuid, eventArgs.BeaconPid, DateTime.Now, eventArgs.BeaconEventType).AsAsyncAction();
        }

        /// <summary>
        /// For convenience.
        /// </summary>
        /// <param name="beaconAction"></param>
        /// <param name="beaconPid"></param>
        /// <param name="beaconEventType"></param>
        /// <returns></returns>
        public IAsyncAction SaveExecutedResolvedActionAsync(BeaconAction beaconAction, string beaconPid, BeaconEventType beaconEventType)
        {
            return ServiceManager.StorageService.SaveHistoryAction(
                beaconAction.Uuid, beaconPid, DateTime.Now, beaconEventType).AsAsyncAction();
        }

        /// <summary>
        /// Checks if there are new events or actions in the history and sends them to the server.
        /// </summary>
        public async Task FlushHistoryAsync()
        {
            await ServiceManager.StorageService.FlushHistory();
        }

    }
}

