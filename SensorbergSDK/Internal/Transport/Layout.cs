// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MetroLog;
using Newtonsoft.Json;
using SensorbergSDK.Internal.Transport.Converter;

namespace SensorbergSDK.Internal
{
    /// <summary>
    /// Represents a layout with beacons and actions associated with them.
    /// </summary>
    [DataContract]
    public sealed class Layout
    {
        private static readonly ILogger Logger = LogManagerFactory.DefaultLogManager.GetLogger<Layout>();
        private const string KeyMaxAge = "max-age";

        [DataMember(Name = "accountProximityUUIDs")]
        public IList<string> AccountBeaconId1S
        {
            get;
            private set;
        }

        [DataMember(Name = "actions")]
        [JsonConverter(typeof(ResolvedActionConverter))]
        public IList<ResolvedAction> ResolvedActions
        {
            get;
            private set;
        }

        private IList<BeaconAction> PlainActions
        {
            get;
            set;
        }

        public DateTimeOffset ValidTill
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructs a Layout instance from the given JSON data.
        /// </summary>
        /// <param name="headers">String repersentation of the header fields.</param>
        /// <param name="layoutRetrievedTime">Timestamp of receiving the layout.</param>
        public void FromJson(string headers, DateTimeOffset layoutRetrievedTime)
        {

            try
            {

                if (!string.IsNullOrEmpty(headers))
                {
                    try
                    {
                        ResolveMaxAge(headers, layoutRetrievedTime);
                    }
                    catch (Exception)
                    {
                        ValidTill = DateTimeOffset.MaxValue;
                    }
                }
                else
                {
                    ValidTill = DateTimeOffset.MaxValue;
                }

            } 
            catch (Exception ex)
            {
                Logger.Error("Layout.FromJson(): Failed to parse: " + ex, ex);
            }
        }

        public Layout()
        {
            AccountBeaconId1S = new List<string>();
            ResolvedActions = new List<ResolvedAction>();
        }

        /// <summary>
        /// Resolves the beacon actions associated with the given PID and event type.
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="eventType"></param>
        /// <returns>A list of actions based on the given values or an empty list if none found.</returns>
        public IList<ResolvedAction> GetResolvedActionsForPidAndEvent(string pid, BeaconEventType eventType)
        {
            List<ResolvedAction> actions = new List<ResolvedAction>();

            foreach (ResolvedAction item in ResolvedActions)
            {
                if (item.BeaconPids.Contains(pid)
                    && (item.EventTypeDetectedByDevice == eventType || item.EventTypeDetectedByDevice == BeaconEventType.EnterExit))
                { 
                    actions.Add(item);
                }
            }

            return actions;
        }

        /// <summary>
        /// Checks if the beacon ID1s in this layout contain other than those of Sensorberg
        /// beacons.
        /// </summary>
        /// <returns>True, if the beacon ID1s are not limited to Sensorberg beacons.</returns>
        public bool ContainsOtherThanSensorbergBeaconId1S()
        {
            bool containsOtherThanSensorbergBeaconId1S = false;

            foreach (string beaconId1 in AccountBeaconId1S)
            {
                if (!beaconId1.StartsWith(Constants.SensorbergUuidSpace, StringComparison.CurrentCultureIgnoreCase))
                {
                     containsOtherThanSensorbergBeaconId1S = true;
                }
            }

            return containsOtherThanSensorbergBeaconId1S;
        }

        private void ResolveMaxAge(string headers, DateTimeOffset layoutRetrievedTime)
        {
            int startIndex = headers.IndexOf(KeyMaxAge, 0, headers.Length, StringComparison.Ordinal) + KeyMaxAge.Length + 1;
            int endIndex = headers.IndexOf(';', startIndex, headers.Length - startIndex);
            string maxAgeAsString = headers.Substring(startIndex, endIndex - startIndex);
            double maxAgeAsDouble = double.Parse(maxAgeAsString);
            ValidTill = layoutRetrievedTime + TimeSpan.FromSeconds(maxAgeAsDouble);
        }

    }
}
