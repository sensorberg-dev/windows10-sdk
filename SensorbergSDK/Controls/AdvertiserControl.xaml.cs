using System;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SensorbergControlLibrary.Model;
using SensorbergSDK;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace SensorbergControlLibrary.Controls
{
    public sealed partial class AdvertiserControl : UserControl
    {
        private const char HexStringSeparator = '-';
        private const int BeaconId1LengthWithoutDashes = 32;
        private const UInt16 ManufacturerId = 0x004c;
        private const UInt16 BeaconCode = 0x0215;


        public event Action<string> ShowInformationalMessage;
        private readonly Advertiser _advertiser;
        private AdvertiserControlModel Model { get; } = new AdvertiserControlModel();

        public AdvertiserControl()
        {
            InitializeComponent();
            _advertiser = new Advertiser();
            _advertiser.ManufacturerId = ManufacturerId;
            _advertiser.BeaconCode = BeaconCode;
        }

        private async void OnToggleAdvertizingButtonClickedAsync(object sender, RoutedEventArgs e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (_advertiser.IsStarted)
                {
                    _advertiser.Stop();
                }
                else
                {
                    if (ValuesForAdvertisementAreValid())
                    {
                        _advertiser.BeaconId1 = Model.BeaconId1;

                        try
                        {
                            int id2 = int.Parse(Model.BeaconId2);
                            if (id2 > ushort.MaxValue)
                            {
                                ShowInformationalMessage?.Invoke("The major id is to long, it should be between 0 and " + ushort.MaxValue);
                                return;
                            }
                            int id3 = int.Parse(Model.BeaconId3);
                            if (id3 > ushort.MaxValue)
                            {
                                ShowInformationalMessage?.Invoke("The minor id is to long, it should be between 0 and " + ushort.MaxValue);
                                return;
                            }
                            _advertiser.BeaconId2 = (ushort)id2;
                            _advertiser.BeaconId3 = (ushort)id3;

                            _advertiser.Start();

                        }
                        catch (Exception ex)
                        {
                            ShowInformationalMessage?.Invoke("Failed to start advertiser "+ ex.ToString());
                        }
                    }
                    else
                    {
                        ShowInformationalMessage?.Invoke(
                            "At least one of the entered values is invalid. The length of the beacon ID 1 (without dashes, which are ignored) must be "
                            + BeaconId1LengthWithoutDashes + " characters.");
                    }
                }

                Model.IsAdvertisingStarted = _advertiser.IsStarted;
            });
        }

        /// <summary>
        /// Validates the entered beacon IDs.
        /// </summary>
        /// <returns>True, if the values are valid, false otherwise.</returns>
        private bool ValuesForAdvertisementAreValid()
        {
            bool valid = false;

            if (!String.IsNullOrEmpty(Model.BeaconId1))
            {
                string beaconId1WithoutDashes = string.Join("", Model.BeaconId1.Split(HexStringSeparator));
                bool isValidHex = Regex.IsMatch(beaconId1WithoutDashes, @"\A\b[0-9a-fA-F]+\b\Z");

                if (isValidHex && beaconId1WithoutDashes.Length == BeaconId1LengthWithoutDashes)
                {
                    try
                    {
                        int.Parse(Model.BeaconId2);
                        int.Parse(Model.BeaconId3);
                        valid = true;
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return valid;
        }
    }
}
