using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SensorbergSDK;
using SensorbergSDK.Internal;
using System.Threading;
using SensorbergSDK.Internal.Services;
using SensorbergSDKTests.Mocks;


namespace SensorBergTests
{
    [TestClass]
    public class UnitTestResolver
    {
        ManualResetEvent _manualEvent = new ManualResetEvent(false);
        Beacon beacon = new Beacon();
        Resolver res = new Resolver();
        BeaconEventArgs args = new BeaconEventArgs();
        ResolvedActionsEventArgs _e = null;

        [TestInitialize]
        public void TestSetup()
        {
            ServiceManager.LayoutManager = new MockLayoutManager();
        }

        [TestMethod]
        public void resolver_test()
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

        private void Res_ActionResolved(object sender, ResolvedActionsEventArgs e)
        {
            _e = e;
            _manualEvent.Set();
        }
    }
}
