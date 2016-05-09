using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

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
        private ICollection<string> _beaconPids;

        [DataMember]
        public BeaconAction BeaconAction
        {
            [DebuggerStepThrough]
            get;
            [DebuggerStepThrough]
            set;
        }

        [DataMember(Name = "beacons")]
        public ICollection<string> BeaconPids
        {
            [DebuggerStepThrough]
            get { return _beaconPids; }
            [DebuggerStepThrough]
            set { _beaconPids = value; }
        }

        [DataMember(Name = "trigger")]
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

        [DataMember(Name = "suppressionTime")]
        public int SuppressionTime
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
            return /*Equals(beaconPids, other.beaconPids)*/ (!_beaconPids?.Except(other._beaconPids).GetEnumerator().MoveNext()).Value && Equals(BeaconAction.ToString(), other.BeaconAction.ToString()) && EventTypeDetectedByDevice == other.EventTypeDetectedByDevice &&
                   Delay == other.Delay && SendOnlyOnce == other.SendOnlyOnce && SuppressionTime == other.SuppressionTime && ReportImmediately == other.ReportImmediately &&
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
                var hashCode = (_beaconPids != null ? _beaconPids.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (BeaconAction != null ? BeaconAction.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (int) EventTypeDetectedByDevice;
                hashCode = (hashCode*397) ^ Delay.GetHashCode();
                hashCode = (hashCode*397) ^ SendOnlyOnce.GetHashCode();
                hashCode = (hashCode*397) ^ SuppressionTime;
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
