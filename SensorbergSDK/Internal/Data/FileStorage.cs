// Created by Kay Czarnotta on 30.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using MetroLog;
using SensorbergSDK.Services;

namespace SensorbergSDK.Internal.Data
{
    public class FileStorage : IStorage
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<FileStorage>();
        private const string BackgroundFolderName = "background";
        private const string ForegroundFolderName = "foreground";
        private const string ActionsFolderName = "actions";
        private const string EventsFolderName = "events";
        private const string SettingsFolderName = "settings";
        private const string FolderLockFile = "folderlock";
        public const string ActionsFileName = "actions.ini";
        private const string DelayedActionsFileName = "delayedactions.ini";
        private const string Serperator = "\\";
        private const string RootFolder = "sensorberg-storage";
        private const string BackgroundFolder = RootFolder + Serperator + BackgroundFolderName;
        private const string ForegroundFolder = RootFolder + Serperator + ForegroundFolderName;
        private const string BackgroundActionsFolder = BackgroundFolder + Serperator + ActionsFolderName;
        private const string BackgroundEventsFolder = BackgroundFolder + Serperator + EventsFolderName;
        private const string BackgroundSettingsFolder = BackgroundFolder + Serperator + SettingsFolderName;
        public const string ForegroundActionsFolder = ForegroundFolder + Serperator + ActionsFolderName;
        public const string ForegroundEventsFolder = ForegroundFolder + Serperator + EventsFolderName;
        private readonly string[] _eventFolders = new string[] {BackgroundEventsFolder, ForegroundEventsFolder};
        private readonly string[] _actionFolders = new string[] {BackgroundActionsFolder, ForegroundActionsFolder};

