using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDKTests.Mocks;

namespace SensorbergSDKTests
{
    [TestClass]
    public class IntegrationTest
    {
        ManualResetEvent _manualEvent = new ManualResetEvent(false);
        Beacon beacon = new Beacon();
        Resolver res = new Resolver(false);
        BeaconEventArgs args = new BeaconEventArgs();
        ResolvedActionsEventArgs _e = null;

        [TestInitialize]
        public async Task Setup()
        {
            ServiceManager.ReadOnlyForTests = false;
            ServiceManager.Clear();
            ServiceManager.ApiConnction = new MockApiConnection();
            ServiceManager.LayoutManager = new LayoutManager();
            ServiceManager.SettingsManager = new SettingsManager();
            ServiceManager.StorageService = new StorageService() {Storage = new MockStorage()};
            ServiceManager.ReadOnlyForTests = true;
        }

        [TestMethod]
        [Timeout(10000)]
        public async Task Integration_connection()
        {
            SdkData.Instance.ApiKey = "db427f16996116144c206efc651885bd76c864e1d5c07691e1ab0157d976ffd4";
            beacon.Id1 = "7367672374000000ffff0000ffff0006";
            beacon.Id2 = 59242;
            beacon.Id3 = 27189;

            args.Beacon = beacon;
            args.EventType = BeaconEventType.Enter;
            res.ActionsResolved += Res_ActionResolved;
            await res.CreateRequest(args);
            _manualEvent.WaitOne();

            Assert.IsNotNull(_e);
            Assert.IsTrue(_e.ResolvedActions.Count == 1);
        }

        [TestMethod]
        [Timeout(10000)]
        public async Task Integration_timeframes1()
        {
            SdkData.Instance.ApiKey = "db427f16996116144c206efc651885bd76c864e1d5c07691e1ab0157d976ffd4";
            beacon.Id1 = "7367672374000000ffff0000ffff0007";
            beacon.Id2 = 39187;
            beacon.Id3 = 58763; //Valid only in 2017, beacon

            args.Beacon = beacon;
            args.EventType = BeaconEventType.Enter;
            res.ActionsResolved += Res_ActionResolved;
            await res.CreateRequest(args);
            _manualEvent.WaitOne();

            Assert.IsNotNull(_e);
            Assert.IsTrue(_e.ResolvedActions.Count == 1);

            var trueOffset = new DateTimeOffset(2017, 5, 1, 8, 6, 32, new TimeSpan(1, 0, 0));
            var falseOffset = new DateTimeOffset(2018, 5, 1, 8, 6, 32, new TimeSpan(1, 0, 0));

            Assert.IsTrue(_e.ResolvedActions[0].IsInsideTimeframes(trueOffset));
            Assert.IsFalse(_e.ResolvedActions[0].IsInsideTimeframes(falseOffset));
        }

        [TestMethod]
        [Timeout(10000)]
        public async Task Integration_timeframes2()
        {
            SdkData.Instance.ApiKey = "db427f16996116144c206efc651885bd76c864e1d5c07691e1ab0157d976ffd4";
            beacon.Id1 = "7367672374000000ffff0000ffff0003";
            beacon.Id2 = 48869;
            beacon.Id3 = 21321; //Three actions, beacon

            args.Beacon = beacon;
            args.EventType = BeaconEventType.Enter;
            res.ActionsResolved += Res_ActionResolved;
            await res.CreateRequest(args);
            _manualEvent.WaitOne();

            Assert.IsNotNull(_e);
            Assert.IsTrue(_e.ResolvedActions.Count == 4);

            var trueOffset = new DateTimeOffset(2017, 5, 1, 8, 6, 32, new TimeSpan(1, 0, 0));
            var falseOffset = new DateTimeOffset(2013, 5, 1, 8, 6, 32, new TimeSpan(1, 0, 0));

            Assert.IsTrue(_e.ResolvedActions[0].IsInsideTimeframes(trueOffset));
            Assert.IsFalse(_e.ResolvedActions[0].IsInsideTimeframes(falseOffset));
        }

        [TestMethod]
        [Timeout(10000)]
        public async Task Integration_payload()
        {
            SdkData.Instance.ApiKey = "db427f16996116144c206efc651885bd76c864e1d5c07691e1ab0157d976ffd4";
            beacon.Id1 = "7367672374000000ffff0000ffff0006";

            beacon.Id2 = 23430;
            beacon.Id3 = 28018; //Payload is awesome

            args.Beacon = beacon;
            args.EventType = BeaconEventType.Enter;
            res.ActionsResolved += Res_ActionResolved;
            await res.CreateRequest(args);
            _manualEvent.WaitOne();

            Assert.IsNotNull(_e);
            Assert.IsTrue(_e.ResolvedActions.Count == 1);
        }

        private void Res_ActionResolved(object sender, ResolvedActionsEventArgs e)
        {
            _e = e;
            _manualEvent.Set();
        }

    }
}