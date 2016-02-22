using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensorbergSDK.Internal.Data;

namespace SensorbergSDK.Internal
{
    internal class SettingsEventArgs: EventArgs
    {
        public SettingsEventArgs(AppSettings settings)
        {
            Settings = settings;
        }

        public AppSettings Settings { get; private set; }
    }
}