        public bool Background { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        public async Task InitStorage()
        {
            Logger.Trace("Create folders");
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFolder root = await folder.CreateFolderAsync(RootFolder, CreationCollisionOption.OpenIfExists);
            StorageFolder background = await root.CreateFolderAsync(BackgroundFolderName, CreationCollisionOption.OpenIfExists);
            StorageFolder foreground = await root.CreateFolderAsync(ForegroundFolderName, CreationCollisionOption.OpenIfExists);
            await background.CreateFolderAsync(ActionsFolderName, CreationCollisionOption.OpenIfExists);
            await background.CreateFolderAsync(EventsFolderName, CreationCollisionOption.OpenIfExists);
            await background.CreateFolderAsync(SettingsFolderName, CreationCollisionOption.OpenIfExists);
            await foreground.CreateFolderAsync(ActionsFolderName, CreationCollisionOption.OpenIfExists);
            await foreground.CreateFolderAsync(EventsFolderName, CreationCollisionOption.OpenIfExists);
            await foreground.CreateFolderAsync(SettingsFolderName, CreationCollisionOption.OpenIfExists);
        }

        public async Task<IList<HistoryEvent>> GetUndeliveredEvents()
        {
            return await GetUndeliveredEvents(true);
        }


        public async Task SetEventsAsDelivered()
        {
            foreach (string currentfolder in _eventFolders)
            {
                StorageFolder folder = await GetFolder(currentfolder, true);
                IReadOnlyList<StorageFolder> folders = await folder.GetFoldersAsync();
                foreach (StorageFolder storageFolder in folders)
                {
                    IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();

                    //ignore unlocked folders
                    if (files.FirstOrDefault(f => f.Name == FolderLockFile) == null)
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
            foreach (string currentfolder in _actionFolders)
            {
                StorageFolder folder = await GetFolder(currentfolder, true);
                StorageFile deliveredActionsFile = await folder.CreateFileAsync(ActionsFileName, CreationCollisionOption.OpenIfExists);
                IReadOnlyList<StorageFolder> folders = await folder.GetFoldersAsync();
                foreach (StorageFolder storageFolder in folders)
                {
                    IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();

                    //ignore unlocked folders
                    if (files.FirstOrDefault(f => f.Name == FolderLockFile) == null)
                    {
                        continue;
                    }

                    StorageFile actionFile = files.FirstOrDefault(f => f.Name == ActionsFileName);
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

        public async Task<bool> SaveHistoryAction(HistoryAction action)
        {
            try
            {
                StorageFolder folder = await GetFolder(Background ? BackgroundActionsFolder : ForegroundActionsFolder);
                StorageFile file = await folder.CreateFileAsync(ActionsFileName, CreationCollisionOption.OpenIfExists);
                action.Background = Background;
                string actionToString = FileStorageHelper.ActionToString(action);
                return await RetryAppending(file, actionToString);
            }
            catch (Exception e)
            {
                Logger.Error("Error writing HistoryAction", e);
            }
            return false;
        }

        public async Task<bool> SaveHistoryEvents(HistoryEvent he)
        {
            try
            {
                StorageFolder folder = await GetFolder(Background ? BackgroundEventsFolder : ForegroundEventsFolder);
                StorageFile file = await folder.CreateFileAsync(he.BeaconId, CreationCollisionOption.OpenIfExists);
                string eventToString = FileStorageHelper.EventToString(he);
                return await RetryAppending(file, eventToString);
            }
            catch (Exception e)
            {
                Logger.Error("Error writing HistoryEvent", e);
            }
            return false;
        }


        public async Task<IList<HistoryAction>> GetActions(string uuid)
        {
            Logger.Trace("GetActions {0}", uuid);
            List<HistoryAction> returnActions = new List<HistoryAction>();
            IList<HistoryAction> actions = await GetUndeliveredActions(false);

            foreach (HistoryAction historyAction in actions)
            {
                if (historyAction.EventId == uuid)
                {
                    returnActions.Add(historyAction);
                }
            }
            try
            {
                StorageFolder folder = await GetFolder(ForegroundActionsFolder, true);
                StorageFile storageFile = await folder.GetFileAsync(ActionsFileName);
                List<HistoryAction> actionsFromStrings = FileStorageHelper.ActionsFromStrings(await FileIO.ReadLinesAsync(storageFile));
                foreach (HistoryAction historyAction in actionsFromStrings)
                {
                    if (historyAction.EventId == uuid)
                    {
                        returnActions.Add(historyAction);
                    }
                }
            }
            catch (SEHException)
            {
            }
            catch (FileNotFoundException)
            {
            }
            Logger.Trace("GetActions {0} End", uuid);
            return returnActions;
        }

        public async Task<HistoryAction> GetAction(string uuid)
        {
            return (await GetActions(uuid)).FirstOrDefault();
        }

        public async Task CleanDatabase()
        {
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder root = await folder.CreateFolderAsync(RootFolder, CreationCollisionOption.OpenIfExists);
                await root.DeleteAsync();
            }
            catch (SEHException)
            {
            }
            catch (FileNotFoundException)
            {
            }
            await InitStorage();
        }

        public async Task<IList<DelayedActionData>> GetDelayedActions(int maxDelayFromNowInSeconds)
        {
            DateTimeOffset maxDelayfromNow = DateTimeOffset.Now.AddSeconds(maxDelayFromNowInSeconds);
            List<DelayedActionData> actions = new List<DelayedActionData>();

            StorageFolder folder = await GetFolder(ForegroundActionsFolder, true);
            StorageFile file = await folder.CreateFileAsync(DelayedActionsFileName, CreationCollisionOption.OpenIfExists);
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
            StorageFolder folder = await GetFolder(ForegroundActionsFolder, true);
            StorageFile file = await folder.CreateFileAsync(DelayedActionsFileName, CreationCollisionOption.OpenIfExists);
            List<FileStorageHelper.DelayedActionHelper> delayedActionHelpers = FileStorageHelper.DelayedActionsFromStrings(await FileIO.ReadLinesAsync(file));

            bool needed = false;
            List<string> strings = new List<string>();
            foreach (FileStorageHelper.DelayedActionHelper delayedActionHelper in delayedActionHelpers)
            {
                if (delayedActionHelper.Id == id)
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

        public async Task<bool> SaveDelayedAction(ResolvedAction action, DateTimeOffset dueTime, string beaconPid,
            BeaconEventType beaconEventType)
        {
            StorageFolder folder = await GetFolder(Background ? BackgroundActionsFolder : ForegroundActionsFolder, true);
            StorageFile file = await folder.CreateFileAsync(DelayedActionsFileName, CreationCollisionOption.OpenIfExists);
            string actionToString = FileStorageHelper.DelayedActionToString(action, dueTime, beaconPid, beaconEventType);
            return await RetryAppending(file, actionToString);

        }

        public async Task<bool> SaveBeaconEventState(string pid, BeaconEventType type)
        {
            StorageFolder folder = await GetFolder(BackgroundSettingsFolder, true);
            StorageFile file = await folder.CreateFileAsync(pid, CreationCollisionOption.OpenIfExists);
            return await RetryWriting(file, FileStorageHelper.BeaconEventStateToString(pid, type, DateTimeOffset.Now));
        }

        public async Task<BackgroundEvent> GetLastEventStateForBeacon(string pid)
        {
            StorageFolder folder = await GetFolder(BackgroundSettingsFolder, true);
            StorageFile file = await folder.CreateFileAsync(pid, CreationCollisionOption.OpenIfExists);
            try
            {
                return FileStorageHelper.BeaconEventStateFromString(await FileIO.ReadTextAsync(file));
            }
            catch (SEHException)
            {
                return null;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public async Task<List<HistoryAction>> GetActionsForForeground(bool doNotDelete = false)
        {
            List<HistoryAction> actions = new List<HistoryAction>();

            StorageFolder folder = await GetFolder(BackgroundActionsFolder, true);
            StorageFile deliveredActionsFile = await folder.CreateFileAsync(ActionsFileName, CreationCollisionOption.OpenIfExists);

            List<HistoryAction> fileActions = FileStorageHelper.ActionsFromStrings(await FileIO.ReadLinesAsync(deliveredActionsFile));
            if (fileActions != null)
            {
                foreach (HistoryAction historyAction in fileActions)
                {
                    if (historyAction.Background)
                    {
                        actions.Add(historyAction);
                    }
                }
                if (!doNotDelete && fileActions.Count != 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (HistoryAction historyAction in fileActions)
                    {
                        historyAction.Background = false;
                        sb.Append(FileStorageHelper.ActionToString(historyAction));
                        sb.Append('\n');
                    }
                    if (!await RetryWriting(deliveredActionsFile, sb.ToString()))
                    {
                        Logger.Error("GetActionsForForeground#1: Writing failed ");
                    }
                }
            }
            IReadOnlyList<StorageFolder> folders = await folder.GetFoldersAsync();
            foreach (StorageFolder storageFolder in folders)
            {
                try
                {
                    IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();
                    StorageFile first = null;
                    foreach (var f in files)
                    {
                        if (f.Name == ActionsFileName)
                        {
                            first = f;
                            break;
                        }
                    }
                    if (first != null)
                    {
                        fileActions = FileStorageHelper.ActionsFromStrings(await FileIO.ReadLinesAsync(first));
                        if (fileActions != null && fileActions.Count != 0)
                        {
                            foreach (HistoryAction historyAction in fileActions)
                            {
                                if (historyAction.Background)
                                {
                                    actions.Add(historyAction);
                                }
                            }
                            if (!doNotDelete)
                            {
                                StringBuilder sb = new StringBuilder();
                                foreach (HistoryAction historyAction in fileActions)
                                {
                                    historyAction.Background = false;
                                    sb.Append(FileStorageHelper.ActionToString(historyAction));
                                    sb.Append('\n');
                                }
                                if (!await RetryWriting(first, sb.ToString()))
                                {
                                    Logger.Error("GetActionsForForeground#2: Writing failed ");
                                }
                            }
                        }
                    }
                }
                catch (SEHException)
                {
                }
                catch (FileNotFoundException)
                {
                }
            }
            return actions;
        }

        private async Task CreateEventMarker(StorageFolder folder)
        {
            StorageFile storageFile = await folder.CreateFileAsync(FolderLockFile, CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(storageFile, "lock");
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
                    if (await storageFolder.TryGetItemAsync(FolderLockFile) == null)
                    {
                        return storageFolder;
                    }
                }
                catch (SEHException)
                {
                    return storageFolder;
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

            foreach (string currentfolder in _eventFolders)
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
                    if (!lockFolder && files.FirstOrDefault(f => f.Name == FolderLockFile) == null)
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

            foreach (string currentfolder in _actionFolders)
            {
                StorageFolder folder = await GetFolder(currentfolder);
                if (lockFolder)
                {
                    await CreateEventMarker(folder);
                }
                StorageFolder parentFolder = await folder.GetParentAsync();
                IReadOnlyList<StorageFolder> folders = await parentFolder.GetFoldersAsync();
                foreach (StorageFolder storageFolder in folders)
                {
                    try
                    {
                        IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();
                        StorageFile first = null;
                        foreach (var f in files)
                        {
                            if (f.Name == ActionsFileName)
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
                    catch (SEHException)
                    {
                    }
                    catch (FileNotFoundException)
                    {
                    }
                }
            }
            return actions;
        }


        /// <summary>
        /// Retry of append to file.
        /// </summary>
        /// <param name="file">File to write.</param>
        /// <param name="s">String to write.</param>
        private static async Task<bool> RetryAppending(StorageFile file, string s)
        {
            int retry = 0;
            int maxRetry = 6;
            do
            {
                try
                {
                    await FileIO.AppendTextAsync(file, s);
                    return true;
                }
                catch (FileLoadException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                    //file is locked
                }
                catch (FileNotFoundException)
                {
                    return false;
                }
                catch (Exception ex)
                {
                    Logger.Error("unknown error RetryWriting", ex);
                }
                finally
                {
                    retry++;
                }
                await Task.Delay((int) Math.Pow(2, retry + 1)*10);
            } while (retry < maxRetry);

            return false;
        }

        /// <summary>
        /// Retry of writing to file.
        /// </summary>
        /// <param name="file">File to write.</param>
        /// <param name="s">String to write.</param>
        private static async Task<bool> RetryWriting(StorageFile file, string s)
        {
            Logger.Trace("RetryWriting " + s);
            int retry = 0;
            int maxRetry = 6;
            do
            {
                try
                {
                    await FileIO.WriteTextAsync(file, s);
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    //file is locked
                }
                catch (FileNotFoundException)
                {
                    return false;
                }
                catch (Exception ex)
                {
                    Logger.Error("unknown error RetryWriting", ex);
                }
                finally
                {
                    retry++;
                }
                await Task.Delay((int) Math.Pow(2, retry + 1)*10);
            } while (retry < maxRetry);

            return false;
        }

    }
}