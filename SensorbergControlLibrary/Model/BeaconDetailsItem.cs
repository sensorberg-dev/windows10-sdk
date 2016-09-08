using System;
using System.Collections.Generic;
using System.ComponentModel;
using SensorbergSDK;

namespace SensorbergControlLibrary.Model
{
    public class BeaconDetailsItem : INotifyPropertyChanged
    {
        public readonly Dictionary<string, string> UUIDMap = new Dictionary<string, string>()
        {
            { "73676723-7400-0000-FFFF", "Sensorberg" },
            { "10A8774D-E5D7-404D-9D25-66ADD4AD1DB3", "Movingtracks" },
            { "19D5F76A-FD04-5AA3-B16E-E93277163AF6", "Passkit" },
            { "20CAE8A0-A9CF-11E3-A5E2-0800200C9A66", "Onyx beacon" },
            { "2F234454-CF6D-4A0F-ADF2-F4911BA9FFA6", "RadBeacon" },
            { "61687109-905F-4436-91F8-E602F514C96D", "Bluecats" },
            { "74278BDA-B644-4520-8F0C-720EAF059935", "Glimworm" },
            { "85FC11DD-4CCA-4B27-AFB3-876854BB5C3B", "Smartbeacon" },
            { "8DEEFBB9-F738-4297-8040-96668BB44281", "Roximity" },
            { "ACFD065e-C3C0-11E3-9BBE-1A514932AC01", "BlueUp" },
            { "B9407F30-F5F8-466E-AFF9-25556B57FE6D", "Estimote" },
            { "B0702980-A295-A8AB-F734-031A98A512DE", "RedBear" },
            { "F0018B9B-7509-4C31-A905-1A27D39C003C", "Beaconinside" },
            { "E2C56DB5-DFFB-48D2-B060-D0F5A71096E0", "Twocanoes" },
            { "EBEFD083-70A2-47C8-9837-E7B5634DF524", "EasiBeacon or Beaconic" },
            { "F7826DA6-4FA2-4E98-8024-BC5B71E0893E", "kontakt.io" },
            { "F2A74FC4-7625-44DB-9B08-CB7E130B2029", "Ubudu" },
            { "D57092AC-DFAA-446C-8EF3-C81AA22815B5", "SB Legacy or shopnow" }
        };

        private const int MinRange = 0;
        private const int MaxRange = 3;

