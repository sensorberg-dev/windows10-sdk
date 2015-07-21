using System;
using SensorbergSDK;
using System.ComponentModel;

namespace SensorbergShowcase
{
	public class BeaconDetailsItem : INotifyPropertyChanged
	{
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

			VendorName = "Unknown";
            Update(beacon);
		}

		private BeaconDetailsItem()
		{
		}

		public void Update(Beacon beacon)
		{
			if (beacon != null)
			{
				_beacon = beacon;
                UUID = _beacon.Id1;
                BeaconId2 = _beacon.Id2;
                BeaconId3 = _beacon.Id3;
                RawSignalStrengthInDBm = _beacon.RawSignalStrengthInDBm;
                MeasuredPower = _beacon.MeasuredPower;
                Distance = Math.Round(_beacon.Distance, 2);
				SetRange();
                UpdateLastSeen();
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
    }
}
