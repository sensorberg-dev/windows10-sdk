using SensorbergSDK;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System;

namespace SensorbergShowcase
{
    public class BeaconDetailsModel
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

        private Timer _updateBeaconTimesTimer;
 
        public ObservableCollection<BeaconDetailsItem> BeaconDetailsCollection
        {
            get;
            set;
        }

        public BeaconDetailsModel()
        {
            BeaconDetailsCollection = new ObservableCollection<BeaconDetailsItem>();
        }

        public void AddOrReplace(Beacon beacon)
        {
            bool updated = false;

            for (int i = 0; i < BeaconDetailsCollection.Count; ++i)
            {
                if (BeaconDetailsCollection[i].Matches(beacon))
                {
                    BeaconDetailsCollection[i].Update(beacon);
                    updated = true;
                    break;
                }
            }

            if (!updated)
            {
                BeaconDetailsItem item = new BeaconDetailsItem(beacon);
                item.VendorName = ResolveVendor(beacon.Id1);
                BeaconDetailsCollection.Add(item);

                if (_updateBeaconTimesTimer == null)
                {
                    _updateBeaconTimesTimer = new Timer(UpdateBeaconTimesAsync, null, 1000, 1000);
                }
            }
        }

        public void Remove(Beacon beacon)
        {
            bool found = false;
            int index = 0;

            for (index = 0; index < BeaconDetailsCollection.Count; ++index)
            {
                if (BeaconDetailsCollection[index].Matches(beacon))
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                BeaconDetailsCollection.RemoveAt(index);
            }
        }

        public void Clear()
        {
            BeaconDetailsCollection.Clear();
        }

        public int Count()
        {
            return BeaconDetailsCollection.Count;
        }

        public void SetBeaconRange(Beacon beacon, int range)
        {
            for (int i = 0; i < BeaconDetailsCollection.Count; ++i)
            {
                if (BeaconDetailsCollection[i].Matches(beacon))
                {
                    BeaconDetailsCollection[i].Range = range;
                    break;
                }
            }
        }

        public void SortBeaconsBasedOnDistance()
        {
            if (BeaconDetailsCollection.Count > 1)
            {
                bool wasChanged = true;

                while (wasChanged)
                {
                    wasChanged = false;

                    for (int i = 0; i < BeaconDetailsCollection.Count - 1; ++i)
                    {
                        if (BeaconDetailsCollection[i + 1].Distance < BeaconDetailsCollection[i].Distance
                            || (BeaconDetailsCollection[i + 1].Distance == BeaconDetailsCollection[i].Distance
                                && BeaconDetailsCollection[i + 1].Timestamp < BeaconDetailsCollection[i].Timestamp))
                        {
                            BeaconDetailsItem temp = BeaconDetailsCollection[i];
                            BeaconDetailsCollection[i] = BeaconDetailsCollection[i + 1];
                            BeaconDetailsCollection[i + 1] = temp;
                            wasChanged = true;
                        }
                    }
                }
            }
        }

        public bool Contains(Beacon beacon)
        {
            bool found = false;

            for (int i = 0; i < BeaconDetailsCollection.Count; ++i)
            {
                if (BeaconDetailsCollection[i].Matches(beacon))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        /// <summary>
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

        private async void UpdateBeaconTimesAsync(object state)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (BeaconDetailsCollection.Count > 0)
                {
                    for (int i = 0; i < BeaconDetailsCollection.Count; ++i)
                    {
                        BeaconDetailsCollection[i].UpdateLastSeen();
                    }
                }
                else if (_updateBeaconTimesTimer != null)
                {
                    _updateBeaconTimesTimer.Dispose();
                    _updateBeaconTimesTimer = null;
                }
            });         
        }
    }
}
