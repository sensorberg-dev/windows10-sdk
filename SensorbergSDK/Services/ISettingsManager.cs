// Created by Kay Czarnotta on 15.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading.Tasks;
using SensorbergSDK.Data;
using SensorbergSDK.Internal;

namespace SensorbergSDK.Services
{
    public interface ISettingsManager
    {
        event EventHandler<SettingsEventArgs> SettingsUpdated;
        AppSettings DefaultAppSettings { get; set; }
        Task<AppSettings> GetSettings(bool forceUpdate = false);
    }
}