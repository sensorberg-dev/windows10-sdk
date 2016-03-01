using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorBergTests
{
    [TestClass]
    public class UnitTestEventHistory
    {
        [TestMethod]
        public async Task EventHistory_ShouldSupress()
        {
            
            SDKData.Instance.ApiKey = "540aa95ccf215718295c2c563a2090676994f09927f09a6e09a67c83be10b00c";
            await Storage.Instance.CreateDBAsync();
            var beacon = new Beacon();
            beacon.Id1 = "7367672374000000ffff0000ffff0007";
            beacon.Id2 = 8008;
            beacon.Id3 = 5;
            beacon.Timestamp = DateTimeOffset.Now;
            var args = new BeaconEventArgs();
            args.Beacon = beacon;
            args.EventType = BeaconEventType.Exit;
            var resolvedActionEventArgs = new ResolvedActionsEventArgs() { BeaconPid = beacon.Pid, BeaconEventType = BeaconEventType.Enter };

            BeaconAction beaconaction1 = new BeaconAction() { Body = "body", Url = "http://www.com",  Uuid = "1223" };
            BeaconAction beaconaction2 = new BeaconAction() { Body = "body", Url = "http://www.com",  Uuid = "5678" };
            BeaconAction beaconaction3 = new BeaconAction() { Body = "body", Url = "http://www.com",  Uuid = "9678" };
            ResolvedAction res1 = new ResolvedAction() { SupressionTime = 100, SendOnlyOnce = true, BeaconAction = beaconaction1 };
            ResolvedAction res2 = new ResolvedAction() { SupressionTime = 100, SendOnlyOnce = true, BeaconAction = beaconaction2 };
            ResolvedAction res3 = new ResolvedAction() { SupressionTime = 1, SendOnlyOnce = true, BeaconAction = beaconaction3 };

            EventHistory eventHistory = new EventHistory();

            await eventHistory.SaveBeaconEventAsync(args);
            await eventHistory.SaveExecutedResolvedActionAsync(resolvedActionEventArgs, beaconaction1);
            await eventHistory.SaveExecutedResolvedActionAsync(resolvedActionEventArgs, beaconaction3);
            await Task.Delay(2000);

            bool shouldSupress1 = await eventHistory.ShouldSupressAsync(res1);
            bool shouldSupress2 = await eventHistory.ShouldSupressAsync(res2);
            bool shouldSupress3 = await eventHistory.ShouldSupressAsync(res3);

            Assert.IsTrue(shouldSupress1);
            Assert.IsFalse(shouldSupress2);
            Assert.IsFalse(shouldSupress3);//Supression time should be over
           
        }

        [TestMethod]
        public async Task EventHistory_FlushHistory()
        {

            SDKData.Instance.ApiKey = "540aa95ccf215718295c2c563a2090676994f09927f09a6e09a67c83be10b00c";
            await Storage.Instance.CreateDBAsync();
            var beacon = new Beacon();
            beacon.Id1 = "7367672374000000ffff0000ffff0007";
            beacon.Id2 = 8008;
            beacon.Id3 = 5;
            beacon.Timestamp = DateTimeOffset.Now;
            var args = new BeaconEventArgs();
            args.Beacon = beacon;
            args.EventType = BeaconEventType.Exit;
            var resolvedActionEventArgs = new ResolvedActionsEventArgs() { BeaconPid = beacon.Pid, BeaconEventType = BeaconEventType.Enter };

            BeaconAction beaconaction1 = new BeaconAction() { Body = "body", Url = "http://www.com", Uuid = "1223" };
            BeaconAction beaconaction2 = new BeaconAction() { Body = "body", Url = "http://www.com", Uuid = "5678" };
            BeaconAction beaconaction3 = new BeaconAction() { Body = "body", Url = "http://www.com", Uuid = "9678" };
            ResolvedAction res1 = new ResolvedAction() { SupressionTime = 100, SendOnlyOnce = true, BeaconAction = beaconaction1 };
            ResolvedAction res2 = new ResolvedAction() { SupressionTime = 100, SendOnlyOnce = true, BeaconAction = beaconaction2 };
            ResolvedAction res3 = new ResolvedAction() { SupressionTime = 1, SendOnlyOnce = true, BeaconAction = beaconaction3 };

            EventHistory eventHistory = new EventHistory();

            await eventHistory.SaveBeaconEventAsync(args);
            await eventHistory.SaveExecutedResolvedActionAsync(resolvedActionEventArgs, beaconaction1);
            await eventHistory.SaveExecutedResolvedActionAsync(resolvedActionEventArgs, beaconaction3);

            await eventHistory.FlushHistoryAsync();


        }

    }
}
