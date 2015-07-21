using Windows.ApplicationModel.Background;

namespace SensorbergSDKBackground
{
    /// <summary>
    /// Timer triggered background task for processing pending delayed actions.
    /// This is not part of the public API. Making modifications into background tasks is not required in order to use the SDK.
    /// </summary>
    public sealed class TimedBackgroundTask : IBackgroundTask
    {
        private BackgroundEngine _backgroundEngine;

        public TimedBackgroundTask()
        {
            _backgroundEngine = new BackgroundEngine();
            _backgroundEngine.Finished += OnFinished;
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            System.Diagnostics.Debug.WriteLine("TimedBackgroundTask.Run()");
            await _backgroundEngine.InitializeAsync(taskInstance);
            await _backgroundEngine.ProcessDelayedActionsAsync();
        }

        private void OnFinished(object sender, int e)
        {
            System.Diagnostics.Debug.WriteLine("TimedBackgroundTask.OnFinished()");
        }
    }
}
