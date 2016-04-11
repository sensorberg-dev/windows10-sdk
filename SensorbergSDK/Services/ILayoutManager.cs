// Created by Kay Czarnotta on 08.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading.Tasks;
using Windows.Foundation;
using SensorbergSDK.Internal;

namespace SensorbergSDK.Services
{
    public interface ILayoutManager
    {
        IAsyncOperation<RequestResultState> ExecuteRequestAsync(Request currentRequest);
        Task InvalidateLayout();
        bool IsLayoutValid { get; }
        Layout Layout { get; }
        IAsyncOperation<bool> VerifyLayoutAsync(bool b = false);
        event EventHandler<bool> LayoutValidityChanged;
        ResolvedAction GetAction(string actionId);
    }
}