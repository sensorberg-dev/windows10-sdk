// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.
namespace SensorbergSDK.Internal.Services
{
    public static class ServiceManager
    {
        private static IApiConnection _apiConnction;
        private static IBeaconScanner _beaconScanner;
        private static ILayoutManager _layoutManager;

        public static IApiConnection ApiConnction
        {
            get { return _apiConnction; }
            set
            {
                if (_apiConnction == null || !ReadOnlyForTests)
                {
                    _apiConnction = value;
                }
            }
        }

        public static IBeaconScanner BeaconScanner
        {
            get { return _beaconScanner; }
            set
            {
                if (_beaconScanner == null || !ReadOnlyForTests)
                {
                    _beaconScanner = value;
                }
            }
        }

        public static ILayoutManager LayoutManager
        {
            get { return _layoutManager; }
            set
            {
                if (_layoutManager == null || !ReadOnlyForTests)
                {
                    _layoutManager = value;
                }
            }
        }

        public static bool ReadOnlyForTests { get; set; }
    }
}