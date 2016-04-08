// Created by Kay Czarnotta on 08.04.2016
// 
// Copyright (c) 2016,  EagleEye .
// 
// All rights reserved.

using SensorbergSDK.Internal;

namespace SensorbergSDKTests.Mocks
{
    public class LayoutManagerExtend:LayoutManager
    {
        public void SetLayout(Layout layout)
        {
            Layout = layout;
        }
    }
}