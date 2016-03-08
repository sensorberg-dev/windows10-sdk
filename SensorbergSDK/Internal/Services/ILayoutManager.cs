// Created by Kay Czarnotta on 08.03.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.

using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace SensorbergSDK.Internal.Services
{
    public interface ILayoutManager
    {
        IAsyncOperation<RequestResultState> ExecuteRequestAsync(Request currentRequest);
        IAsyncAction InvalidateLayoutAsync();
        bool IsLayoutValid { get; }
        Layout Layout { get; }
        IAsyncOperation<bool> VerifyLayoutAsync(bool b = false);
        event EventHandler<bool> LayoutValidityChanged;
    }
}