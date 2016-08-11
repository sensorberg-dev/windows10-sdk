// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using MetroLog;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Internal.Transport;

namespace SensorbergSDK.Internal.Data
{
    /// <summary>
    /// Event storage. It stores all past beacon events and actions associated with the events.
    /// </summary>
    public sealed class EventHistory
    {
        public const string KeyHistoryevents = "historyEvents";
        public const string KeyFireOnlyOnceActions = "fire_only_once_actions";
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<EventHistory>();
        private ApplicationDataContainer lastEvents;
        private ApplicationDataContainer firedActions;

        public EventHistory()
        {
            if (!ApplicationData.Current.LocalSettings.Containers.ContainsKey(KeyHistoryevents))
            {
                ApplicationData.Current.LocalSettings.CreateContainer(KeyHistoryevents,ApplicationDataCreateDisposition.Always);
            }
            lastEvents = ApplicationData.Current.LocalSettings.Containers[KeyHistoryevents];

            if (!ApplicationData.Current.RoamingSettings.Containers.ContainsKey(KeyFireOnlyOnceActions))
            {
                ApplicationData.Current.RoamingSettings.CreateContainer(KeyFireOnlyOnceActions, ApplicationDataCreateDisposition.Always);
            }
            firedActions = ApplicationData.Current.RoamingSettings.Containers[KeyFireOnlyOnceActions];
        }

        /// <summary>
        /// If sendOnlyOnce is true for resolved action, fuction will check from the history if the
        /// action is already presented for the user.
        /// </summary>
        /// <param name="resolvedAction"></param>
        /// <returns>True ,if action type is SendOnlyOnce, and it has been shown already. Otherwise false.</returns>
        public bool CheckSendOnlyOnceAsync(ResolvedAction resolvedAction)
        {
            Logger.Trace("CheckSendOnlyOnceAsync {0}", resolvedAction.BeaconAction.Id);

            if (resolvedAction.SendOnlyOnce)
            {
                if (firedActions.Values.ContainsKey(resolvedAction.BeaconAction.Uuid))
                {
                    return true;
                }
                else
                {
                    firedActions.Values[resolvedAction.BeaconAction.Uuid] = true;
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// If supressionTime is set for the action, fuction will check from the history if the
        /// action is already presented during the supression time.
        /// </summary>
        /// <param name="resolvedAction"></param>
        /// <returns>True only if action should be supressed.</returns>
        public bool ShouldSupressAsync(ResolvedAction resolvedAction)
        {
            Logger.Trace("ShouldSupressAsync {0}", resolvedAction.BeaconAction.Id);
            bool retVal = false;

            if (resolvedAction.SuppressionTime > 0)
            {
                if (lastEvents.Values.ContainsKey(resolvedAction.BeaconAction.Uuid))
                {
                    if ((long)lastEvents.Values[resolvedAction.BeaconAction.Uuid] + resolvedAction.SuppressionTime*1000 > DateTimeOffset.Now.ToUnixTimeMilliseconds())
                    {
                        retVal = true;
                    }
                }
                lastEvents.Values[resolvedAction.BeaconAction.Uuid] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
            return retVal;
        }

        /// <summary>
        /// Stores a beacon event to the database.
        /// </summary>
        public async Task SaveBeaconEventAsync(BeaconEventArgs eventArgs, string location)
        {
            await ServiceManager.StorageService.SaveHistoryEvent(eventArgs.Beacon.Pid, eventArgs.Timestamp, eventArgs.EventType, location);
        }

        /// <summary>
        /// Stores a resolved and executed action to the database.
        /// </summary>
        public async Task SaveExecutedResolvedActionAsync(ResolvedActionsEventArgs eventArgs, BeaconAction beaconAction)
        {
            await ServiceManager.StorageService.SaveHistoryAction(beaconAction.Uuid, eventArgs.BeaconPid, DateTime.Now, eventArgs.BeaconEventType, eventArgs.Location);
        }

        /// <summary>
        /// For convenience.
        /// </summary>
        public async Task SaveExecutedResolvedActionAsync(BeaconAction beaconAction, string beaconPid, BeaconEventType beaconEventType, string location)
        {
            await ServiceManager.StorageService.SaveHistoryAction(beaconAction.Uuid, beaconPid, DateTime.Now, beaconEventType, location);
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

