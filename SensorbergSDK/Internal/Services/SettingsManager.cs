// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using MetroLog;
using Newtonsoft.Json;
using SensorbergSDK.Services;

namespace SensorbergSDK.Internal.Services
{
    /// <summary>
    /// Implementation of the SettingsManager.
    /// </summary>
    public sealed class SettingsManager: ISettingsManager, IDisposable
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<SettingsManager>();
        private const string StorageKey = "app_settings";
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private Timer _updateSettingsTimer;
        public AppSettings AppSettings { get; set; }

        public event EventHandler<SettingsEventArgs> SettingsUpdated;
        public AppSettings DefaultAppSettings { get; set; }

        public async Task<AppSettings> GetSettings(bool forceUpdate = false)
        {
            if (AppSettings != null && !forceUpdate)
            {
                Logger.Debug("SettingsManager returned settings from cache." + AppSettings);
                return AppSettings;
            }

            var settings = (await GetSettingsFromApiAsync() ?? GetSettingsFromStorage()) ?? CreateDefaultSettings();

            InitTimer(settings.SettingsUpdateInterval);

            AppSettings = settings;
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
                var responseMessage = await ServiceManager.ApiConnction.LoadSettings();
                if (string.IsNullOrEmpty(responseMessage))
                {
                    return null;
                }

                var settings = JsonConvert.DeserializeObject<AppSettings>(responseMessage);

                SaveSettingsToStorage(settings);

                Logger.Debug("Got settings from api. " + settings.Keys);

                return settings;
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
