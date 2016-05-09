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
using SensorbergSDK.Services;

namespace SensorbergSDKTests.Mocks
{
    public class MockLayoutManager:ILayoutManager
    {
        public async Task<RequestResultState> ExecuteRequestAsync(Request currentRequest)
        {
            FailToken token = new FailToken();
            ShouldFail?.Invoke(currentRequest, token);
            if (FindOneAction)
            {
                currentRequest.ResolvedActions = new List<ResolvedAction>() { new ResolvedAction() };
            }
            return token.Fail ? RequestResultState.Failed : RequestResultState.Success;
        }

        public async Task InvalidateLayout()
        {
        }

        public bool IsLayoutValid { get; }
        public Layout Layout { get; set; }
        public bool FindOneAction { get; set; }

        public async Task<bool> VerifyLayoutAsync(bool b = false)
        {
            return true;
        }

        public event EventHandler<bool> LayoutValidityChanged;
        public ResolvedAction GetAction(string actionId)
        {
            throw new NotImplementedException();
        }

        public event Action<Request,FailToken> ShouldFail;
    }
}