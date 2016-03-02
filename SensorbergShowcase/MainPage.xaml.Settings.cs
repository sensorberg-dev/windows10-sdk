using SensorbergSDK;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using System.ServiceModel.Channels;
using Windows.UI.Core;
using Windows.UI.Popups;
using SensorbergShowcase.Controls;

namespace SensorbergShowcase
{
	/// <summary>
	/// Code for the settings part of the main page. Here we also control the
    /// background task registration.
	/// </summary>
	public sealed partial class MainPage : Page
	{
        private const string KeyEnableActions = "enable_actions";
        private const string KeyApiKey = "api_key";
        private const string KeyEmail = "email";
        private const string KeyPassword = "password";
        private const string KeyBeaconId1 = "beacon_id1";
        private const string KeyBeaconId2 = "beaconId2";
        private const string KeyBeaconId3 = "beaconId3";

        private ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private ApiKeyHelper _apiKeyHelper = new ApiKeyHelper();
        private bool _enableActionsSwitchToggledByUser = true;
        private bool _apiKeyWasJustSuccessfullyFetchedOrReset = false;
	    private bool _messageDialogAlreadyOpen;
	    private bool _qrAlreadyFetched;
	    private QrCodeScanner _scanner;

        #region Properties (API key, email, password, background task status etc.)

        public string ApiKey
        {
            get
            {
                return (string)GetValue(ApiKeyProperty);
            }
            private set
            {
                SetValue(ApiKeyProperty, value);
            }
        }
        public static readonly DependencyProperty ApiKeyProperty =
            DependencyProperty.Register("ApiKey", typeof(string), typeof(MainPage),
                new PropertyMetadata(SDKManager.DemoApiKey));

        public string Email
        {
            get
            {
                return (string)GetValue(EmailProperty);
            }
            private set
            {
                SetValue(EmailProperty, value);
            }
        }
        public static readonly DependencyProperty EmailProperty =
            DependencyProperty.Register("Email", typeof(string), typeof(MainPage),
                new PropertyMetadata(string.Empty));

        public string Password
        {
            get
            {
                return (string)GetValue(PasswordProperty);
            }
            private set
            {
                SetValue(PasswordProperty, value);
            }
        }
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register("Password", typeof(string), typeof(MainPage),
                new PropertyMetadata(string.Empty));

        public bool AreActionsEnabled
        {
            get
            {
                return (bool)GetValue(AreActionsEnabledProperty);
            }
            private set
            {
                SetValue(AreActionsEnabledProperty, value);
            }
        }
        public static readonly DependencyProperty AreActionsEnabledProperty =
            DependencyProperty.Register("AreActionsEnabled", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));

