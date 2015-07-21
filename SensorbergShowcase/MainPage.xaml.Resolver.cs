using SensorbergSDK;
using System;
using Windows.Foundation;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SensorbergShowcase
{
	/// <summary>
	/// Implementation for handling resolved beacon events and possible error situations.
	/// </summary>
	public sealed partial class MainPage : Page
	{
        public bool IsLayoutValid
        {
            get
            {
                return (bool)GetValue(IsLayoutValidProperty);
            }
            private set
            {
                SetValue(IsLayoutValidProperty, value);
            }
        }
        public static readonly DependencyProperty IsLayoutValidProperty =
            DependencyProperty.Register("IsLayoutValid", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));

        private IAsyncOperation<IUICommand> _beaconActionDialogOperation;

        /// <summary>
        /// Hooks the resolver specific events.
        /// </summary>
        private void HookResolverEvents()
        {
            _sdkManager.BeaconActionResolved += OnBeaconActionResolvedAsync;
            _sdkManager.FailedToResolveBeaconAction += OnFailedToResolveBeaconAction;
        }

        /// <summary>
        /// Unhooks the resolver specific events.
        /// </summary>
        private void UnhookResolverEvents()
        {
            _sdkManager.BeaconActionResolved -= OnBeaconActionResolvedAsync;
            _sdkManager.FailedToResolveBeaconAction -= OnFailedToResolveBeaconAction;
        }

        private void OnBeaconLayoutValidityChanged(object sender, bool e)
        {
            IsLayoutValid = e;
        }

        /// <summary>
        /// Displays a dialog and a toast notification corresponding to the given beacon action.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnBeaconActionResolvedAsync(object sender, BeaconAction e)
		{
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                MessageDialog messageDialog = e.ToMessageDialog();

                switch (e.Type)
                {
                    case BeaconActionType.UrlMessage:
                    case BeaconActionType.VisitWebsite:
                        messageDialog.Commands.Add(new UICommand("Yes",
                            new UICommandInvokedHandler(async (command) =>
                            {
                                await Windows.System.Launcher.LaunchUriAsync(new Uri(e.Url));
                                _beaconActionDialogOperation.Cancel();
                                _beaconActionDialogOperation = null;
                            })));

                        messageDialog.Commands.Add(new UICommand("No"));

                        _beaconActionDialogOperation = messageDialog.ShowAsync();
                        break;

                    case BeaconActionType.InApp:
                        await messageDialog.ShowAsync();
                        break;
                }
            });
        }

        /// <summary>
        /// Called when there was a failure in resolving the beacon action.
        /// 
        /// In most cases this event can be ignored. However, if you wish to act upon this,
        /// this is the place where to do it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The error message.</param>
        private void OnFailedToResolveBeaconAction(object sender, string e)
        {
            // No implementation
        }
    }
}
