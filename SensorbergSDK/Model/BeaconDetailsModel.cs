using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using SensorbergSDK;

namespace SensorbergControlLibrary.Model
{
    public class BeaconDetailsModel : INotifyPropertyChanged
    {
        private Timer _updateBeaconTimesTimer;
        private ObservableCollection<BeaconDetailsItem> _beaconDetailsCollection;

        public ObservableCollection<BeaconDetailsItem> BeaconDetailsCollection
        {
            get { return _beaconDetailsCollection; }
            set
            {
                _beaconDetailsCollection = value;
                OnPropertyChanged();
            }
        }

        public bool BeaconsInRange
        {
            get { return BeaconDetailsCollection.Count != 0; }
        }

        public BeaconDetailsModel()
        {
            BeaconDetailsCollection = new ObservableCollection<BeaconDetailsItem>();
            BeaconDetailsCollection.CollectionChanged += (sender, args) =>
            {
                OnPropertyChanged("BeaconsInRange");
            };
        }

        public async void AddOrReplace(Beacon beacon)
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
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                    BeaconDetailsCollection.Add(item));

                if (_updateBeaconTimesTimer == null)
                {
                    _updateBeaconTimesTimer = new Timer(UpdateBeaconTimesAsync, null, 1000, 1000);
                }
            }
        }

        public void Remove(Beacon beacon)
        {
            int index = 0;

            for (index = 0; index < BeaconDetailsCollection.Count; ++index)
            {
                if (BeaconDetailsCollection[index].Matches(beacon))
                {
                    BeaconDetailsCollection.RemoveAt(index);
                    break;
                }
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

        private async void UpdateBeaconTimesAsync(object state)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (BeaconDetailsCollection.Count > 0)
                {
                    List<BeaconDetailsItem> removeItems = new List<BeaconDetailsItem>();
                    for (int i = 0; i < BeaconDetailsCollection.Count; ++i)
                    {
                        BeaconDetailsCollection[i].UpdateLastSeen();
                        if (BeaconDetailsCollection[i].SecondsElapsedSinceLastSeen > 600)
                        {
                            removeItems.Add(BeaconDetailsCollection[i]);
                        }
                    }

                    foreach (BeaconDetailsItem item in removeItems)
                    {
                        BeaconDetailsCollection.Remove(item);
                    }
                }
                else if (_updateBeaconTimesTimer != null)
                {
                    _updateBeaconTimesTimer.Dispose();
                    _updateBeaconTimesTimer = null;
                }
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
