// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;

namespace SensorbergSDK.Internal
{
    public class SettingsEventArgs: EventArgs
    {
        public SettingsEventArgs(AppSettings settings)
        {
            Settings = settings;
        }

        public AppSettings Settings { get; private set; }
    }
}
