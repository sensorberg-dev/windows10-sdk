// Created by Kay Czarnotta on 05.09.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace SensorbergControlLibrary.Model
{
    public class AdvertiserControlModel : INotifyPropertyChanged
    {
        private static readonly string DefaultBeaconId1 = "73676723-7400-0000-ffff-0000ffff0001";
        private static readonly string DefaultBeaconId2 = "4";
        private static readonly string DefaultBeaconId3 = "2";
        private bool _isAdvertisingStarted;
        public event PropertyChangedEventHandler PropertyChanged;

        public string BeaconId1
        {
            get { return GetSettingsString("BeaconId1", DefaultBeaconId1); }
            set
            {
                ApplicationData.Current.LocalSettings.Values["BeaconId1"] = value;
                OnPropertyChanged();
            }
        }

        public string BeaconId2
        {
            get { return GetSettingsString("BeaconId2", DefaultBeaconId2); }
            set
            {
                ApplicationData.Current.LocalSettings.Values["BeaconId2"] = value;
                OnPropertyChanged();
            }
        }

        public string BeaconId3
        {
            get { return GetSettingsString("BeaconId3", DefaultBeaconId3); }
            set
            {
                ApplicationData.Current.LocalSettings.Values["BeaconId3"] = value;
                OnPropertyChanged();
            }
        }

        public bool IsAdvertisingStarted
        {
            get { return _isAdvertisingStarted; }
            set
            {
                _isAdvertisingStarted = value;
                OnPropertyChanged();
            }
        }

        private string GetSettingsString(string key, string defaultvalue)
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                return ApplicationData.Current.LocalSettings.Values[key] as string;
            }
            return defaultvalue;
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}