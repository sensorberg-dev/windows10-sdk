// Created by Kay Czarnotta on 04.05.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Collections.Generic;
using Windows.Data.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SensorbergSDK.Internal.Transport.Converter
{
    public class ResolvedActionConverter: JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            List<ResolvedAction> resolvedActions = new List<ResolvedAction>();

            JArray actionsArray = JArray.Load(reader);
            foreach (JObject jobject in actionsArray)
            {
                ResolvedAction resolvedAction = new ResolvedAction();
                serializer.Populate(jobject.CreateReader(), resolvedAction);
                resolvedAction.BeaconAction = new BeaconAction();
                serializer.Populate(jobject.CreateReader(), resolvedAction.BeaconAction);
                if (jobject["content"] != null)
                {
                    serializer.Populate(jobject["content"]?.CreateReader(), resolvedAction.BeaconAction);
                    resolvedAction.BeaconAction.PayloadString = jobject["content"]["payload"].ToString();
                    // create json object for fallback
                    if(!string.IsNullOrEmpty(resolvedAction.BeaconAction.PayloadString))
                    {
                        resolvedAction.BeaconAction.Payload = JsonObject.Parse(resolvedAction.BeaconAction.PayloadString);
                    }
                }
                resolvedActions.Add(resolvedAction);
            }

            return resolvedActions;
        }
    }
}