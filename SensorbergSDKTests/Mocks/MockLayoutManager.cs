// Created by Kay Czarnotta on 09.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SensorbergSDK;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Transport;
using SensorbergSDK.Services;

namespace SensorbergSDKTests.Mocks
{
    public class MockLayoutManager : ILayoutManager
    {
        private MockLayout _layout = new MockLayout();

        public async Task<RequestResultState> ExecuteRequestAsync(Request currentRequest)
        {
            FailToken token = new FailToken();
            if (FindOneAction)
            {
                currentRequest.ResolvedActions = new List<ResolvedAction>() {new ResolvedAction()};
            }
            return token.Fail ? RequestResultState.Failed : RequestResultState.Success;
        }

        public async Task InvalidateLayout()
        {
        }

        public bool IsLayoutValid { get; }

        public Layout Layout
        {
            get { return _layout; }
            set { }
        }

        public bool FindOneAction { get; set; }

        public async Task<bool> VerifyLayoutAsync(bool forceUpdate = false)
        {
            return true;
        }

        public event EventHandler<bool> LayoutValidityChanged;

        public ResolvedAction GetAction(string uuid)
        {
            throw new NotImplementedException();
        }

        public event Action<Request, FailToken> ShouldFail
        {
            add { _layout.ShouldFail += value; }
            remove { _layout.ShouldFail -= value; }
        }
    }

    internal class MockLayout:Layout
    {
        public override IList<ResolvedAction> GetResolvedActionsForPidAndEvent(Request request)
        {
            if (ShouldFail != null)
            {
                FailToken token = new FailToken();
                ShouldFail(request, token);
                if (token.Fail)
                {
                    return null;
                }
            }
            return new List<ResolvedAction>() {new ResolvedAction()};
        }

        public event Action<Request, FailToken> ShouldFail;
    }
}