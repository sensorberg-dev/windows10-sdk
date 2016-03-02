using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace SensorbergShowcase.Common
{
    public static class DeviceTypeHelper
    {
        public static Platform GetCurrentDeviceType()
        {
            bool isHardwareButtonsAPIPresent = ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons");

            if (isHardwareButtonsAPIPresent)
            {
                return Platform.WindowsPhone;
            }
            else
            {
                return Platform.Windows;
            }
        }
    }
}