        public event PropertyChangedEventHandler PropertyChanged;
        private Beacon _beacon;

        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            private set
            {
                if (string.IsNullOrEmpty(_name) || !_name.Equals(value))
                {
                    _name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        private string _vendorName;
        public string VendorName
        {
            get
            {
                return _vendorName;
            }
            set
            {
                if (string.IsNullOrEmpty(_vendorName) || !_vendorName.Equals(value))
                {
                    _vendorName = value;

                    if (!string.IsNullOrEmpty(_vendorName))
                    {
                        Name = _vendorName + " beacon";
                    }

                    NotifyPropertyChanged("VendorName");
                }
            }
        }

        private string _uuid;
        public string UUID
        {
            get
            {
                return _uuid;
            }
            private set
            {
                if (string.IsNullOrEmpty(_uuid) || !_uuid.Equals(value))
                {
                    _uuid = value;
                    NotifyPropertyChanged("UUID");
                }
            }
        }

        private int _beaconId2;
        public int BeaconId2
        {
            get
            {
                return _beaconId2;
            }
            private set
            {
                if (_beaconId2 != value)
                {
                    _beaconId2 = value;
                    NotifyPropertyChanged("BeaconId2");
                }
            }
        }

        private int _beaconId3;
        public int BeaconId3
        {
            get
            {
                return _beaconId3;
            }
            private set
            {
                if (_beaconId3 != value)
                {
                    _beaconId3 = value;
                    NotifyPropertyChanged("BeaconId3");
                }
            }
        }

        private int _rawSignalStrengthInDBm;
        public int RawSignalStrengthInDBm
        {
            get
            {
                return _rawSignalStrengthInDBm;
            }
            private set
            {
                if (_rawSignalStrengthInDBm != value)
                {
                    _rawSignalStrengthInDBm = value;
                    NotifyPropertyChanged("RawSignalStrengthInDBm");
                }
            }
        }

        private int _measuredPower;
        public int MeasuredPower
        {
            get
            {
                return _measuredPower;
            }
            private set
            {
                if (_measuredPower != value)
                {
                    _measuredPower = value;
                    NotifyPropertyChanged("MeasuredPower");
                }
            }
        }

        private double _distance;
        public double Distance
        {
            get
            {
                return _distance;
            }
            private set
            {
                if (_distance != value)
                {
                    _distance = value;
                    NotifyPropertyChanged("Distance");
                }
            }
        }

        /// <summary>
        /// Can have value from 0 to 3.
        /// 0 indicates furthest and 3 closest.
        /// </summary>
        private int _range;
        public int Range
        {
            get
            {
                return _range;
            }
            set
            {
                if (_range != value)
                {
                    _range = value;
                    NotifyPropertyChanged("Range");
                }
            }
        }

        public DateTimeOffset Timestamp
        {
            get
            {
                return _beacon.Timestamp;
            }
        }

        private int _secondsElapsedSinceLastSeen;
        public int SecondsElapsedSinceLastSeen
        {
            get
            {
                return _secondsElapsedSinceLastSeen;
            }
            set
            {
                if (_secondsElapsedSinceLastSeen != value)
                {
                    _secondsElapsedSinceLastSeen = value;
                    NotifyPropertyChanged("SecondsElapsedSinceLastSeen");
                }
            }
        }

        public BeaconDetailsItem(Beacon beacon)
        {
            if (beacon == null)
            {
                throw new ArgumentException("Beacon cannot be null");
            }
            Update(beacon);
        }

        private BeaconDetailsItem()
        {
        }

        public async void Update(Beacon beacon)
        {
            if (beacon != null)
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    _beacon = beacon;
                    UUID = _beacon.Id1;
                    BeaconId2 = _beacon.Id2;
                    BeaconId3 = _beacon.Id3;
                    VendorName = ResolveVendor(_beacon.Id1);
                    RawSignalStrengthInDBm = _beacon.RawSignalStrengthInDBm;
                    MeasuredPower = _beacon.MeasuredPower;
                    Distance = Math.Round(_beacon.Distance, 2);
                    SetRange();
                    UpdateLastSeen();
                });
            }
        }

        /// <summary>
        /// We show the icon with no lines when the beacon was not seen for 2 seconds
        /// We show the icon with 3 lines when the beacon is closer than 1 meter
        /// We show the icon with 2 lines when the beacon closer than 10 meter but further away than 1 meter
        /// We show the icon with 1 lines when the beacon further than 10 meter away(edited)
        /// </summary>
        public void SetRange()
        {
            if (Distance <= 1.0d)
            {
                Range = 3;
            }
            else if (Distance <= 10.0d)
            {
                Range = 2;
            }
            else
            {
                Range = 1;
            }
        }

        public void UpdateLastSeen()
        {
            TimeSpan timeElapsedSinceLastSeen = DateTime.Now - Timestamp;
            SecondsElapsedSinceLastSeen = (int)timeElapsedSinceLastSeen.TotalSeconds;
        }

        public bool Matches(Beacon beacon)
        {
            return (_beacon != null && _beacon.Matches(beacon));
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        /// Resolves the vendor name based on the given UUID.
        /// </summary>
        /// <param name="uuid">The beacon UUID.</param>
        /// <returns>The vendor name or "Unknown" if not found.</returns>
        private string ResolveVendor(string uuid)
        {
            string vendor = "Unknown";

            foreach (string key in UUIDMap.Keys)
            {
                if (uuid.ToLower().StartsWith(key.ToLower()))
                {
                    vendor = UUIDMap[key];
                }
            }

            return vendor;
        }

    }
}
