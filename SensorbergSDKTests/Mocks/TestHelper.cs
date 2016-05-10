// Created by Kay Czarnotta on 21.04.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace SensorbergSDKTests.Mocks
{
    public class TestHelper
    {
        public static async Task ClearFiles(string folder)
        {
            try
            {
                await ClearFiles(await ApplicationData.Current.LocalFolder.GetFolderAsync(folder));
            }
            catch (FileNotFoundException )
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
            catch (Exception) { }
        }
    }
}