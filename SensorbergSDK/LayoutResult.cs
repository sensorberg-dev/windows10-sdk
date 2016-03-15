// Created by Kay Czarnotta on 10.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Diagnostics;
using SensorbergSDK.Internal;

namespace SensorbergSDK
{
    /// <summary>
    /// Class provides the result for requests to the storage.
    /// </summary>
    public class LayoutResult
    {
        /// <summary>
        /// Layout that was loaded, null if it fails or no layout was found.
        /// </summary>
        public Layout Layout { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }
        
        /// <summary>
        /// Result of the requests.
        /// </summary>
        public NetworkResult Result { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }
    }
}