// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using SensorbergSDK.Internal.Utils;

namespace SensorbergSDK
{
    [DataContract]
    public sealed class Beacon
    {
        private const char HexStringSeparator = '-'; // For UpdatePid()

        [DataMember]
        public ushort ManufacturerId
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        [DataMember]
        public ushort Code
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        private string _id1;
        [DataMember]
        public string Id1
        {
            [DebuggerStepThrough]
            get
            {
                return _id1;
            }
            [DebuggerStepThrough]
            set
            {
                if (_id1 != value)
                {
                    _id1 = value;
                    UpdatePid();
                }
            }
        }

        private ushort _id2;
        [DataMember]
        public ushort Id2
        {
            [DebuggerStepThrough]
            get
            {
                return _id2;
            }
            [DebuggerStepThrough]
            set
            {
                if (_id2 != value)
                {
                    _id2 = value;
                    UpdatePid();
                }
            }
        }

        private ushort _id3;
        [DataMember]
        public ushort Id3
        {
            [DebuggerStepThrough]
            get
            {
                return _id3;
            }
            [DebuggerStepThrough]
            set
            {
                if (_id3 != value)
                {
                    _id3 = value;
                    UpdatePid();
                }
            }
        }

        public byte Aux
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }
            
        /// <summary>
        /// PID is a unique string consisting of IDs 1, 2 and 3 this beacon.
        /// The length of the PID is always the same.
        /// </summary>
        [DataMember]
        public string Pid
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            private set;
        }

        [DataMember]
        public double Distance
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            private set;
        }

        private int _rawSignalStrengthInDBm;
        [DataMember]
        public int RawSignalStrengthInDBm
        {
            [DebuggerStepThrough]
            get
            {
                return _rawSignalStrengthInDBm;
            }
            [DebuggerStepThrough]
            set
            {
                if (_rawSignalStrengthInDBm != value)
                {
                    _rawSignalStrengthInDBm = value;
                    CalculateDistance(_rawSignalStrengthInDBm, MeasuredPower);
                }
            }
        }

        private int _measuredPower;
        [DataMember]
        public int MeasuredPower
        {
            [DebuggerStepThrough]
            get
            {
                return _measuredPower;
            }
            [DebuggerStepThrough]
            set
            {
                if (_measuredPower != value)
                {
                    _measuredPower = value;
                    CalculateDistance(RawSignalStrengthInDBm, _measuredPower);
                }
            }
        }

        [DataMember]
        public DateTimeOffset Timestamp
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        /// <summary>
        /// Compares the given beacon to this.
        /// </summary>
        /// <param name="beacon">The beacon to compare to.</param>
        /// <returns>True, if the beacons match.</returns>
        public bool Matches(Beacon beacon)
        {
            return beacon.Id1==Id1
                && beacon.Id2 == Id2
                && beacon.Id3 == Id3;
        }

        /// <summary>
        /// Creates a string representation of this instance consisting of ID1,
        /// ID2, ID3 and measured power.
        /// </summary>
        /// <returns>A string representation of this instance.</returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("[");
            stringBuilder.Append(string.Join("; ", Id1, Id2, Id3, MeasuredPower));
            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Updated the beacon PID; The ID 1 (without dashes) + 5 digits ID 2
        /// (padded with zeros) + 5 digits ID 3 (padded with zeros).
        /// </summary>
        private void UpdatePid()
        {
            string template = "00000";
            string beaconId2 = Id2.ToString();
            string beaconId3 = Id3.ToString();
            beaconId2 = template.Substring(beaconId2.Length) + beaconId2;
            beaconId3 = template.Substring(beaconId3.Length) + beaconId3;
            string pid = Id1.Replace(HexStringSeparator.ToString(), string.Empty) + beaconId2 + beaconId3;
            Pid = pid.ToLower();
        }

        private void CalculateDistance(int rawSignalStrengthInDBm, int measuredPower)
        {
            if (rawSignalStrengthInDBm != 0 && measuredPower != 0)
            {
                Distance = BeaconFactory.CalculateDistanceFromRssi(rawSignalStrengthInDBm, measuredPower);
            }
        }
    }
}
