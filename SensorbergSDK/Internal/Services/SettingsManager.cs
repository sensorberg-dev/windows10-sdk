﻿// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using MetroLog;
using Newtonsoft.Json;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Services;

namespace SensorbergSDK.Internal.Services
{
    public sealed class SettingsManager: ISettingsManager, IDisposable
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<SettingsManager>();
        private const string StorageKey = "app_settings";
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private readonly SdkData _sdkData;
        private Timer _updateSettingsTimer;
        private AppSettings _lastSettings;

        public event EventHandler<SettingsEventArgs> SettingsUpdated;
        public AppSettings DefaultAppSettings { get; set; }


        public SettingsManager()
        {
            _sdkData = SdkData.Instance;
        }

        public async Task<AppSettings> GetSettings(bool forceUpdate = false)
        {
            if (_lastSettings != null && !forceUpdate)
            {
                Logger.Debug("SettingsManager returned settings from cache." + _lastSettings);
                return _lastSettings;
            }

            var settings = (await GetSettingsFromApiAsync() ?? GetSettingsFromStorage()) ?? CreateDefaultSettings();

            InitTimer(settings.SettingsUpdateInterval);

            _lastSettings = settings;
            return settings;
        }

        private void InitTimer(ulong miliseconds)
        {
            TimeSpan interval = TimeSpan.FromMilliseconds(miliseconds);
            if (_updateSettingsTimer != null)
            {
                _updateSettingsTimer.Change(interval, interval);
                return;
            }

            _updateSettingsTimer = new Timer(OnTimerTick,null, interval, interval);
        }

        private async void OnTimerTick(object state)
        {
            var settings = await GetSettings(true);
            if (SettingsUpdated != null && settings != null)
            {
                SettingsUpdated(this, new SettingsEventArgs(settings));
            }
        }

        private async Task<AppSettings> GetSettingsFromApiAsync()
        {

            try
            {
                var responseMessage = await ServiceManager.ApiConnction.LoadSettings(_sdkData);
                if (string.IsNullOrEmpty(responseMessage))
                {
                    return null;
                }

                var settings = JsonConvert.DeserializeObject<AppSettingsResponse>(responseMessage);

                SaveSettingsToStorage(settings.Settings);

                Logger.Debug("Got settings from api. " + settings.Settings);

                return settings.Settings;
            }
            catch (Exception ex)
            {
                Logger.Debug("SettingsManager.GetSettingsFromApiAsync(): Failed to send HTTP request: " + ex.Message);
                return null;
            }
        }

        private AppSettings CreateDefaultSettings()
        {
            Logger.Debug("SettingsManager used default settings values.");
            return DefaultAppSettings != null ? DefaultAppSettings : new AppSettings();
        }

        private void SaveSettingsToStorage(AppSettings settings)
        {
            _localSettings.Values[StorageKey] = JsonConvert.SerializeObject(settings);
        }

        private AppSettings GetSettingsFromStorage()
        {
            var storageValue = _localSettings.Values[StorageKey];

            var storageString = storageValue?.ToString();

            if (string.IsNullOrEmpty(storageString))
            {
                return null;
            }
            return JsonConvert.DeserializeObject<AppSettings>(storageString);
        }

        public void Dispose()
        {
            _updateSettingsTimer?.Dispose();
        }
    }
}