        public bool IsApiKeyValid
        {
            get
            {
                return (bool)GetValue(IsApiKeyValidProperty);
            }
            private set
            {
                SetValue(IsApiKeyValidProperty, value);

                if (value && ShouldActionsBeEnabled)
                {
                    TryToReinitializeSDK();
                }
            }
        }
        public static readonly DependencyProperty IsApiKeyValidProperty =
            DependencyProperty.Register("IsApiKeyValid", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));

        public bool IsValidatingOrFetchingApiKey
        {
            get
            {
                return (bool)GetValue(IsValidatingOrFetchingApiKeyProperty);
            }
            private set
            {
                SetValue(IsValidatingOrFetchingApiKeyProperty, value);
            }
        }
        public static readonly DependencyProperty IsValidatingOrFetchingApiKeyProperty =
            DependencyProperty.Register("IsValidatingOrFetchingApiKey", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));



        public bool IsScannerAvailable
        {
            get { return (bool)GetValue(IsScannerAvailableProperty); }
            set { SetValue(IsScannerAvailableProperty, value); }
        }

        public static readonly DependencyProperty IsScannerAvailableProperty =
            DependencyProperty.Register("IsScannerAvailable", typeof(bool), typeof(MainPage),
                new PropertyMetadata(true));

        public bool IsBackgroundTaskRegistered
        {
            get
            {
                return (bool)GetValue(IsBackgroundTaskRegisteredProperty);
            }
            private set
            {
                SetValue(IsBackgroundTaskRegisteredProperty, value);
            }
        }
        public static readonly DependencyProperty IsBackgroundTaskRegisteredProperty =
            DependencyProperty.Register("IsBackgroundTaskRegistered", typeof(bool), typeof(MainPage),
                new PropertyMetadata(false));

        private bool ShouldActionsBeEnabled
        {
            get;
            set;
        }

        #endregion

        private void LoadApplicationSettings()
        {
            if (_localSettings.Values.ContainsKey(KeyEnableActions))
            {
                ShouldActionsBeEnabled = (bool)_localSettings.Values[KeyEnableActions];
            }

            if (_localSettings.Values.ContainsKey(KeyApiKey))
            {
                ApiKey = _localSettings.Values[KeyApiKey].ToString();
            }
            else
            {
                ApiKey = SDKManager.DemoApiKey;
            }

            if (_localSettings.Values.ContainsKey(KeyEmail))
            {
                Email = _localSettings.Values[KeyEmail].ToString();
            }

            if (_localSettings.Values.ContainsKey(KeyPassword))
            {
                Password = _localSettings.Values[KeyPassword].ToString();
            }

            if (_localSettings.Values.ContainsKey(KeyBeaconId1))
            {
                BeaconId1 = _localSettings.Values[KeyBeaconId1].ToString();
            }

            if (_localSettings.Values.ContainsKey(KeyBeaconId2))
            {
                BeaconId2 = _localSettings.Values[KeyBeaconId2].ToString();
            }

            if (_localSettings.Values.ContainsKey(KeyBeaconId3))
            {
                BeaconId3 = _localSettings.Values[KeyBeaconId3].ToString();
            }

            if (AreActionsEnabled != ShouldActionsBeEnabled)
            {
                _enableActionsSwitchToggledByUser = false;
                AreActionsEnabled = ShouldActionsBeEnabled;
            }

            IsBackgroundTaskRegistered = _sdkManager.IsBackgroundTaskRegistered;
        }

        /// <summary>
        /// Saves the application settings.
        /// </summary>
        /// <param name="key">If empty or null, will save all settings. Otherwise will save the 
        /// specific settings related to the given key.</param>
        private void SaveApplicationSettings(string key = null)
        {
            if (string.IsNullOrEmpty(key) || key.Equals(KeyEnableActions))
            {
                _localSettings.Values[KeyEnableActions] = ShouldActionsBeEnabled;
            }

            if (string.IsNullOrEmpty(key) || key.Equals(KeyApiKey))
            {
                _localSettings.Values[KeyApiKey] = ApiKey;
                _localSettings.Values[KeyEmail] = Email;
                _localSettings.Values[KeyPassword] = Password;
            }

            if (string.IsNullOrEmpty(key) || key.Equals(KeyBeaconId1))
            {
                _localSettings.Values[KeyBeaconId1] = BeaconId1;
                _localSettings.Values[KeyBeaconId2] = BeaconId2;
                _localSettings.Values[KeyBeaconId3] = BeaconId3;
            }
        }

        private void TryToReinitializeSDK()
        {
            if (_sdkManager != null)
            {
                _sdkManager.Deinitialize(false);
                _sdkManager.InitializeAsync(ApiKey);
                HookResolverEvents();
            }

            if (!AreActionsEnabled)
            {
                _enableActionsSwitchToggledByUser = false;
                AreActionsEnabled = true;
            }
        }

        /// <summary>
        /// Validates the API key.
        /// </summary>
        /// <param name="displayResultDialogInCaseOfFailure">If true, will display a result dialog in case of an error.</param>
        private async void ValidateApiKeyAsync(bool displayResultDialogInCaseOfFailure = false)
        {
            IsValidatingOrFetchingApiKey = true;

            ApiKeyValidationResult result = await _apiKeyHelper.ValidateApiKey(ApiKey);

            if (result == ApiKeyValidationResult.Valid)
            {
                IsApiKeyValid = true;
            }
            else
            {
                IsApiKeyValid = false;

                if (displayResultDialogInCaseOfFailure)
                {
                    string message = "Could not validate the API key due to unknown error.";

                    switch (result)
                    {
                        case ApiKeyValidationResult.Invalid:
                            message = "The API key is invalid.";
                            break;
                        case ApiKeyValidationResult.NetworkError:
                            message = "Failed to validate the API key due to a possible network error.";
                            break;
                    }

                    ShowInformationalMessageDialogAsync(message, "API key not validated");
                }
            }

            IsValidatingOrFetchingApiKey = false;
        }

        private void OnValidateApiKeyButtonClicked(object sender, RoutedEventArgs e)
        {
            ValidateApiKeyAsync(true);
        }

        private async void OnFetchApiKeyButtonClickedAsync(object sender, RoutedEventArgs e)
		{
            IsValidatingOrFetchingApiKey = true;

            if (!string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password))
			{
                FetchApiKeyResult result = await _apiKeyHelper.FetchApiKeyAsync(Email, Password);

                if (result == FetchApiKeyResult.Success)
                {
                    _apiKeyWasJustSuccessfullyFetchedOrReset = true;
                    ApiKey = _apiKeyHelper.ApiKey;
                    IsApiKeyValid = true;
                    SaveApplicationSettings();
                }
                else
                {
                    string message = "Could not fetch the API key due to unknown error.";

                    switch (result)
                    {
                        case FetchApiKeyResult.NetworkError:
                            message = "Failed to fetch the API key due to a possible network error.";
                            break;
                        case FetchApiKeyResult.AuthenticationFailed:
                            message = "Authentication failed. Please check your email address and password.";
                            break;
                        case FetchApiKeyResult.ParsingError:
                            message = "Failed to parse the server response.";
                            break;
                        case FetchApiKeyResult.NoWindowsCampains:
                            message = "No Windows campaigns available.";
                            break;
                    }

                    ShowInformationalMessageDialogAsync(message, "Could not fetch API key");
                }
			}

            IsValidatingOrFetchingApiKey = false;
		}

        private void OnResetToDemoApiKeyButtonClicked(object sender, RoutedEventArgs e)
        {
            _apiKeyWasJustSuccessfullyFetchedOrReset = true;
            ApiKey = SDKManager.DemoApiKey;
            IsApiKeyValid = true;
        }

	    private async void OnScanApiQrCodeButtonClicked(object sender, RoutedEventArgs e)
	    {
	        _qrAlreadyFetched = false;

            _scanner.Visibility = Visibility.Visible;
            await _scanner.StartScanningAsync();
	    }

        private void OnEnableActionsSwitchToggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch)
            {
                ToggleSwitch enableActionsSwitch = sender as ToggleSwitch;

                if (enableActionsSwitch.IsOn)
                {
                    TryToReinitializeSDK();
                }
                else
                {
                    UnhookResolverEvents();
                    _sdkManager.Deinitialize(false);
                }

                if (_enableActionsSwitchToggledByUser)
                {
                    ShouldActionsBeEnabled = enableActionsSwitch.IsOn;
                    SaveApplicationSettings(KeyEnableActions);
                }
                else
                {
                    _enableActionsSwitchToggledByUser = true;
                }
            }
        }

        private async void OnEnableBackgroundTaskSwitchToggledAsync(object sender, RoutedEventArgs e)
		{
            if (sender is ToggleSwitch && (sender as ToggleSwitch).IsOn)
            {
                if (string.IsNullOrEmpty(_sdkManager.ApiKey))
                {
                    _sdkManager.ApiKey = SDKManager.DemoApiKey;
                }

                BackgroundTaskRegistrationResult result = await _sdkManager.RegisterBackgroundTaskAsync();

				if (!result.success)
				{
                    string exceptionMessage = string.Empty;

                    if (result.exception != null)
                    {
                        exceptionMessage = ": " + result.exception.Message;
                    }

                    (sender as ToggleSwitch).IsOn = false;
                    ShowInformationalMessageDialogAsync(exceptionMessage, "Failed to register background task");
				}
            }
			else
			{
                _sdkManager.UnregisterBackgroundTask();
			}

            IsBackgroundTaskRegistered = _sdkManager.IsBackgroundTaskRegistered;
		}

		private async void OnSettingsTextBoxTextChanged(object sender, TextChangedEventArgs e)
		{
            if (sender is TextBox)
            {
                string textBoxName = (sender as TextBox).Name.ToLower();
                string text = (sender as TextBox).Text;

                if (textBoxName.StartsWith("apikey"))
                {
                    ApiKey = text;

                    if (_apiKeyWasJustSuccessfullyFetchedOrReset)
                    {
                        _apiKeyWasJustSuccessfullyFetchedOrReset = false;
                    }
                    else
                    {
                        IsApiKeyValid = false;
                    }

                    await _sdkManager.InvalidateCacheAsync();
                }
                else if (textBoxName.StartsWith("email"))
                {
                    Email = text;
                }

                SaveApplicationSettings(KeyApiKey);
            }
        }

        private void OnPasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox)
            {
                Password = (sender as PasswordBox).Password;
                SaveApplicationSettings(KeyApiKey);
            }
        }

        private async void OnQrCodeResolved(object sender, string e)
        {
            if (_messageDialogAlreadyOpen || _qrAlreadyFetched)
            {
                return;
            }

            MessageDialog dialog = new MessageDialog(string.Format("Do you want to set api key to: {0} ?", e), "Qr code resolved");

            dialog.Commands.Add(new UICommand("Yes", async command =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                 {
                     _scanner.Visibility = Visibility.Collapsed;
                     await _scanner.StopScanningAsync();
                     ApiKey = e;
                 });
                _qrAlreadyFetched = true;
            }));

            dialog.Commands.Add(new UICommand("Scan again", command =>
            {
            }));
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
            _messageDialogAlreadyOpen = true;
                await dialog.ShowAsync();
            _messageDialogAlreadyOpen = false;
            });

        }

        private void OnCodeScannerLoaded(object sender, RoutedEventArgs e)
	    {
            _scanner = sender as QrCodeScanner;
	    }

	    private void OnScannerNotAvailable(object sender, EventArgs e)
	    {
	        IsScannerAvailable = false;
	    }

	    private async void OnBackRequested(object sender, BackRequestedEventArgs e)
	    {
	        e.Handled = true;
            _scanner.Visibility = Visibility.Collapsed;
            await _scanner.StopScanningAsync();
        }
	}
}