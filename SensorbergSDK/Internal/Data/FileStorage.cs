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
        public const string FOREGROUND_ACTIONS_FOLDER = FOREGROUND_FOLDER + SERPERATOR + ACTIONS_FOLDER_NAME;
        public const string FOREGROUND_EVENTS_FOLDER = FOREGROUND_FOLDER + SERPERATOR + EVENTS_FOLDER_NAME;
        private readonly string[] EVENT_FOLDERS = new string[] { BACKGROUND_EVENTS_FOLDER, FOREGROUND_EVENTS_FOLDER };
        private readonly string[] ACTION_FOLDERS = new string[] { BACKGROUND_ACTIONS_FOLDER, FOREGROUND_ACTIONS_FOLDER };
        private const string BACKGROUND_FOLDER_NAME = "background";
        private const string FOREGROUND_FOLDER_NAME = "foreground";
        private const string ACTIONS_FOLDER_NAME = "actions";
        private const string EVENTS_FOLDER_NAME = "events";
        private const string FOLDER_LOCK_FILE = "folderlock";
        public const string ACTIONS_FILE_NAME = "actions.ini";
        private const string DELAYED_ACTIONS_FILE_NAME = "delayedactions.ini";

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
            foreach (string currentfolder in EVENT_FOLDERS)
            {
                StorageFolder folder = await GetFolder(currentfolder, true);
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
        }


        public async Task<IList<HistoryAction>> GetUndeliveredActions()
        {
            return await GetUndeliveredActions(true);

        }

        public async Task SetActionsAsDelivered()
        {
            foreach (string currentfolder in ACTION_FOLDERS)
            {
                StorageFolder folder = await GetFolder(currentfolder, true);
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
        }

        public async Task SaveHistoryAction(string uuid, string beaconPid, DateTimeOffset now, BeaconEventType beaconEventType)
        {
            StorageFolder folder = await GetFolder(Background ? BACKGROUND_ACTIONS_FOLDER : FOREGROUND_ACTIONS_FOLDER);
            StorageFile file = await folder.CreateFileAsync(ACTIONS_FILE_NAME, CreationCollisionOption.OpenIfExists);
            string actionToString = FileStorageHelper.ActionToString(uuid, beaconPid, now, beaconEventType);
            await RetryAppending(file, actionToString);
        }

        public async Task SaveHistoryEvents(string pid, DateTimeOffset timestamp, BeaconEventType eventType)
        {
            StorageFolder folder = await GetFolder(Background ? BACKGROUND_EVENTS_FOLDER : FOREGROUND_EVENTS_FOLDER);
            StorageFile file = await folder.CreateFileAsync(pid, CreationCollisionOption.OpenIfExists);
            string eventToString = FileStorageHelper.EventToString(pid, timestamp, eventType);
            await RetryAppending(file, eventToString);
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

        public async Task CleanDatabase()
        {
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder root = await folder.CreateFolderAsync(ROOT_FOLDER, CreationCollisionOption.OpenIfExists);
                await root.DeleteAsync();
            }
            catch (FileNotFoundException )
            {
            }
            await InitStorage();
        }

        public Task<IList<BeaconAction>> GetBeaconActionsFromBackground()
        {
            throw new NotImplementedException();
        }

        public async Task<IList<DelayedActionData>> GetDelayedActions(int maxDelayFromNowInSeconds)
        {
            DateTimeOffset maxDelayfromNow = DateTimeOffset.Now.AddSeconds(maxDelayFromNowInSeconds);
            List<DelayedActionData> actions = new List<DelayedActionData>();

            StorageFolder folder = await GetFolder(FOREGROUND_ACTIONS_FOLDER, true);
            StorageFile file = await folder.CreateFileAsync(DELAYED_ACTIONS_FILE_NAME, CreationCollisionOption.OpenIfExists);
            List<FileStorageHelper.DelayedActionHelper> delayedActionHelpers = FileStorageHelper.DelayedActionsFromStrings(await FileIO.ReadLinesAsync(file));

            foreach (FileStorageHelper.DelayedActionHelper delayedActionHelper in delayedActionHelpers)
            {
                if (delayedActionHelper.Offset < maxDelayfromNow && !delayedActionHelper.Executed)
                {
                    DelayedActionData data = FileStorageHelper.DelayedActionFromHelper(delayedActionHelper);
                    actions.Add(data);
                }
            }

            return actions;
        }

        public async Task SetDelayedActionAsExecuted(string id)
        {
            StorageFolder folder = await GetFolder(FOREGROUND_ACTIONS_FOLDER, true);
            StorageFile file = await folder.CreateFileAsync(DELAYED_ACTIONS_FILE_NAME, CreationCollisionOption.OpenIfExists);
            List<FileStorageHelper.DelayedActionHelper> delayedActionHelpers = FileStorageHelper.DelayedActionsFromStrings(await FileIO.ReadLinesAsync(file));

            bool needed = false;
            List<string> strings = new List<string>();
            foreach (FileStorageHelper.DelayedActionHelper delayedActionHelper in delayedActionHelpers)
            {
                if(delayedActionHelper.Id == id)
                {
                    delayedActionHelper.Executed = true;
                    needed = true;
                }
                strings.Add(FileStorageHelper.DelayedActionToString(delayedActionHelper));
            }
            if (needed)
            {
                await FileIO.WriteLinesAsync(file, strings);
            }

        }

        public async Task SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid, BeaconEventType beaconEventType)
        {
            StorageFolder folder = await GetFolder(Background?BACKGROUND_ACTIONS_FOLDER:FOREGROUND_ACTIONS_FOLDER, true);
            StorageFile file = await folder.CreateFileAsync(DELAYED_ACTIONS_FILE_NAME, CreationCollisionOption.OpenIfExists);
            string actionToString = FileStorageHelper.DelayedActionToString(action, dueTime, beaconPid, beaconEventType);
            await RetryAppending(file, actionToString);

        }

        public async Task<IList<DBBackgroundEventsHistory>> GetBeaconBackgroundEventsHistory(string pid)
        {
            StorageFolder folder = await GetFolder(BACKGROUND_ACTIONS_FOLDER, true);
            StorageFile file = await folder.CreateFileAsync(ACTIONS_FILE_NAME, CreationCollisionOption.OpenIfExists);
            Dictionary<string, Dictionary<string, long>> dic = FileStorageHelper.BackoundEventsFromString(await FileIO.ReadTextAsync(file));
            IList<DBBackgroundEventsHistory> histories = new List<DBBackgroundEventsHistory>();
            if (dic.ContainsKey(pid))
            {
                Dictionary<string, long> historyDictionary = dic[pid];
                DBBackgroundEventsHistory h = new DBBackgroundEventsHistory();
                h.BeaconPid = pid;
                h.EventTime = DateTimeOffset.FromUnixTimeMilliseconds(historyDictionary["time"]);
                h.EventType = (int) historyDictionary["event"];
                histories.Add(h);
            }
            return histories;
        }

        public async Task SaveBeaconBackgroundEvent(string pid, BeaconEventType enter)
        {
            await SaveHistoryEvents(pid, DateTimeOffset.Now, enter);
        }

        public async Task UpdateBackgroundEvent(string pidIn, BeaconEventType eventType)
        {
            await SaveBeaconBackgroundEvent(pidIn, eventType);
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


        private async Task CreateEventMarker(StorageFolder folder)
        {
            await folder.CreateFileAsync(FOLDER_LOCK_FILE, CreationCollisionOption.OpenIfExists);
        }

        public async Task<StorageFolder> GetFolder(string path, bool parentOnly = false)
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
            
            foreach (string currentfolder in EVENT_FOLDERS)
            {
                StorageFolder folder = await GetFolder(currentfolder);
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
            }

            return events;
        }
        private async Task<IList<HistoryAction>> GetUndeliveredActions(bool lockFolder)
        {
            IList<HistoryAction> actions = new List<HistoryAction>();

            foreach (string currentfolder in ACTION_FOLDERS)
            {
                StorageFolder folder = await GetFolder(currentfolder);
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
                    {
                    }
                }
            }
            return actions;
        }
        private static async Task RetryAppending(StorageFile file, string s)
        {
            int retry = 0;
            int maxRetry = 6;
            do
            {
                try
                {
                    await FileIO.AppendTextAsync(file, s);
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    //file is locked
                }
                finally
                {
                    retry++;
                }
                await Task.Delay((int)Math.Pow(2, retry + 1) * 10);
            } while (retry < maxRetry);
            throw new UnauthorizedAccessException("File was locked");
        }

    }
}