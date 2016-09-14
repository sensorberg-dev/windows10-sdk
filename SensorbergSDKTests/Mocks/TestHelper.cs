// Created by Kay Czarnotta on 21.04.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using SensorbergSDK;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Internal.Transport;

namespace SensorbergSDKTests.Mocks
{
    public class TestHelper
    {
        public static async Task Clear()
        {
            ApplicationData.Current.LocalSettings.DeleteContainer(EventHistory.KeyHistoryevents);
            ApplicationData.Current.RoamingSettings.DeleteContainer(EventHistory.KeyFireOnlyOnceActions);
            await ClearFiles("sensorberg-storage");
        }

        public static async Task ClearFiles(string folder)
        {
            try
            {
                await ClearFiles(await ApplicationData.Current.LocalFolder.GetFolderAsync(folder));
            }
            catch (FileNotFoundException)
            {
            }
        }

        public static async Task ClearFiles(StorageFolder folder)
        {
            try
            {
                foreach (IStorageItem item in await folder.GetItemsAsync())
                {
                    if (item.IsOfType(StorageItemTypes.Folder))
                    {
                        await ClearFiles((StorageFolder) item);
                    }
                    else
                    {
                        await item.DeleteAsync();
                    }
                }
            }
            catch (FileNotFoundException)
            {
            }
        }

        public static async Task RemoveFile(string keyLayoutContent)
        {
            try
            {
                StorageFile storageFile = await ApplicationData.Current.LocalFolder.GetFileAsync(keyLayoutContent);
                await storageFile.DeleteAsync();
            }
            catch (Exception)
            {
            }
        }

        public static Request ToRequest(string uuid, ushort man, ushort beaconId, BeaconEventType type)
        {
            return new Request(new BeaconEventArgs() {Beacon = new Beacon() {Id1 = uuid, Id2 = man, Id3 = beaconId }, EventType = type}, SdkData.NextId());
        }
    }
}