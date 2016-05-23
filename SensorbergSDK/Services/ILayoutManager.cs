// Created by Kay Czarnotta on 08.03.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Threading.Tasks;
using SensorbergSDK.Internal.Data;
using SensorbergSDK.Internal.Transport;

namespace SensorbergSDK.Services
{
    /// <summary>
    /// Abstraction for the layout manager, to handle all actions about the layout.
    /// </summary>
    public interface ILayoutManager
    {
        /// <summary>
        /// Event for changes on the validity of the layout.
        /// </summary>
        event EventHandler<bool> LayoutValidityChanged;
        /// <summary>
        /// Validates the request against the layout.
        /// </summary>
        /// <param name="currentRequest">Request to validate.</param>
        Task<RequestResultState> ExecuteRequestAsync(Request currentRequest);

        /// <summary>
        /// Invalidates the layout, so a new will received next time.
        /// </summary>
        /// <returns></returns>
        Task InvalidateLayout();

        /// <summary>
        /// Returns true if the layout is still valid.
        /// </summary>
        bool IsLayoutValid { get; }

        /// <summary>
        /// Returns the current Layout or null.
        /// </summary>
        Layout Layout { get; }

        /// <summary>
        /// Verify the currenty layout and update if needed.
        /// </summary>
        /// <param name="forceUpdate">Force an update of the current layout.</param>
        Task<bool> VerifyLayoutAsync(bool forceUpdate = false);

        /// <summary>
        /// Returns the action by the given uuid.
        /// </summary>
        /// <param name="uuid">UUID of the action to searach.</param>
        ResolvedAction GetAction(string uuid);
    }
}