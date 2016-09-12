using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Practices.Unity;
using Prism.Unity.Windows;
using Prism.Windows.Navigation;
using SensorbergControlLibrary.Model;
using SensorbergSDK;
using SensorbergSDK.Internal.Services;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace SensorbergControlLibrary.Controls
{
    public sealed partial class SettingsControl : UserControl
    {
        private ResourceLoader loader = new ResourceLoader();
        public static readonly DependencyProperty ApiKeyProperty = DependencyProperty.Register("ApiKey", typeof(string), typeof(SettingsControl), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty BeaconNotificationEnableProperty = DependencyProperty.Register("BeaconNotificationEnable", typeof(bool), typeof(SettingsControl), new PropertyMetadata(default(bool)));
        private SettingsControlModel Model { get; } = new SettingsControlModel();

        public string ApiKey
        {
            get { return Model.ApiKey; }
            set { Model.ApiKey = value; }
        }

        public bool BeaconNotificationEnable
        {
            get { return Model.AreActionsEnabled; }
            set { Model.AreActionsEnabled = value; }
        }

        public event Action<string> ApiKeyChanged
        {
            add { Model.ApiKeyChanged += value; }
            remove { Model.ApiKeyChanged -= value; }
        }

        public event Action<bool> BeaconNotificationChanged;

        public SettingsControl()
        {
            InitializeComponent();
            Model.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "AreActionsEnabled")
                {
                    BeaconNotificationChanged?.Invoke(Model.AreActionsEnabled);
                }
            };
        }

        private async void OnValidateApiKeyButtonClicked(object sender, RoutedEventArgs e)
        {
            await Model.ValidateApiKeyAsync(true);
        }

        private void OnScanApiQrCodeButtonClicked(object sender, RoutedEventArgs e)
        {
            PrismUnityApplication.Current.Container.Resolve<INavigationService>().Navigate("QrCodeScanner", null);
        }
        private async void OnEnableBackgroundTaskSwitchToggledAsync(object sender, RoutedEventArgs e)
        {
            SDKManager sdkManager = SDKManager.Instance();
            if (sender is ToggleSwitch && (sender as ToggleSwitch).IsOn)
            {
                BackgroundTaskRegistrationResult result = await sdkManager.RegisterBackgroundTaskAsync();

                if (!result.Success)
                {
                    (sender as ToggleSwitch).IsOn = false;
                }
            }
            else
            {
                sdkManager.UnregisterBackgroundTask();
            }

            Model.IsBackgroundTaskRegistered = sdkManager.IsBackgroundTaskRegistered;
        }

        private async void OnFetchApiKeyButtonClickedAsync(object sender, RoutedEventArgs e)
        {
            Model.IsValidatingOrFetchingApiKey = true;
            if (!string.IsNullOrEmpty(Model.Email) && !string.IsNullOrEmpty(Model.Password))
            {
                ApiKeyHelper apiKeyHelper = new ApiKeyHelper();
                NetworkResult result = await apiKeyHelper.FetchApiKeyAsync(Model.Email, Model.Password);

                if (result == NetworkResult.Success)
                {
                    Model.ShowApiKeyErrorMessage = false;

                    Model.Applications = apiKeyHelper.Applications;
                    if (apiKeyHelper.Applications?.Count > 1)
                    {
                        Model.ShowApiKeySelection = apiKeyHelper.Applications.Count > 1;
                    }
                    else
                    {
//                        Model.ApiKey = apiKeyHelper.ApiKey;
//                        Model.IsApiKeyValid = true;
                        if (apiKeyHelper.Applications?.Count > 0)
                        {
                            Model.Application = apiKeyHelper.Applications[0];
                        }
                    }
                }
                else
                {
                    Model.IsApiKeyValid = false;
                    string message = loader.GetString("unknownFetchApiKeyError");

                    switch (result)
                    {
                        case NetworkResult.NetworkError:
                            message = loader.GetString("failedToFetchApiKeyDueToNetworkError");
                            break;
                        case NetworkResult.AuthenticationFailed:
                            message = loader.GetString("authenticationFailedForFetchingApiKey");
                            break;
                        case NetworkResult.ParsingError:
                            message = loader.GetString("failedToParseServerResponse");
                            break;
                        case NetworkResult.NoWindowsCampains:
                            message = loader.GetString("noWindowsCampaignsAvailable");
                            break;
                    }

                    Model.ApiKeyErrorMessage = message;
                    Model.ShowApiKeyErrorMessage = true;
                }
            }
            Model.IsValidatingOrFetchingApiKey = false;
        }
    }
}
