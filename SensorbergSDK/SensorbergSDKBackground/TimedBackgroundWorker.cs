using Windows.ApplicationModel.Background;

namespace SensorbergSDKBackground
{
    /// <summary>
    /// Timer triggered background task for processing pending delayed actions.
    /// This is not part of the public API. Making modifications into background tasks is not required in order to use the SDK.
    /// </summary>
    public class TimedBackgroundWorker 
    {
        protected BackgroundEngine BackgroundEngine { get; }

        public TimedBackgroundWorker()
        {
            BackgroundEngine = new BackgroundEngine();
            BackgroundEngine.Finished += OnFinished;
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            System.Diagnostics.Debug.WriteLine("TimedBackgroundWorker.Run()");
            await BackgroundEngine.InitializeAsync(taskInstance);
            await BackgroundEngine.ProcessDelayedActionsAsync();
        }

        private void OnFinished(object sender, int e)
        {
            System.Diagnostics.Debug.WriteLine("TimedBackgroundWorker.OnFinished()");
        }
    }
}
