using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using System;
using System.Threading;


namespace SensorBergTests
{
    [TestClass]
    public class IntegrationTest
    {
        ManualResetEvent _manualEvent = new ManualResetEvent(false);
        Beacon beacon = new Beacon();
        Resolver res = new Resolver();
        BeaconEventArgs args = new BeaconEventArgs();
        ResolvedActionsEventArgs _e = null;

        [TestMethod]
        public void Integration_connection()
        {
            SDKData.Instance.ApiKey = "db427f16996116144c206efc651885bd76c864e1d5c07691e1ab0157d976ffd4";
            beacon.Id1 = "7367672374000000ffff0000ffff0006";
            beacon.Id2 = 59242;
            beacon.Id3 = 27189;

            args.Beacon = beacon;
            args.EventType = BeaconEventType.Enter;
            res.ActionsResolved += Res_ActionResolved;
            res.CreateRequest(args);
            _manualEvent.WaitOne();

            Assert.IsNotNull(_e);
            Assert.IsTrue(_e.ResolvedActions.Count == 1);
        }

        [TestMethod]
        public void Integration_timeframes1()
        {
            SDKData.Instance.ApiKey = "db427f16996116144c206efc651885bd76c864e1d5c07691e1ab0157d976ffd4";
            beacon.Id1 = "7367672374000000ffff0000ffff0007";
            beacon.Id2 = 39187;
            beacon.Id3 = 58763; //Valid only in 2017, beacon

            args.Beacon = beacon;
            args.EventType = BeaconEventType.Enter;
            res.ActionsResolved += Res_ActionResolved;
            res.CreateRequest(args);
            _manualEvent.WaitOne();

            Assert.IsNotNull(_e);
            Assert.IsTrue(_e.ResolvedActions.Count == 1);

            var trueOffset = new DateTimeOffset(2017, 5, 1, 8, 6, 32, new TimeSpan(1, 0, 0));
            var falseOffset = new DateTimeOffset(2018, 5, 1, 8, 6, 32, new TimeSpan(1, 0, 0));

            Assert.IsTrue(_e.ResolvedActions[0].IsInsideTimeframes(trueOffset));
            Assert.IsFalse(_e.ResolvedActions[0].IsInsideTimeframes(falseOffset));

        }

        [TestMethod]
        public void Integration_timeframes2()
        {
            SDKData.Instance.ApiKey = "db427f16996116144c206efc651885bd76c864e1d5c07691e1ab0157d976ffd4";
            beacon.Id1 = "7367672374000000ffff0000ffff0003";
            beacon.Id2 = 48869;
            beacon.Id3 = 21321; //Three actions, beacon

            args.Beacon = beacon;
            args.EventType = BeaconEventType.Enter;
            res.ActionsResolved += Res_ActionResolved;
            res.CreateRequest(args);
            _manualEvent.WaitOne();

            Assert.IsNotNull(_e);
            Assert.IsTrue(_e.ResolvedActions.Count == 3);

            var trueOffset = new DateTimeOffset(2017, 5, 1, 8, 6, 32, new TimeSpan(1, 0, 0));
            var falseOffset = new DateTimeOffset(2013, 5, 1, 8, 6, 32, new TimeSpan(1, 0, 0));

            Assert.IsTrue(_e.ResolvedActions[0].IsInsideTimeframes(trueOffset));
            Assert.IsFalse(_e.ResolvedActions[0].IsInsideTimeframes(falseOffset));

        }

        [TestMethod]
        public void Integration_payload()
        {
            SDKData.Instance.ApiKey = "db427f16996116144c206efc651885bd76c864e1d5c07691e1ab0157d976ffd4";
            beacon.Id1 = "7367672374000000ffff0000ffff0006";

            beacon.Id2 = 23430;
            beacon.Id3 = 28018; //Payload is awesome

            args.Beacon = beacon;
            args.EventType = BeaconEventType.Enter;
            res.ActionsResolved += Res_ActionResolved;
            res.CreateRequest(args);
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
