using System;
using System.IO;
using System.Runtime.Serialization.Json;
using Windows.Data.Json;

namespace SensorbergSDK.Internal
{
    public sealed class ActionFactory
    {
        private static readonly string KeySubject = "subject";
        private static readonly string KeyBody = "body";
        private static readonly string KeyUrl = "url";
        private static readonly string KeyPayload = "payload";
        
        /// <summary>
        /// Parses and creates a beacon action instance from the given data.
        /// </summary>
        /// <param name="actionType"></param>
        /// <param name="message"></param>
        /// <param name="actionUuid"></param>
        /// <returns>A newly created BeaconAction instance or null in case of a failure.</returns>
        public static BeaconAction CreateBeaconAction(int actionType, JsonObject message, string actionUuid)
        {
            BeaconAction beaconAction = null;

            if (message != null)
            {
                JsonObject payload = null;
                IJsonValue jsonValue;

                if (message.TryGetValue(KeyPayload, out jsonValue))
                {
                    payload = JsonHelper.OptionalObject(message, KeyPayload);
                }

                switch (actionType)
                {
                    case Constants.ActionTypeUrlMessage:
                        beaconAction = new BeaconAction
                        {
                            Uuid = actionUuid,
                            Subject = message.GetNamedString(KeySubject),
                            Body = message.GetNamedString(KeyBody),
                            Url = message.GetNamedString(KeyUrl),
                            Payload = payload
                        };

                        beaconAction.SetType(Constants.ActionTypeUrlMessage);
                        break;

                    case Constants.ActionTypeVisitWebsite:
                        beaconAction = new BeaconAction
                        {
                            Uuid = actionUuid,
                            Subject = JsonHelper.OptionalString(message, KeySubject),
                            Body = JsonHelper.OptionalString(message, KeyBody),
                            Url = JsonHelper.OptionalString(message, KeyUrl),
                            Payload = payload
                        };

                        beaconAction.SetType(Constants.ActionTypeVisitWebsite);
                        break;

                    case Constants.ActionTypeInApp:
                        beaconAction = new BeaconAction
                        {
                            Uuid = actionUuid,
                            Subject = JsonHelper.OptionalString(message, KeySubject),
                            Body = JsonHelper.OptionalString(message, KeyBody),
                            Url = JsonHelper.OptionalString(message, KeyUrl),
                            Payload = payload
                        };

                        beaconAction.SetType(Constants.ActionTypeInApp);
                        break;
                }
            }

            return beaconAction;
        }

        /// <summary>
        /// Serializes the given BeaconAction instance.
        /// </summary>
        /// <param name="beaconAction">The instance to serialize.</param>
        /// <returns>The serialized instance as string.</returns>
        public static string Serialize(BeaconAction beaconAction)
        {
            string serializedBeaconAction = string.Empty;

            if (beaconAction != null)
            {
                MemoryStream stream = new MemoryStream();
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(BeaconAction));
                bool success = false;

                try
                {
                    serializer.WriteObject(stream, beaconAction);
                    success = true;
                }
                catch (Exception)
                {
                }

                stream.Position = 0;
                StreamReader streamReader = new StreamReader(stream);

                if (success)
                {
                    try
                    {
                        serializedBeaconAction = streamReader.ReadToEnd();
                    }
                    catch (Exception)
                    {
                    }

                    System.Diagnostics.Debug.WriteLine("ActionFactory.Serialize(): " + serializedBeaconAction);
                }
            }

            return serializedBeaconAction;
        }

        /// <summary>
        /// Deserializes the given serialized BeaconAction.
        /// </summary>
        /// <param name="serializedBeaconAction">The serialized BeaconAction as string.</param>
        /// <returns>The deserialized BeaconAction instance.</returns>
        public static BeaconAction Deserialize(string serializedBeaconAction)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(BeaconAction));
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serializedBeaconAction));
            return (BeaconAction)serializer.ReadObject(stream);
        }
    }
}
