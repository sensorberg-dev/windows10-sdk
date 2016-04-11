using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Windows.Data.Json;

namespace SensorbergSDK.Internal
{
    public sealed class Timeframe
    {
        public DateTimeOffset ?Start
        {
            get;
            set;
        }
        public DateTimeOffset ?End
        {
            get;
            set;
        }

        private bool Equals(Timeframe other)
        {
            return Start.Equals(other.Start) && End.Equals(other.End);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Timeframe && Equals((Timeframe) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start.GetHashCode()*397) ^ End.GetHashCode();
            }
        }

        public static bool operator ==(Timeframe left, Timeframe right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Timeframe left, Timeframe right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// Internal class that represents a single action coming from the server. 
    /// Class holds a BeaconAction object which exposes public API for the application. 
    /// </summary>
    /// 
    [DataContract]
    public sealed class ResolvedAction
    {
        private static readonly string KeyActionUuid = "eid";
        private static readonly string KeyBeacons = "beacons";
        private static readonly string KeyContent = "content";
        private static readonly string KeyDelayTime = "delay";
        private static readonly string KeyTrigger = "trigger";
        private static readonly string KeyType = "type";
        private static readonly string KeySendOnlyOnce = "sendOnlyOnce";
        private static readonly string KeysupressionTime = "suppressionTime";
        private static readonly string KeyReportImmediately = "reportImmediately";
        private static readonly string KeyTimeframes = "timeframes";
        private static readonly string KeyStart = "start";
        private static readonly string KeyEnd = "end";
        private ICollection<string> beaconPids;

        [DataMember]
        public BeaconAction BeaconAction
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        [DataMember]
        public ICollection<string> BeaconPids
        {
            [DebuggerStepThrough]
            get { return beaconPids; }
            [DebuggerStepThrough]
            set { beaconPids = value; }
        }

        [DataMember]
        public BeaconEventType EventTypeDetectedByDevice
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        [DataMember]
        public long Delay
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        [DataMember]
        public bool SendOnlyOnce
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        [DataMember]
        public int SupressionTime
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        [DataMember]
        public bool ReportImmediately
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        [DataMember]
        public IList<Timeframe> Timeframes
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        public ResolvedAction()
        {
            BeaconPids = new HashSet<string>();
            Timeframes = new List<Timeframe>();
        }

        /// <summary>
        /// Parses and constructs a ResolvedAction instance from the given JSON data.
        /// </summary>
        /// <param name="contentJson"></param>
        /// <returns>A newly created ResolvedAction instance.</returns>
        public static ResolvedAction ResolvedActionFromJsonObject(JsonObject contentJson)
        {
            var resolvedAction = new ResolvedAction();

            var obj = contentJson.GetObject();
            var type = (int)obj.GetNamedValue(KeyType).GetNumber();
            var actionUUID = obj.GetNamedString(KeyActionUuid);
            var trigger = (int)obj.GetNamedNumber(KeyTrigger);
            var delaySeconds = JsonHelper.Optional(obj, KeyDelayTime, 0);
            var jsonContent = obj.GetNamedObject(KeyContent);
            var sendOnlyOnce = JsonHelper.OptionalBoolean(obj, KeySendOnlyOnce, false);
            var beacons = contentJson.GetNamedArray(KeyBeacons);
            var supressionTime = JsonHelper.Optional(obj, KeysupressionTime, -1);
            var reportImmediately = JsonHelper.OptionalBoolean(obj, KeyReportImmediately, false);

            // TimeFrames
            if (obj.ContainsKey(KeyTimeframes))
            {
                if (obj.GetNamedValue(KeyTimeframes).ValueType == JsonValueType.Array)
                {
                    var keyframes = obj.GetNamedArray(KeyTimeframes);

                    foreach (var frame in keyframes)
                    {
                        if (frame.ValueType == JsonValueType.Object)
                        {
                            string start = JsonHelper.OptionalString(frame.GetObject(), KeyStart);
                            string end = JsonHelper.OptionalString(frame.GetObject(), KeyEnd);
                            DateTimeOffset? startOffset = null;
                            DateTimeOffset? endOffset = null;

                            var newFrame = new Timeframe();

                            if (start.Length > 5)
                            {
                                startOffset = DateTimeOffset.Parse(start);
                            }
                            if (end.Length > 5)
                            {
                                endOffset = DateTimeOffset.Parse(end);
                            }

                            resolvedAction.Timeframes.Add(new Timeframe() { Start = startOffset, End = endOffset });
                        }
                    }
                }
            }

            foreach (JsonValue resp in beacons)
            {
                if (resp.ValueType == JsonValueType.String)
                {
                    resolvedAction.BeaconPids.Add(resp.GetString());
                }
            }

            BeaconAction action = ActionFactory.CreateBeaconAction(type, jsonContent, actionUUID);
            resolvedAction.BeaconAction = action;
            resolvedAction.EventTypeDetectedByDevice = (BeaconEventType)trigger;
            resolvedAction.Delay = delaySeconds;
            resolvedAction.SendOnlyOnce = sendOnlyOnce;
            resolvedAction.SupressionTime = supressionTime;
            resolvedAction.ReportImmediately = reportImmediately;

            return resolvedAction;
        }

        /// <summary>
        /// Serializes the given ResolvedAction instance.
        /// </summary>
        /// <param name="resolvedAction">The instance to serialize.</param>
        /// <returns>The serialized instance as string.</returns>
        public static string Serialize(ResolvedAction resolvedAction)
        {
            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ResolvedAction));
            serializer.WriteObject(stream, resolvedAction);
            stream.Position = 0;
            StreamReader streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }

