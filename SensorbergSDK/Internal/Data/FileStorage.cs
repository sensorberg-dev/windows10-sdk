// Created by Kay Czarnotta on 30.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private const string FOLDER_LOCK_FILE = "folderlock";
        private const string ACTIONS_FILE_NAME = "actions.ini";

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
            return await GetUndeliveredEvents(true);
        }


        public async Task SetEventsAsDelivered()
        {
            StorageFolder folder = await GetFolder(FOREGROUND_EVENTS_FOLDER, true);
            IReadOnlyList<StorageFolder> folders = await folder.GetFoldersAsync();
            foreach (StorageFolder storageFolder in folders)
            {
                IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();

                //ignore unlocked folders
                if (files.FirstOrDefault(f => f.Name == FOLDER_LOCK_FILE) == null)
                {
                    continue;
                }
                await storageFolder.DeleteAsync();
            }

        }


        public async Task<IList<HistoryAction>> GetUndeliveredActions()
        {
            return await GetUndeliveredActions(true);

        }

        public async Task SetActionsAsDelivered()
        {
            StorageFolder folder = await GetFolder(FOREGROUND_ACTIONS_FOLDER, true);
            StorageFile deliveredActionsFile = await folder.CreateFileAsync(ACTIONS_FILE_NAME, CreationCollisionOption.OpenIfExists);
            IReadOnlyList<StorageFolder> folders = await folder.GetFoldersAsync();
            foreach (StorageFolder storageFolder in folders)
            {
                IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();

                //ignore unlocked folders
                if (files.FirstOrDefault(f => f.Name == FOLDER_LOCK_FILE) == null)
                {
                    continue;
                }

                StorageFile actionFile = files.FirstOrDefault(f => f.Name == ACTIONS_FILE_NAME);
                if (actionFile != null)
                {
                    List<HistoryAction> actions = FileStorageHelper.ActionsFromStrings(await FileIO.ReadLinesAsync(actionFile));

                    List<string> stringActions = new List<string>();
                    foreach (HistoryAction historyAction in actions)
                    {
                        historyAction.Delivered = true;
                        stringActions.Add(FileStorageHelper.ActionToString(historyAction));
                    }
                    await FileIO.AppendLinesAsync(deliveredActionsFile, stringActions);
                }
                await storageFolder.DeleteAsync();
            }
        }

        public async Task SaveHistoryAction(string uuid, string beaconPid, DateTimeOffset now, BeaconEventType beaconEventType)
        {
            StorageFolder folder = await GetFolder(Background ? BACKGROUND_ACTIONS_FOLDER : FOREGROUND_ACTIONS_FOLDER);
            StorageFile file = await folder.CreateFileAsync(ACTIONS_FILE_NAME, CreationCollisionOption.OpenIfExists);
            await FileIO.AppendTextAsync(file, FileStorageHelper.ActionToString(uuid, beaconPid, now, beaconEventType));
        }

        public async Task SaveHistoryEvents(string pid, DateTimeOffset timestamp, BeaconEventType eventType)
        {
            StorageFolder folder = await GetFolder(Background ? BACKGROUND_EVENTS_FOLDER : FOREGROUND_EVENTS_FOLDER);
            StorageFile file = await folder.CreateFileAsync(pid, CreationCollisionOption.OpenIfExists);
            await FileIO.AppendTextAsync(file, FileStorageHelper.EventToString(pid, timestamp, eventType));
        }


        public async Task<IList<DBHistoryAction>> GetActions(string uuid)
        {
            List<DBHistoryAction> returnActions = new List<DBHistoryAction>();
            IList<HistoryAction> actions = await GetUndeliveredActions(false);

            foreach (HistoryAction historyAction in actions)
            {
                if (historyAction.eid == uuid)
                {
                    returnActions.Add(new DBHistoryAction()
                    {
                        delivered = historyAction.Delivered,
                        dt = DateTimeOffset.Parse(historyAction.dt),
                        eid = historyAction.eid,
                        pid = historyAction.pid,
                        trigger = historyAction.trigger
                    });
                }
            }
            try
            {
                StorageFolder folder = await GetFolder(FOREGROUND_ACTIONS_FOLDER, true);
                StorageFile storageFile = await folder.GetFileAsync(ACTIONS_FILE_NAME);
                List<HistoryAction> actionsFromStrings = FileStorageHelper.ActionsFromStrings(await FileIO.ReadLinesAsync(storageFile));
                foreach (HistoryAction historyAction in actionsFromStrings)
                {
                    if (historyAction.eid == uuid)
                    {
                        returnActions.Add(new DBHistoryAction()
                        {
                            delivered = historyAction.Delivered,
                            dt = DateTimeOffset.Parse(historyAction.dt),
                            eid = historyAction.eid,
                            pid = historyAction.pid,
                            trigger = historyAction.trigger
                        });
                    }
                }
            }
            catch (FileNotFoundException)
            {
            }
            return returnActions;
        }

        public async Task<DBHistoryAction> GetAction(string uuid)
        {
            return (await GetActions(uuid)).FirstOrDefault();
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

        private async Task CreateEventMarker(StorageFolder folder)
        {
            await folder.CreateFileAsync(FOLDER_LOCK_FILE, CreationCollisionOption.OpenIfExists);
        }

        private async Task<StorageFolder> GetFolder(string path, bool parentOnly = false)
        {
            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(path);
            if (parentOnly)
            {
                return folder;
            }
            IReadOnlyList<StorageFolder> readOnlyList = await folder.GetFoldersAsync();
            foreach (StorageFolder storageFolder in readOnlyList)
            {
                try
                {
                    if (await storageFolder.GetFileAsync(FOLDER_LOCK_FILE) == null)
                    {
                        return storageFolder;
                    }
                }
                catch (FileNotFoundException)
                {
                    return storageFolder;
                }
            }
            return await folder.CreateFolderAsync(DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss"), CreationCollisionOption.OpenIfExists);
        }
        private async Task<IList<HistoryEvent>> GetUndeliveredEvents(bool lockFolder)
        {
            IList<HistoryEvent> events = new List<HistoryEvent>();

            StorageFolder folder = await GetFolder(FOREGROUND_EVENTS_FOLDER);
            if (lockFolder)
            {
                await CreateEventMarker(folder);
            }
            IReadOnlyList<StorageFolder> folders = await (await folder.GetParentAsync()).GetFoldersAsync();
            foreach (StorageFolder storageFolder in folders)
            {
                IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();

                //when no lock ignore unlocked folders
                if (!lockFolder && files.FirstOrDefault(f => f.Name == FOLDER_LOCK_FILE) == null)
                {
                    continue;
                }

                foreach (StorageFile file in files)
                {
                    List<HistoryEvent> fileEvents = FileStorageHelper.EventsFromStrings(await FileIO.ReadLinesAsync(file));
                    if (fileEvents != null)
                    {
                        foreach (HistoryEvent historyEvent in fileEvents)
                        {
                            if (!historyEvent.Delivered)
                            {
                                events.Add(historyEvent);
                            }
                        }
                    }
                }
            }

            return events;
        }
        private async Task<IList<HistoryAction>> GetUndeliveredActions(bool lockFolder)
        {
            IList<HistoryAction> actions = new List<HistoryAction>();

            StorageFolder folder = await GetFolder(FOREGROUND_ACTIONS_FOLDER);
            if (lockFolder)
            {
                await CreateEventMarker(folder);
            }
            IReadOnlyList<StorageFolder> folders = await (await folder.GetParentAsync()).GetFoldersAsync();
            foreach (StorageFolder storageFolder in folders)
            {
                try
                {
                    IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();

                    //when no lock ignore unlocked folders
                    if (!lockFolder && files.FirstOrDefault(f => f.Name == FOLDER_LOCK_FILE) == null)
                    {
                        continue;
                    }

                    StorageFile first = null;
                    foreach (var f in files)
                    {
                        if (f.Name == ACTIONS_FILE_NAME)
                        {
                            first = f;
                            break;
                        }
                    }
                    if (first != null)
                    {
                        List<HistoryAction> fileActions = FileStorageHelper.ActionsFromStrings(await FileIO.ReadLinesAsync(first));
                        if (fileActions != null)
                        {
                            foreach (HistoryAction historyAction in fileActions)
                            {
                                if (!historyAction.Delivered)
                                {
                                    actions.Add(historyAction);
                                }
                            }
                        }
                    }
                }
                catch (FileNotFoundException)
                {}
            }

            return actions;
        }
    }
}