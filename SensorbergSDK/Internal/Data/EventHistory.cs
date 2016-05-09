// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using MetroLog;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDK.Internal
{
    /// <summary>
    /// Event storage. It stores all past beacon events and actions associated with the events.
    /// </summary>
    public sealed class EventHistory : IDisposable
    {
        private static readonly ILogger logger = LogManagerFactory.DefaultLogManager.GetLogger<EventHistory>();
        private readonly AutoResetEvent _asyncWaiter;

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
            logger.Trace("CheckSendOnlyOnceAsync {0}", resolvedAction.BeaconAction.Id);
            bool sendonlyOnce = false;

            if (resolvedAction.SendOnlyOnce)
            {
                try
                {
                    _asyncWaiter.WaitOne();
                    HistoryAction dbHistoryAction = await ServiceManager.StorageService.GetAction(resolvedAction.BeaconAction.Uuid);

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
            logger.Trace("ShouldSupressAsync {0}", resolvedAction.BeaconAction.Id);

            if (resolvedAction.SuppressionTime > 0)
            {
                try
                {
                    _asyncWaiter.WaitOne();
                    IList<HistoryAction> dbHistoryActions = await ServiceManager.StorageService.GetActions(resolvedAction.BeaconAction.Uuid);

                    if (dbHistoryActions != null)
                    {
                        foreach (var dbHistoryAction in dbHistoryActions)
                        {
                            var actionTimestamp = DateTimeOffset.Parse(dbHistoryAction.dt).AddSeconds(resolvedAction.SuppressionTime);

                            if (actionTimestamp > DateTimeOffset.Now)
                            {
                                return true;
                            }
                        }
                    }
                }
                finally
                {
                    _asyncWaiter.Set();
                }
            }

            return false;
        }

        /// <summary>
        /// Stores a beacon event to the database.
        /// </summary>
        /// <param name="eventArgs"></param>
        public async Task SaveBeaconEventAsync(BeaconEventArgs eventArgs)
        {
            await ServiceManager.StorageService.SaveHistoryEvent(eventArgs.Beacon.Pid, eventArgs.Timestamp, eventArgs.EventType);
        }

        /// <summary>
        /// Stores a resolved and executed action to the database.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <param name="beaconAction"></param>
        public async Task SaveExecutedResolvedActionAsync(ResolvedActionsEventArgs eventArgs, BeaconAction beaconAction)
        {
            await ServiceManager.StorageService.SaveHistoryAction(beaconAction.Uuid, eventArgs.BeaconPid, DateTime.Now, eventArgs.BeaconEventType);
        }

        /// <summary>
        /// For convenience.
        /// </summary>
        /// <param name="beaconAction"></param>
        /// <param name="beaconPid"></param>
        /// <param name="beaconEventType"></param>
        /// <returns></returns>
        public async Task SaveExecutedResolvedActionAsync(BeaconAction beaconAction, string beaconPid, BeaconEventType beaconEventType)
        {
            await ServiceManager.StorageService.SaveHistoryAction(beaconAction.Uuid, beaconPid, DateTime.Now, beaconEventType);
        }

        /// <summary>
        /// Checks if there are new events or actions in the history and sends them to the server.
        /// </summary>
        public async Task FlushHistoryAsync()
        {
            await ServiceManager.StorageService.FlushHistory();
        }

        public void Dispose()
        {
            _asyncWaiter?.Dispose();
        }
    }
}