        /// <summary>
        /// Deserializes the given serialized ResolvedAction.
        /// </summary>
        /// <param name="serializedResolvedAction">The serialized ResolvedAction as string.</param>
        /// <returns>The deserialized ResolvedAction instance.</returns>
        public static ResolvedAction Deserialize(string serializedResolvedAction)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ResolvedAction));
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serializedResolvedAction));
            return (ResolvedAction)serializer.ReadObject(stream);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public bool IsInsideTimeframes(DateTimeOffset time)
        {
            if (Timeframes.Count == 0)
            {
                // No timeframes specified
                return true;
            }

            foreach (var frame in Timeframes)
            {
                // If time is inside any Timeframes then we return true
                bool start = false;
                bool end = false;

                if (frame.Start != null)
                {
                    if (frame.Start < time)
                    {
                        start = true;
                    }
                }
                else
                {
                    // No start set so we are in it
                    start = true;
                }

                if (frame.End != null)
                {
                    if (frame.End > time)
                    {
                        end = true;
                    }
                }
                else
                {
                    end = true;
                }

                if (start && end)
                {
                    return true;
                }
            }

            return false;
        }

        private bool Equals(ResolvedAction other)
        {
            return /*Equals(beaconPids, other.beaconPids)*/ (!beaconPids?.Except(other.beaconPids).GetEnumerator().MoveNext()).Value && Equals(BeaconAction.ToString(), other.BeaconAction.ToString()) && EventTypeDetectedByDevice == other.EventTypeDetectedByDevice &&
                   Delay == other.Delay && SendOnlyOnce == other.SendOnlyOnce && SupressionTime == other.SupressionTime && ReportImmediately == other.ReportImmediately &&
                   /*Equals(Timeframes, other.Timeframes)*/ (!Timeframes?.Except(other.Timeframes).GetEnumerator().MoveNext()).Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) { return false;}
            if (ReferenceEquals(this, obj)) { return true;}
            return obj is ResolvedAction && Equals((ResolvedAction) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (beaconPids != null ? beaconPids.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (BeaconAction != null ? BeaconAction.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (int) EventTypeDetectedByDevice;
                hashCode = (hashCode*397) ^ Delay.GetHashCode();
                hashCode = (hashCode*397) ^ SendOnlyOnce.GetHashCode();
                hashCode = (hashCode*397) ^ SupressionTime;
                hashCode = (hashCode*397) ^ ReportImmediately.GetHashCode();
                hashCode = (hashCode*397) ^ (Timeframes != null ? Timeframes.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ResolvedAction left, ResolvedAction right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ResolvedAction left, ResolvedAction right)
        {
            return !Equals(left, right);
        }
    }
}
