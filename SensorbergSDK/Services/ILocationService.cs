// Created by Kay Czarnotta on 17.06.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace SensorbergSDK.Services
{
    /// <summary>
    /// Abstraction for the current location service.
    /// </summary>
    public interface ILocationService
    {
        /// <summary>
        /// Configuration for the sdk.
        /// </summary>
        SdkConfiguration Configuration { get; set; }

        /// <summary>
        /// Initialize the Locationservice.
        /// </summary>
        Task Initialize();

        /// <summary>
        /// Return string representation of the location. See https://en.wikipedia.org/wiki/Geohash .
        /// </summary>
        /// <returns>String representation of the current location or null if no location is available.</returns>
        Task<string> GetGeoHashedLocation();

        /// <summary>
        /// Returns the current Location.
        /// </summary>
        Task<Geoposition> GetLocation();
    }
}