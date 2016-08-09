// Created by Kay Czarnotta on 09.08.2016
// 
// Copyright (c) 2016,  EagleEye
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK.Internal.Services;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class QueueFileWriterTest
    {
        [TestInitialize]
        public async Task Setup()
        {
            await TestHelper.RemoveFile("test.txt");
        }

        [TestMethod]
        public async Task TestWriteFile()
        {
            TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();

            IQueuedFileWriter fileWriter = new QueuedFileWriter(ApplicationData.Current.LocalFolder, "test.txt");
            await fileWriter.WriteLine("1");
            await fileWriter.WriteLine("2");
            await fileWriter.WriteLine("3");
            fileWriter.QueueEmpty += () => source.SetResult(true);
            await fileWriter.WriteLine("4");
            await source.Task;
            List<string> list = new List<string>() {"1","2","3","4"};
            List<string> collection = (await FileIO.ReadLinesAsync(await ApplicationData.Current.LocalFolder.CreateFileAsync("test.txt",CreationCollisionOption.OpenIfExists))).ToList();
            Assert.AreEqual(list.Count, collection.Count, "Not 4 elements");
            CollectionAssert.AreEqual(list, collection);
        }

        [TestMethod]
        public async Task TestMultipleWriteFile()
        {
            TaskCompletionSource<bool> source1 = new TaskCompletionSource<bool>();

            IQueuedFileWriter fileWriter = new QueuedFileWriter(ApplicationData.Current.LocalFolder, "test.txt");
            await fileWriter.WriteLine("1");
            await fileWriter.WriteLine("2");
            await fileWriter.WriteLine("3");
            fileWriter.QueueEmpty += () => source1.TrySetResult(true);
            await fileWriter.WriteLine("4");
            await source1.Task;
            List<string> list = new List<string>() {"1", "2", "3", "4"};
            StorageFile storageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("test.txt", CreationCollisionOption.OpenIfExists);
            List<string> collection = (await FileIO.ReadLinesAsync(storageFile)).ToList();
            Assert.AreEqual(list.Count, collection.Count, "Not 4 elements");
            CollectionAssert.AreEqual(list, collection);

            //second write on same object
            TaskCompletionSource<bool> source2 = new TaskCompletionSource<bool>();

            await fileWriter.WriteLine("1");
            await fileWriter.WriteLine("2");
            await fileWriter.WriteLine("3");
            fileWriter.QueueEmpty += () => source2.TrySetResult(true);
            await fileWriter.WriteLine("4");
            await source2.Task;

            list = new List<string>() {"1", "2", "3", "4", "1", "2", "3", "4"};

            collection = (await FileIO.ReadLinesAsync(storageFile)).ToList();
            Assert.AreEqual(list.Count, collection.Count, "Not 8 elements");
            CollectionAssert.AreEqual(list, collection);

            //thrid write on new object
            TaskCompletionSource<bool> source3 = new TaskCompletionSource<bool>();

            fileWriter.Dispose();
            fileWriter = new QueuedFileWriter(ApplicationData.Current.LocalFolder, "test.txt");
            await fileWriter.WriteLine("1");
            await fileWriter.WriteLine("2");
            await fileWriter.WriteLine("3");
            fileWriter.QueueEmpty += () => source3.TrySetResult(true);
            await fileWriter.WriteLine("4");
            await source3.Task;
            list = new List<string>() {"1", "2", "3", "4", "1", "2", "3", "4", "1", "2", "3", "4"};
            collection = (await FileIO.ReadLinesAsync(storageFile)).ToList();
            Assert.AreEqual(list.Count, collection.Count, "Not 12 elements");
            CollectionAssert.AreEqual(list, collection);
        }

        [TestMethod]
        public async Task TestReadWrittenFile()
        {
            TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();

            IQueuedFileWriter fileWriter = new QueuedFileWriter(ApplicationData.Current.LocalFolder, "test.txt");
            await fileWriter.WriteLine("1");
            await fileWriter.WriteLine("2");
            await fileWriter.WriteLine("3");
            fileWriter.QueueEmpty += () => source.SetResult(true);
            await fileWriter.WriteLine("4");
            await source.Task;
            List<string> list = new List<string>() { "1", "2", "3", "4" };
            List<string> collection = (await FileIO.ReadLinesAsync(await ApplicationData.Current.LocalFolder.CreateFileAsync("test.txt", CreationCollisionOption.OpenIfExists))).ToList();
            Assert.AreEqual(list.Count, collection.Count, "Not 4 elements");
            CollectionAssert.AreEqual(list, collection);


            collection = await fileWriter.ReadLines();
            Assert.AreEqual(list.Count, collection.Count, "Not 4 elements");
            CollectionAssert.AreEqual(list, collection);
        }

        [TestMethod]
        public async Task TestReadCacheFile()
        {
            TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();
            IQueuedFileWriter fileWriter = new QueuedFileWriter(ApplicationData.Current.LocalFolder, "test.txt");
            List<string> list = new List<string>() ;
            for (int i = 0; i < 40; i++)
            {
                if (i >= 39)
                {
                    fileWriter.QueueEmpty += () => source.SetResult(true);
                }
                await fileWriter.WriteLine(i.ToString());
                list.Add(i.ToString());
            }
            await source.Task;
            List<string> collection = await fileWriter.ReadLines();
            Assert.AreEqual(list.Count, collection.Count, "Not 40 elements");
            CollectionAssert.AreEqual(list, collection);
        }
        [TestMethod]
        public async Task TestReadWriteFile()
        {
            IQueuedFileWriter fileWriter = new QueuedFileWriter(ApplicationData.Current.LocalFolder, "test.txt");
            List<string> list = new List<string>();
            for (int i = 0; i < 40; i++)
            {
                await fileWriter.WriteLine(i.ToString());
                list.Add(i.ToString());
            }
            Assert.IsNotNull(await fileWriter.ReadLines());
        }
    }
}