// Created by Kay Czarnotta on 18.08.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.System.Profile;
using SensorbergSDK;
using SensorbergSDK.Internal.Data;

namespace SensorbergControlLibrary.Model
{
    public class SettingsControlModel : INotifyPropertyChanged
    {
        public event Action<string> ApiKeyChanged;
        private ResourceLoader loader = new ResourceLoader("ms-appx:///SensorbergSDK/Resources");
        private List<SensorbergApplication> _applications;
        private bool _showApiKeySelection;
        private string _apiKeyErrorMessage;
        private SensorbergApplication _application;
        private bool _isScannerAvailable;
        private bool _isValidatingOrFetchingApiKey;
        private bool _isApiKeyValid;
        private string _apiKey;
        private bool _showApiKeyErrorMessage;
        private bool _shouldRegisterBackgroundTask;
        private bool _isBackgroundTaskRegistered;

        public bool ShowApiKeyErrorMessage
        {
            get { return _showApiKeyErrorMessage; }
            set
            {
                _showApiKeyErrorMessage = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get { return GetSettingsString("email",""); }
            set
            {
                ApplicationData.Current.LocalSettings.Values["email"] = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get { return GetSettingsString("password", ""); }
            set
            {
                ApplicationData.Current.LocalSettings.Values["password"] = value;
                OnPropertyChanged();
            }
        }

        public List<SensorbergApplication> Applications
        {
            get { return _applications; }
            set
            {
                _applications = value;
                OnPropertyChanged();
            }
        }

        public bool ShowApiKeySelection
        {
            get { return _showApiKeySelection; }
            set
            {
                _showApiKeySelection = value;
                OnPropertyChanged();
            }
        }

        public string ApiKey
        {
            get { return _apiKey; }
            set
            {
                _apiKey = value;
                OnPropertyChanged();
            }
        }

        public bool IsApiKeyValid
        {
            get { return _isApiKeyValid; }
            set
            {
                _isApiKeyValid = value;
                OnPropertyChanged();
                if (IsApiKeyValid)
                {
                    ShowApiKeyErrorMessage = false;
                }
            }
        }

        public bool IsValidatingOrFetchingApiKey
        {
            get { return _isValidatingOrFetchingApiKey; }
            set
            {
                _isValidatingOrFetchingApiKey = value;
                OnPropertyChanged();
            }
        }

        public bool IsScannerAvailable
        {
            get { return _isScannerAvailable; }
            set
            {
                _isScannerAvailable = value;
                OnPropertyChanged();
            }
        }

        public object Application
        {
            get { return _application; }
            set
            {
                SensorbergApplication a = value as SensorbergApplication;
                if (a == null)
                {
                    return;
                }
                if (a != _application)
                {
                    _application = a;
                    OnPropertyChanged();
                    ApiKey = a.AppKey;
                    ApiKeyChanged?.Invoke(ApiKey);
                }
            }
        }

        public string ApiKeyErrorMessage
        {
            get { return _apiKeyErrorMessage; }
            set
            {
                _apiKeyErrorMessage = value;
                OnPropertyChanged();
            }
        }

        public bool ShouldRegisterBackgroundTask
        {
            get { return _shouldRegisterBackgroundTask; }
            set
            {
                _shouldRegisterBackgroundTask = value;
                OnPropertyChanged();
            }
        }

        public bool IsBackgroundTaskRegistered
        {
            get { return _isBackgroundTaskRegistered; }
            set
            {
                _isBackgroundTaskRegistered = value;
                OnPropertyChanged();
            }
        }
        public SettingsControlModel()
        {
            IsScannerAvailable = AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.IoT";
            IsApiKeyValid = true;
        }

        private string GetSettingsString(string key, string defaultvalue)
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                return ApplicationData.Current.LocalSettings.Values[key] as string;
            }
            return defaultvalue;
        }
        /// <summary>
        /// Validates the given API key.
        /// </summary>
        /// <param name="displayResultDialogInCaseOfFailure">If true, will display a result dialog in case of an error.</param>
        /// <returns>The API key validation result.</returns>
        public async Task<ApiKeyValidationResult> ValidateApiKeyAsync(bool displayResultDialogInCaseOfFailure = false)
        {
            IsValidatingOrFetchingApiKey = true;
            ApiKeyValidationResult result = await new ApiKeyHelper().ValidateApiKey(ApiKey);

            if (result == ApiKeyValidationResult.Valid)
            {
                IsApiKeyValid = true;
                ApiKeyChanged?.Invoke(ApiKey);
            }
            else
            {
                IsApiKeyValid = false;

                if (displayResultDialogInCaseOfFailure)
                {
                    string message = loader.GetString("unknownApiKeyValidationError");

                    switch (result)
                    {
                        case ApiKeyValidationResult.Invalid:
                            message = loader.GetString("invalidApiKey");
                            break;
                        case ApiKeyValidationResult.NetworkError:
                            message = loader.GetString("apiKeyValidationFailedDueToNetworkError");
                            break;
                    }

                    ApiKeyErrorMessage = message;
                    ShowApiKeyErrorMessage = true;
                }
            }
            IsValidatingOrFetchingApiKey = false;
            return result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}