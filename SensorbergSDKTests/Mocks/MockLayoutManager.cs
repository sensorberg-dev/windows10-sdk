// Created by Kay Czarnotta on 09.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using SensorbergSDK.Internal;
using SensorbergSDK.Internal.Services;
using SensorbergSDK.Services;

namespace SensorbergSDKTests.Mocks
{
    public class MockLayoutManager:ILayoutManager
    {
        public IAsyncOperation<RequestResultState> ExecuteRequestAsync(Request currentRequest)
        {
            FailToken token = new FailToken();
            if (FindOneAction)
            {
                currentRequest.ResolvedActions = new List<ResolvedAction>() {new ResolvedAction()};
            }
            ShouldFail?.Invoke(currentRequest, token);
            return Task.FromResult<RequestResultState>(token.Fail ? RequestResultState.Failed : RequestResultState.Success).AsAsyncOperation();
        }

        public async Task InvalidateLayout()
        {
        }

        public bool IsLayoutValid { get; }
        public Layout Layout { get; }
        public bool FindOneAction { get; set; }

        public IAsyncOperation<bool> VerifyLayoutAsync(bool b = false)
        {
            return Task.FromResult<bool>(true).AsAsyncOperation();
        }

        public event EventHandler<bool> LayoutValidityChanged;
        public event Action<Request,FailToken> ShouldFail;
    }
}