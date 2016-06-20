using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Internal.Transport;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class UnitTestEventHistory
    {
        [TestInitialize]
        public async Task Setup()
        {
            await TestHelper.ClearFiles("sensorberg-storage");
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new MockApiConnection();
            ServiceManager.StorageService = new StorageService() {Storage = new MockStorage()};
            await ServiceManager.StorageService.InitStorage();
            ServiceManager.ReadOnlyForTests = true;
        }

        [TestMethod]
        public async Task EventHistory_ShouldSupress()
        {
            SdkData.ApiKey = "540aa95ccf215718295c2c563a2090676994f09927f09a6e09a67c83be10b00c";
            var beacon = new Beacon();
            beacon.Id1 = "7367672374000000ffff0000ffff0007";
            beacon.Id2 = 8008;
            beacon.Id3 = 5;
            beacon.Timestamp = DateTimeOffset.Now;
            var args = new BeaconEventArgs();
            args.Beacon = beacon;
            args.EventType = BeaconEventType.Exit;
            var resolvedActionEventArgs = new ResolvedActionsEventArgs() {BeaconPid = beacon.Pid, BeaconEventType = BeaconEventType.Enter};

            BeaconAction beaconaction1 = new BeaconAction() {Body = "body", Url = "http://www.com", Uuid = "1223"};
            BeaconAction beaconaction2 = new BeaconAction() {Body = "body", Url = "http://www.com", Uuid = "5678"};
            BeaconAction beaconaction3 = new BeaconAction() {Body = "body", Url = "http://www.com", Uuid = "9678"};
            ResolvedAction res1 = new ResolvedAction() {SuppressionTime = 100, SendOnlyOnce = true, BeaconAction = beaconaction1};
            ResolvedAction res2 = new ResolvedAction() {SuppressionTime = 100, SendOnlyOnce = true, BeaconAction = beaconaction2};
            ResolvedAction res3 = new ResolvedAction() {SuppressionTime = 1, SendOnlyOnce = true, BeaconAction = beaconaction3};

            EventHistory eventHistory = new EventHistory();

            await eventHistory.SaveBeaconEventAsync(args, null);
            await eventHistory.SaveExecutedResolvedActionAsync(resolvedActionEventArgs, beaconaction1);
            await eventHistory.SaveExecutedResolvedActionAsync(resolvedActionEventArgs, beaconaction3);
            await Task.Delay(2000);

            bool shouldSupress1 = await eventHistory.ShouldSupressAsync(res1);
            bool shouldSupress2 = await eventHistory.ShouldSupressAsync(res2);
            bool shouldSupress3 = await eventHistory.ShouldSupressAsync(res3);

            Assert.IsTrue(shouldSupress1);
            Assert.IsFalse(shouldSupress2);
            Assert.IsFalse(shouldSupress3); //Supression time should be over
        }

        [TestMethod]
        public async Task EventHistory_FlushHistory()
        {
            SdkData.ApiKey = "540aa95ccf215718295c2c563a2090676994f09927f09a6e09a67c83be10b00c";
            var beacon = new Beacon();
            beacon.Id1 = "7367672374000000ffff0000ffff0007";
            beacon.Id2 = 8008;
            beacon.Id3 = 5;
            beacon.Timestamp = DateTimeOffset.Now;
            var args = new BeaconEventArgs();
            args.Beacon = beacon;
            args.EventType = BeaconEventType.Exit;
            var resolvedActionEventArgs = new ResolvedActionsEventArgs() {BeaconPid = beacon.Pid, BeaconEventType = BeaconEventType.Enter};

            BeaconAction beaconaction1 = new BeaconAction() {Body = "body", Url = "http://www.com", Uuid = "1223"};
            BeaconAction beaconaction2 = new BeaconAction() {Body = "body", Url = "http://www.com", Uuid = "5678"};
            BeaconAction beaconaction3 = new BeaconAction() {Body = "body", Url = "http://www.com", Uuid = "9678"};
            ResolvedAction res1 = new ResolvedAction() {SuppressionTime = 100, SendOnlyOnce = true, BeaconAction = beaconaction1};
            ResolvedAction res2 = new ResolvedAction() {SuppressionTime = 100, SendOnlyOnce = true, BeaconAction = beaconaction2};
            ResolvedAction res3 = new ResolvedAction() {SuppressionTime = 1, SendOnlyOnce = true, BeaconAction = beaconaction3};

            EventHistory eventHistory = new EventHistory();

            await eventHistory.SaveBeaconEventAsync(args, null);
            await eventHistory.SaveExecutedResolvedActionAsync(resolvedActionEventArgs, beaconaction1);
            await eventHistory.SaveExecutedResolvedActionAsync(resolvedActionEventArgs, beaconaction3);

            await eventHistory.FlushHistoryAsync();
        }

    }
}
