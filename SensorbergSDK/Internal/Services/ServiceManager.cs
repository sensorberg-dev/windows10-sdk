// Created by Kay Czarnotta on 04.03.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.

using System.Diagnostics;

namespace SensorbergSDK.Internal.Services
{
    public static class ServiceManager
    {
        private static IApiConnection _apiConnction;
        private static IBeaconScanner _beaconScanner;
        private static ILayoutManager _layoutManager;

        public static IApiConnection ApiConnction
        {
            [DebuggerStepThrough]
            get { return _apiConnction; }
            [DebuggerStepThrough]
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
            [DebuggerStepThrough]
            get { return _beaconScanner; }
            [DebuggerStepThrough]
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
            [DebuggerStepThrough]
            get { return _layoutManager; }
            [DebuggerStepThrough]
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