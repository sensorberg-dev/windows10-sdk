using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using SensorbergSDK.Services;

namespace SensorbergSDK.Internal.Services
{
    public sealed class SettingsManager: ISettingsManager, IDisposable
    {
        private const string STORAGE_KEY = "app_settings";
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private readonly SDKData _sdkData;
        private Timer _updateSettingsTimer;
        private AppSettings _lastSettings;

        public event EventHandler<SettingsEventArgs> SettingsUpdated;
        public AppSettings DefaultAppSettings { get; set; }


        public SettingsManager()
        {
            _sdkData = SDKData.Instance;
        }

        public async Task<AppSettings> GetSettings(bool forceUpdate = false)
        {
            if (_lastSettings != null && forceUpdate == false)
            {
                Debug.WriteLine("SettingsManager returned settings from cache." + _lastSettings);
                return _lastSettings;
            }

            var settings = (await GetSettingsFromApiAsync() ?? GetSettingsFromStorage()) ?? CreateDefaultSettings();

            InitTimer(settings.SettingsUpdateInterval);

            _lastSettings = settings;
            return settings;
        }

        private void InitTimer(UInt64 miliseconds)
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

                var parsed = JsonValue.Parse(responseMessage);

                var settingsParsed = parsed.GetObject()["settings"];

                var settings = AppSettings.FromJson(settingsParsed.GetObject());
                SaveSettingsToStorage(settings);

                Debug.WriteLine("Got settings from api. " + settings);

                return settings;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SettingsManager.GetSettingsFromApiAsync(): Failed to send HTTP request: " + ex.Message);
                return null;
            }
        }

        private AppSettings CreateDefaultSettings()
        {
            Debug.WriteLine("SettingsManager used default settings values.");
            return DefaultAppSettings != null
                ? DefaultAppSettings
                : new AppSettings();
        }

        private void SaveSettingsToStorage(AppSettings settings)
        {
            MemoryStream stream1 = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(AppSettings));
            ser.WriteObject(stream1, settings);
            stream1.Position = 0;
            StreamReader sr = new StreamReader(stream1);
            string jsonString = sr.ReadToEnd();
            _localSettings.Values[STORAGE_KEY] = jsonString;
        }

        private AppSettings GetSettingsFromStorage()
        {
            var storageValue = _localSettings.Values[STORAGE_KEY];

            var storageString = storageValue?.ToString();

            if (string.IsNullOrEmpty(storageString))
            {
                return null;
            }

            var parsed = JsonValue.Parse(storageString);
            return AppSettings.FromJson(parsed.GetObject());
        }

        public void Dispose()
        {
            _updateSettingsTimer?.Dispose();
        }
    }
}
