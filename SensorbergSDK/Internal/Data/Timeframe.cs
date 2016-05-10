// Created by Kay Czarnotta on 10.05.2016
// 
// Copyright (c) 2016,  EagleEye
// 
// All rights reserved.

using System;

namespace SensorbergSDK.Internal.Data
{
    public sealed class Timeframe
    {
        public DateTimeOffset? Start
        {
            get;
            set;
        }
        public DateTimeOffset? End
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
}