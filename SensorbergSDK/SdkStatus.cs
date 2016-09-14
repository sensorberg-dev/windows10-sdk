// Created by Kay Czarnotta on 01.09.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Devices.Radios;
using Windows.Media.Streaming.Adaptive;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Internal.Utils;

namespace SensorbergSDK
{
    public class SdkStatus : INotifyPropertyChanged, IDisposable
    {
        private readonly Timer _updateTimer;


        private bool? _isBluetoothEnabled;
        private bool? _isLocationEnabled;
        private bool? _isApiKeyValid;
        private bool? _isResolverReachable;
        private DateTimeOffset _lastUpdate;

        public SdkStatus()
        {
            _updateTimer = new Timer(UpdateTick, null, 10000, 10000);
            ServiceManager.LocationService.Locator.StatusChanged += OnStatusChanged;
        }

        private void OnStatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            switch (args.Status)
            {
                case PositionStatus.Ready:
                case PositionStatus.Initializing:
                case PositionStatus.NoData:
                    IsLocationEnabled = true;
                    break;
                case PositionStatus.NotInitialized:
                    break;
                case PositionStatus.Disabled:
                case PositionStatus.NotAvailable:
                    IsLocationEnabled = false;
                    break;
            }
        }

        /// <summary>
        /// Version of the SDK.
        /// </summary>
        public string SDKVersion
        {
            get
            {
                var versionExpression = new Regex("Version=(?<version>[0-9.]*)");
                var match = versionExpression.Match(typeof(SDKManager).AssemblyQualifiedName);
                return match.Success ? match.Groups["version"].Value : null;
            }
        }

        /// <summary>
        /// Check for ApiKey validation.
        /// </summary>
        public async Task<bool> CheckApiKeysValid()
        {
            ApiKeyHelper helper = new ApiKeyHelper();
            return IsApiKeyValid = await helper.ValidateApiKey(null) == ApiKeyValidationResult.Valid;
        }

        /// <summary>
        /// Check for reachability of resolver.
        /// </summary>
        public async Task<bool> CheckResolversReachable()
        {
            if (ServiceManager.ApiConnction == null)
            {
                return false;
            }

            NetworkResult result = ServiceManager.ApiConnction.LastCallResult;

            if (result == NetworkResult.UnknownError)
            {
                await ServiceManager.ApiConnction.LoadSettings();
                result = ServiceManager.ApiConnction.LastCallResult;
            }
            return IsResolverReachable = result != NetworkResult.NetworkError && result != NetworkResult.UnknownError;
        }

        /// <summary>
        /// If bluetooth is enabled.
        /// </summary>
        public bool IsBluetoothEnabled
        {
            get
            {
                if (_isBluetoothEnabled == null)
                {
                    _isBluetoothEnabled = false;
                    CheckIsBluetoothEnabled().ConfigureAwait(false);
                }
                return _isBluetoothEnabled.Value;
            }
            set
            {
                _isBluetoothEnabled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Check for bluetooth status and update property.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> CheckIsBluetoothEnabled()
        {
            var radios = await Radio.GetRadiosAsync();
            var bluetoothRadio = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
            return IsBluetoothEnabled = bluetoothRadio != null && bluetoothRadio.State == RadioState.On;
        }


        /// <summary>
        /// ApiKey validation state.
        /// </summary>
        public bool IsApiKeyValid
        {
            get
            {
                if (_isApiKeyValid == null)
                {
                    _isApiKeyValid = false;
                    CheckApiKeysValid().ConfigureAwait(false);
                }
                return _isApiKeyValid.Value;
            }
            set
            {
                _isApiKeyValid = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Resolver reachable state.
        /// </summary>
        public bool IsResolverReachable
        {
            get
            {
                if (_isResolverReachable == null)
                {
                    _isApiKeyValid = false;
                    CheckResolversReachable().ConfigureAwait(false);
                }
                return _isResolverReachable.Value;
            }
            set
            {
                _isResolverReachable = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Location enable state.
        /// </summary>
        public bool IsLocationEnabled
        {
            get
            {
                if (_isLocationEnabled == null)
                {
                    _isLocationEnabled = false;
                    CheckLocationEnabled().ConfigureAwait(false);
                }
                return _isLocationEnabled.Value;
            }
            set
            {
                _isLocationEnabled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Check for location permissions.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> CheckLocationEnabled()
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            return
                IsLocationEnabled =
                    accessStatus == GeolocationAccessStatus.Allowed && ServiceManager.LocationService.Locator.LocationStatus != PositionStatus.NotAvailable &&
                    ServiceManager.LocationService.Locator.LocationStatus != PositionStatus.Disabled;
        }

        /// <summary>
        /// Last update time of layout.
        /// </summary>
        public DateTimeOffset LastUpdate
        {
            get { return _lastUpdate; }
            set
            {
                _lastUpdate = value;
                OnPropertyChanged();
            }
        }


        private async void UpdateTick(object state)
        {
            await CheckLocationEnabled();
            await CheckApiKeysValid();
            await CheckIsBluetoothEnabled();
            await CheckResolversReachable();
            CheckLastLayoutUpdate();
        }

        private void CheckLastLayoutUpdate()
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(StorageService.KeyLayoutRetrievedTime))
            {
                LastUpdate = (DateTimeOffset) ApplicationData.Current.LocalSettings.Values[StorageService.KeyLayoutRetrievedTime];
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual async void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Helper.BeginInvoke(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }

        public void Dispose()
        {
            _updateTimer?.Dispose();
        }
    }
}