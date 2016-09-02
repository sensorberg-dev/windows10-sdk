// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.UI.Popups;
using SensorbergSDK.Internal;

namespace SensorbergSDK
{
    /// <summary>
    /// Type of BeaconAction.
    /// </summary>
    [DataContract]
    public enum BeaconActionType
    {
        UrlMessage = Constants.ActionTypeUrlMessage,
        VisitWebsite = Constants.ActionTypeVisitWebsite,
        InApp = Constants.ActionTypeInApp,
        Silent = Constants.ActionTypeSilent,
    }

    /// <summary>
    /// Represents an action resolved based on a beacon event.
    /// </summary>
    [DataContract]
    public sealed class BeaconAction
    {
        private const char FieldSeparator = ';'; // For FromString() and ToString()
        private string _payloadString;

        public BeaconAction()
        {
            Payload = null;
        }

        /// <summary>
        /// Id of action.
        /// </summary>
        [DataMember]
        public int Id { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        /// <summary>
        /// Type of action.
        /// </summary>
        [DataMember]
        public BeaconActionType Type { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        /// <summary>
        /// UUID of action. This is received from the receiver.
        /// </summary>
        [DataMember(Name = "eid")]
        public string Uuid { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        /// <summary>
        /// Subject for the action.
        /// </summary>
        [DataMember]
        public string Subject { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        /// <summary>
        /// Body of the action.
        /// </summary>
        [DataMember]
        public string Body { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        /// <summary>
        /// URL to open for the action.
        /// </summary>
        [DataMember]
        public string Url { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }

        /// <summary>
        /// String representation of the payload.
        /// </summary>
        [DataMember]
        public string PayloadString
        {
            get { return string.IsNullOrEmpty(_payloadString) ? Payload?.ToString() : _payloadString; }
            set { _payloadString = value; }
        }

        /// <summary>
        /// Payload message set for Action on the service. 
        /// Value is null, if payload is not set.
        /// </summary>
        public JsonObject Payload { [DebuggerStepThrough] get; [DebuggerStepThrough] set; }


        /// <summary>
        /// Validates the received action.
        /// Requirements for each action type:
        /// - URL message: Mandatory: subject, body, URL
        /// - Visit website: Optional: subject, body. Mandatory URL
        /// - In-app: Optional: subject, body. Mandatory: URL.
        /// </summary>
        /// <returns>True, if valid. False otherwise.</returns>
        public bool Validate()
        {
            bool valid = false;

            switch (Type)
            {
                case BeaconActionType.UrlMessage:
                    if (Subject.Length > 0 && Url.Length > 0 && Body.Length > 0)
                    {
                        valid = true;
                    }

                    break;
                case BeaconActionType.VisitWebsite:
                case BeaconActionType.InApp:
                    if (Url.Length > 0)
                    {
                        valid = true;
                    }

                    break;
            }

            return valid;
        }

        /// <summary>
        /// Tries to launch the web browser with the URL of this beacon action.
        /// Note that the type of this action must be VisitWebsite or no browser is launched.
        /// </summary>
        /// <returns>True, if the web browser was launched. False otherwise.</returns>
        public async Task<bool> LaunchWebBrowserAsync()
        {
            bool webBrowserLaunched = false;

            if (Type == BeaconActionType.VisitWebsite && !string.IsNullOrEmpty(Url))
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(Url));
                webBrowserLaunched = true;
            }

            return webBrowserLaunched;
        }


        /// <summary>
        /// Converts this instance to a string.
        /// </summary>
        /// <returns>A string representation of this instance.</returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(Id.ToString());
            stringBuilder.Append(FieldSeparator);
            stringBuilder.Append(Type.ToString());
            stringBuilder.Append(FieldSeparator);
            stringBuilder.Append(string.IsNullOrEmpty(Subject) ? " " : Subject);
            stringBuilder.Append(FieldSeparator);
            stringBuilder.Append(string.IsNullOrEmpty(Body) ? " " : Body);
            stringBuilder.Append(FieldSeparator);
            stringBuilder.Append(string.IsNullOrEmpty(Url) ? " " : Url);

            if (Payload != null)
            {
                stringBuilder.Append(FieldSeparator);
                stringBuilder.Append(Payload.Stringify());
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Creates a message dialog based on the data of this beacon action.
        /// Note that no commands is added to the created dialog.
        /// </summary>
        /// <returns>A newly created message dialog instance.</returns>
        public MessageDialog ToMessageDialog()
        {
            string message = string.Empty;

            switch (Type)
            {
                case BeaconActionType.UrlMessage:
                case BeaconActionType.VisitWebsite:
                    message = string.Format("{0}\nVisit {1}?", Body, Url);
                    break;
                case BeaconActionType.InApp:
                    message = Body;
                    break;
            }

            MessageDialog messageDialog = new MessageDialog(message, Subject);
            return messageDialog;
        }

        private bool Equals(BeaconAction other)
        {
            return Id == other.Id && Type == other.Type && string.Equals(Uuid, other.Uuid) && string.Equals(Subject, other.Subject) && string.Equals(Body, other.Body) &&
                   string.Equals(Url, other.Url) && Equals(Payload?.ToString(), other.Payload?.ToString());
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is BeaconAction && Equals((BeaconAction) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode*397) ^ (int) Type;
                hashCode = (hashCode*397) ^ (Uuid != null ? Uuid.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Subject != null ? Subject.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Body != null ? Body.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Url != null ? Url.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Payload != null ? Payload.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(BeaconAction left, BeaconAction right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BeaconAction left, BeaconAction right)
        {
            return !Equals(left, right);
        }
    }
}
