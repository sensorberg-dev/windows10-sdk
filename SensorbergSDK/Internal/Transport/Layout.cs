using System;
using System.Collections.Generic;
using Windows.Data.Json;

namespace SensorbergSDK.Internal
{
    /// <summary>
    /// Represents a layout with beacons and actions associated with them.
    /// </summary>
    public sealed class Layout
    {
        private const string KeyAccountBeaconId1s = "accountProximityUUIDs";
        private const string KeyActions = "actions";
        private const string KeyMaxAge = "max-age";

        public IList<string> AccountBeaconId1s
        {
            get;
            private set;
        }

        public IList<ResolvedAction> ResolvedActions
        {
            get;
            private set;
        }

        /*public IList<BeaconAction> InstantActions
        {
            get;
            private set;
        }*/

        public DateTimeOffset LastUpdated
        {
            get;
            private set;
        }

        public DateTimeOffset ValidTill
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructs a Layout instance from the given JSON data.
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="content"></param>
        /// <param name="layoutRetrievedTime"></param>
        /// <returns></returns>
        public static Layout FromJson(string headers, JsonObject content, DateTimeOffset layoutRetrievedTime)
        {
            Layout layout = null;

            try
            {
                layout = new Layout();

                if (!string.IsNullOrEmpty(headers))
                {
                    try
                    {
                        layout.ResolveMaxAge(headers, layoutRetrievedTime);
                    }
                    catch (Exception)
                    {
                        layout.ValidTill = DateTimeOffset.MaxValue;
                    }
                }
                else
                {
                    layout.ValidTill = DateTimeOffset.MaxValue;
                }

                layout.ResolveAccountBeaconId1s(content);
                layout.ResolveActions(content);
            } 
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Layout.FromJson(): Failed to parse: " + ex.ToString());
            }

            return layout;
        }

        private Layout()
        {
            AccountBeaconId1s = new List<string>();
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
                if (item.BeaconPids.ContainsKey(pid)
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
        public bool ContainsOtherThanSensorbergBeaconId1s()
        {
            bool containsOtherThanSensorbergBeaconId1s = false;

            foreach (string beaconId1 in AccountBeaconId1s)
            {
                if (!beaconId1.StartsWith(Constants.SensorbergUuidSpace, StringComparison.CurrentCultureIgnoreCase))
                {
                     containsOtherThanSensorbergBeaconId1s = true;
                }
            }

            return containsOtherThanSensorbergBeaconId1s;
        }

        private void ResolveMaxAge(string headers, DateTimeOffset layoutRetrievedTime)
        {
            int startIndex = headers.IndexOf(KeyMaxAge, 0, headers.Length) + KeyMaxAge.Length + 1;
            int endIndex = headers.IndexOf(';', startIndex, headers.Length - startIndex);
            string maxAgeAsString = headers.Substring(startIndex, endIndex - startIndex);
            double maxAgeAsDouble = double.Parse(maxAgeAsString);
            ValidTill = layoutRetrievedTime + TimeSpan.FromSeconds(maxAgeAsDouble);
        }

        private void ResolveAccountBeaconId1s(JsonObject content)
        {
            AccountBeaconId1s.Clear();
            JsonArray responses = content.GetNamedArray(KeyAccountBeaconId1s);
            string beaconId1 = string.Empty;

            foreach (JsonValue resp in responses)
            {
                if (resp.ValueType == JsonValueType.String)
                {
                    beaconId1 = resp.GetString();
                    AccountBeaconId1s.Add(beaconId1);
                }
            }
        }

        private void ResolveActions(JsonObject content)
        {
            ResolvedActions.Clear();
            var actions = content.GetNamedArray(KeyActions);

            foreach (JsonValue resp in actions)
            {
                if (resp.ValueType == JsonValueType.Object)
                {
                    ResolvedActions.Add(ResolvedAction.ResolvedActionFromJsonObject(resp.GetObject()));
                }
            }
        }
    }
}
