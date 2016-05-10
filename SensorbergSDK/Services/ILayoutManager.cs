// Created by Kay Czarnotta on 08.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading.Tasks;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Transport;

namespace SensorbergSDK.Services
{
    public interface ILayoutManager
    {
        Task<RequestResultState> ExecuteRequestAsync(Request currentRequest);
        Task InvalidateLayout();
        bool IsLayoutValid { get; }
        Layout Layout { get; }
        Task<bool> VerifyLayoutAsync(bool b = false);
        event EventHandler<bool> LayoutValidityChanged;
        ResolvedAction GetAction(string actionId);
    }
}