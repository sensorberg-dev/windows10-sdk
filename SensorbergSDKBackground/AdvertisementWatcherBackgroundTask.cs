using Windows.ApplicationModel.Background;

namespace SensorbergSDKBackground
{
    /// <summary>
    /// Background task for watching BLE advertisement events.
    /// This is not part of the public API. Making modifications into background tasks is not required in order to use the SDK.
    /// </summary>
    public sealed class AdvertisementWatcherBackgroundTask : IBackgroundTask
	{
        BackgroundEngine _backgroundEngine;

        public AdvertisementWatcherBackgroundTask()
        {
            _backgroundEngine = new BackgroundEngine();
            _backgroundEngine.Finished += OnFinished;
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
		{
            System.Diagnostics.Debug.WriteLine("AdvertisementWatcherBackgroundTask.Run()");
            await _backgroundEngine.InitializeAsync(taskInstance);
            await _backgroundEngine.ResolveBeaconActionsAsync();
        }

        private void OnFinished(object sender, int e)
        {
            System.Diagnostics.Debug.WriteLine("AdvertisementWatcherBackgroundTask.OnFinished()");
            _backgroundEngine.Finished -= OnFinished;
        }
    }
}