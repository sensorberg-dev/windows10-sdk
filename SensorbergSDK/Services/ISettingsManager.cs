// Created by Kay Czarnotta on 15.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading.Tasks;
using SensorbergSDK.Internal;

namespace SensorbergSDK.Services
{
    /// <summary>
    /// Abstraction for the settings manager. The manager handles the receiving of the appsettings from the backend.
    /// </summary>
    public interface ISettingsManager
    {
        /// <summary>
        /// Event for changes on the app settings.
        /// </summary>
        event EventHandler<SettingsEventArgs> SettingsUpdated;
        /// <summary>
        /// Returns the default settings for the app.
        /// </summary>
        AppSettings DefaultAppSettings { get; set; }

        /// <summary>
        /// Returns the settings of the app. If needed they will received from the backend.
        /// </summary>
        /// <param name="forceUpdate">forces the manager to update the current settings.</param>
        /// <returns></returns>
        Task<AppSettings> GetSettings(bool forceUpdate = false);
    }
}