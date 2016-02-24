using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDK.Internal.Transport
{
    internal sealed class SettingsManager
    {
        private const string STORAGE_KEY = "app_settings";
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private readonly SDKData _sdkData;
        private static SettingsManager _instance = null;
        private Timer _updateSettingsTimer;
        private AppSettings _lastSettings;

        public event EventHandler<SettingsEventArgs> SettingsUpdated;

        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SettingsManager();
                }

                return _instance;
            }
        }

        private SettingsManager()
        {
            _sdkData = SDKData.Instance;
        }

        public async Task<AppSettings> GetSettingsAsync(bool forceApi = false)
        {
            if (_lastSettings != null && forceApi == false)
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
            var settings = await GetSettingsAsync(true);
            if (SettingsUpdated != null && settings != null)
            {
                SettingsUpdated(this, new SettingsEventArgs(settings));
            }
        }

        private async Task<AppSettings> GetSettingsFromApiAsync()
        {

            HttpRequestMessage requestMessage = new HttpRequestMessage();
            HttpBaseProtocolFilter baseProtocolFilter = new HttpBaseProtocolFilter();

            try
            {
                baseProtocolFilter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
                baseProtocolFilter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;

                requestMessage.Method = HttpMethod.Get;
                requestMessage.RequestUri = new Uri(string.Format(Constants.SettingsUri, _sdkData.ApiKey));

                HttpClient httpClient = new HttpClient(baseProtocolFilter);


                var responseMessage = await httpClient.SendRequestAsync(requestMessage);


                if (responseMessage == null || responseMessage.IsSuccessStatusCode == false)
                {
                    return null;
                }

                var parsed = JsonValue.Parse(responseMessage.Content.ToString());

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
            return new AppSettings() {
                BeaconExitTimeout = Constants.DefaultBeaconExitTimeout,
                SettingsUpdateInterval = Constants.DefaultSettingsUpdateInterval,
                HistoryUploadInterval = Constants.DefaultHistoryUploadInterval,
                LayoutUpdateInterval = Constants.DefaultLayoutUpdateInterval
            };
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
    }
}
