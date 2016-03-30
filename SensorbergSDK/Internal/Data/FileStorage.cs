// Created by Kay Czarnotta on 30.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using SensorbergSDK.Services;

namespace SensorbergSDK.Internal.Data
{
    public class FileStorage : IStorage
    {
        const string SERPERATOR = "\\";
        const string ROOT_FOLDER = "sensorberg-storage";
        const string BACKGROUND_FOLDER = ROOT_FOLDER + SERPERATOR + BACKGROUND_FOLDER_NAME;
        const string FOREGROUND_FOLDER = ROOT_FOLDER + SERPERATOR + FOREGROUND_FOLDER_NAME;
        const string BACKGROUND_ACTIONS_FOLDER = BACKGROUND_FOLDER + SERPERATOR + ACTIONS_FOLDER_NAME;
        const string BACKGROUND_EVENTS_FOLDER = BACKGROUND_FOLDER + SERPERATOR + EVENTS_FOLDER_NAME;
        const string FOREGROUND_ACTIONS_FOLDER = FOREGROUND_FOLDER + SERPERATOR + ACTIONS_FOLDER_NAME;
        const string FOREGROUND_EVENTS_FOLDER = FOREGROUND_FOLDER + SERPERATOR + EVENTS_FOLDER_NAME;
        private const string BACKGROUND_FOLDER_NAME = "background";
        private const string FOREGROUND_FOLDER_NAME = "foreground";
        private const string ACTIONS_FOLDER_NAME = "actions";
        private const string EVENTS_FOLDER_NAME = "events";

        public bool Background { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        public async Task InitStorage()
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFolder root = await folder.CreateFolderAsync(ROOT_FOLDER, CreationCollisionOption.OpenIfExists);
            StorageFolder background = await root.CreateFolderAsync(BACKGROUND_FOLDER_NAME, CreationCollisionOption.OpenIfExists);
            StorageFolder foreground = await root.CreateFolderAsync(FOREGROUND_FOLDER_NAME, CreationCollisionOption.OpenIfExists);
            await background.CreateFolderAsync(ACTIONS_FOLDER_NAME, CreationCollisionOption.OpenIfExists);
            await background.CreateFolderAsync(EVENTS_FOLDER_NAME, CreationCollisionOption.OpenIfExists);
            await foreground.CreateFolderAsync(ACTIONS_FOLDER_NAME, CreationCollisionOption.OpenIfExists);
            await foreground.CreateFolderAsync(EVENTS_FOLDER_NAME, CreationCollisionOption.OpenIfExists);
        }

        public async Task<IList<HistoryEvent>> GetUndeliveredEvents()
        {
            IList<HistoryEvent> events = new List<HistoryEvent>();

            StorageFolder folder = await GetFolder(FOREGROUND_EVENTS_FOLDER);
            IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
            foreach (StorageFile file in files)
            {
                List<HistoryEvent> fileEvents =  FileStorageHelper.EventsFromStrings(await FileIO.ReadLinesAsync(file));
            }

            return events;
        }

        public Task<IList<HistoryAction>> GetUndeliveredActions()
        {
            throw new NotImplementedException();
        }

        public Task SetEventsAsDelivered()
        {
            throw new NotImplementedException();
        }

        public Task SetActionsAsDelivered()
        {
            throw new NotImplementedException();
        }

        public Task SaveHistoryAction(string uuid, string beaconPid, DateTimeOffset now, BeaconEventType beaconEventType)
        {
            throw new NotImplementedException();
        }

        public async Task SaveHistoryEvents(string pid, DateTimeOffset timestamp, BeaconEventType eventType)
        {
            StorageFolder folder = await GetFolder(Background ? BACKGROUND_EVENTS_FOLDER : FOREGROUND_EVENTS_FOLDER);
            StorageFile file = await folder.CreateFileAsync(pid, CreationCollisionOption.OpenIfExists);
            await FileIO.AppendTextAsync(file, FileStorageHelper.EventToString(pid, timestamp, eventType));
        }


        public Task<IList<DBHistoryAction>> GetActions(string uuid)
        {
            throw new NotImplementedException();
        }

        public Task<DBHistoryAction> GetAction(string uuid)
        {
            throw new NotImplementedException();
        }

        public Task CleanDatabase()
        {
            throw new NotImplementedException();
        }

        public Task<IList<BeaconAction>> GetBeaconActionsFromBackground()
        {
            throw new NotImplementedException();
        }

        public Task<IList<DelayedActionData>> GetDelayedActions(int maxDelayFromNowInSeconds)
        {
            throw new NotImplementedException();
        }

        public Task SetDelayedActionAsExecuted(int id)
        {
            throw new NotImplementedException();
        }

        public Task SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventTypeDetectedByDevice)
        {
            throw new NotImplementedException();
        }

        public Task<IList<DBBackgroundEventsHistory>> GetBeaconBackgroundEventsHistory(string pid)
        {
            throw new NotImplementedException();
        }

        public Task SaveBeaconBackgroundEvent(string pid, BeaconEventType enter)
        {
            throw new NotImplementedException();
        }

        public Task DeleteBackgroundEvent(string pid)
        {
            throw new NotImplementedException();
        }

        public Task SaveBeaconActionFromBackground(BeaconAction beaconAction)
        {
            throw new NotImplementedException();
        }

        public Task UpdateBeaconBackgroundEvent(string pidIn, BeaconEventType triggerIn)
        {
            throw new NotImplementedException();
        }

        public Task UpdateBackgroundEvent(string pidIn, BeaconEventType eventType)
        {
            throw new NotImplementedException();
        }


        private async Task<StorageFolder> GetFolder(string path)
        {
            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(path);
            return folder;
        }
    }
}