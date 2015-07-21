using System;
using Windows.Devices.Bluetooth.Advertisement;

namespace SensorbergSDK
{
	/// <summary>
	/// Manages publishing BLE advertisements.
	/// </summary>
	public sealed class Advertiser
	{
		private const int DefaultMeasuredPower = -59;
		private BluetoothLEAdvertisementPublisher _advertisementPublisher;
		private Beacon _beacon;

		public string BeaconId1
		{
			get;
			set;
		}

        public UInt16 ManufacturerId
        {
            get;
            set;
        }

        public UInt16 BeaconCode
        {
            get;
            set;
        }

        public UInt16 BeaconId2
		{
			get;
			set;
		}

		public UInt16 BeaconId3
		{
			get;
			set;
		}

		public bool IsStarted
		{
			get;
			private set;
		}

		public Advertiser()
		{
		}

		/// <summary>
		/// Starts advertizing based on the set values (beacon ID 1, ID 2 and ID 3).
		/// Note that this method does not validate the values and will throw exception, if the
		/// values are invalid.
		/// </summary>
		public void Start()
		{
			if (!IsStarted)
			{
				_beacon = new Beacon();
				_beacon.Id1 = BeaconId1;
                _beacon.ManufacturerId = ManufacturerId;
                _beacon.Code = BeaconCode;
				_beacon.Id2 = BeaconId2;
				_beacon.Id3 = BeaconId3;
				_beacon.MeasuredPower = DefaultMeasuredPower;

				_advertisementPublisher = new BluetoothLEAdvertisementPublisher();
               
                BluetoothLEAdvertisementDataSection dataSection = BeaconFactory.BeaconToSecondDataSection(_beacon);
                System.Diagnostics.Debug.WriteLine("Advertiser.Start(): " + BeaconFactory.DataSectionToRawString(dataSection));
                _advertisementPublisher.Advertisement.DataSections.Add(dataSection);
				_advertisementPublisher.Start();

				IsStarted = true;
			}
		}

		public void Stop()
		{
			if (IsStarted)
			{
				_advertisementPublisher.Stop();
				_advertisementPublisher = null;
				IsStarted = false;
			}
		}
	}
}
