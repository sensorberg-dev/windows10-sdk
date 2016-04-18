using System;
using SensorbergSDK.Data;

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
