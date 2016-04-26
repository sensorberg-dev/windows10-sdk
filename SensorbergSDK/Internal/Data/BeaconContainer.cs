using System;
using System.Collections.Generic;

namespace SensorbergSDK.Internal
{
    /// <summary>
    /// A container class for managing a set of current beacons.
    /// </summary>
    public class BeaconContainer
    {
        private readonly List<Beacon> _beacons;
        private readonly object _beaconListLock;

        /// <summary>
        /// The number of beacons in the container.
        /// </summary>
        public int Count
        {
            get
            {
                return _beacons.Count;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public BeaconContainer()
        {
            _beaconListLock = new object();
            _beacons = new List<Beacon>();
        }

        /// <summary>
        /// Adds the given beacon to this container.
        /// </summary>
        /// <param name="beacon">The beacon to add.</param>
        /// <param name="overwrite">If true, will update a matching beacon.</param>
        public void Add(Beacon beacon, bool overwrite = false)
        {
            if (beacon == null)
            {
                return;
            }

            if (overwrite)
            {
                if (!TryUpdate(beacon))
                {
                    SaveAddBeacon(beacon);
                }
            }
            else
            {
                SaveAddBeacon(beacon);
            }
        }

        /// <param name="beacon"></param>
        /// <returns>True, if the given beacon matches an existing one in this container.</returns>
        public bool Contains(Beacon beacon)
        {
            bool found = false;

            lock (_beaconListLock)
            {
                foreach (Beacon existingBeacon in _beacons)
                {
                    if (beacon.Matches(existingBeacon))
                    {
                        found = true;
                        break;
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Updates a matching beacon in the list based on the given one, if found.
        /// </summary>
        /// <param name="beacon">The beacon, with latest data, to update.</param>
        /// <returns>True, if updated. False, if no matching beacon was found.</returns>
        public bool TryUpdate(Beacon beacon)
        {
            lock (_beaconListLock)
            {
                for (int i = 0; i < _beacons.Count; ++i)
                {
                    if (beacon.Matches(_beacons[i]))
                    {
                        _beacons[i] = beacon;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the beacons, which are older than the given age, from this container.
        /// </summary>
        /// <param name="olderThanAgeInMilliseconds">The minimum age of the beacons, which should be removed.</param>
        /// <returns>A list of removed beacons.</returns>
        public List<Beacon> RemoveBeaconsBasedOnAge(UInt64 olderThanAgeInMilliseconds)
        {
            List<Beacon> removedBeacons = new List<Beacon>();

            if (_beacons.Count > 0)
            {
                TimeSpan ageAsTimeSpan = TimeSpan.FromMilliseconds(olderThanAgeInMilliseconds);

                lock (_beaconListLock)
                {
                    for (int i = _beacons.Count - 1; i >= 0; --i)
                    {
                        var currentBeacon = _beacons[i];

                        if (currentBeacon.Timestamp.Add(ageAsTimeSpan) < DateTime.Now)
                        {
                            System.Diagnostics.Debug.WriteLine("CurrentBeaconsContainer.RemoveOldBeacons(): "
                                + string.Format("Last seen {0:mm:ss}, time is now {1:mm:ss} => exit",
                                    currentBeacon.Timestamp, DateTime.Now));

                            removedBeacons.Add(currentBeacon);
                            _beacons.RemoveAt(i);
                        }
                    }
                }
            }

            return removedBeacons;
        }

        /// <summary>
        /// Finds the beacons, which are older than the given age.
        /// </summary>
        /// <param name="olderThanAgeInMilliseconds">The minimum age of the beacons to find.</param>
        /// <returns>A list of beacons matching the age criteria.</returns>
        public List<Beacon> BeaconsBasedOnAge(UInt64 olderThanAgeInMilliseconds)
        {
            List<Beacon> beacons = new List<Beacon>();
            TimeSpan ageAsTimeSpan = TimeSpan.FromMilliseconds(olderThanAgeInMilliseconds);

            lock (_beaconListLock)
            {
                foreach (Beacon beacon in _beacons)
                {
                    if (beacon.Timestamp.Add(ageAsTimeSpan) < DateTime.Now)
                    {
                        beacons.Add(beacon);
                    }
                }
            }

            return beacons;
        }

        /// <summary>
        /// Locks collection and add beacon to it
        /// </summary>
        /// <param name="beacon">Beacon to add</param>
        private void SaveAddBeacon(Beacon beacon)
        {
            lock (_beaconListLock)
            {
                _beacons.Add(beacon);
            }
        }
    }
}
