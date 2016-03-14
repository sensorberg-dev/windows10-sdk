// Created by Kay Czarnotta on 09.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading.Tasks;
using Windows.Foundation;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDKTests.Mocks
{
    public class MockLayoutManager:ILayoutManager
    {
        public IAsyncOperation<RequestResultState> ExecuteRequestAsync(Request currentRequest)
        {
            FailToken token = new FailToken();

            ShouldFail?.Invoke(currentRequest, token);
            return Task.FromResult<RequestResultState>(token.Fail ? RequestResultState.Failed : RequestResultState.Success).AsAsyncOperation();
        }

        public IAsyncAction InvalidateLayoutAsync()
        {
            Func<Task> action = async () => { };
            return action().AsAsyncAction();
        }

        public bool IsLayoutValid { get; }
        public Layout Layout { get; }
        public IAsyncOperation<bool> VerifyLayoutAsync(bool b = false)
        {
            return Task.FromResult<bool>(true).AsAsyncOperation();
        }

        public event EventHandler<bool> LayoutValidityChanged;
        public event Action<Request,FailToken> ShouldFail;
    }
}