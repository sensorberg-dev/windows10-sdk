using SensorbergSDK.Internal;
using System;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml.Navigation;

namespace SensorbergSDK
{
    [DataContract]
    public enum BeaconActionType
    {
        UrlMessage = Constants.ActionTypeUrlMessage,
        VisitWebsite = Constants.ActionTypeVisitWebsite,
        InApp = Constants.ActionTypeInApp
    };

    /// <summary>
    /// Represents an action resolved based on a beacon event.
    /// </summary>
    [DataContract]
    public sealed class BeaconAction
    {
        private const char FieldSeparator = ';'; // For FromString() and ToString()

        public BeaconAction()
        {
            Payload = null;
        }

        [DataMember]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public BeaconActionType Type
        {
            get;
            set;
        }

        [DataMember]
        public string Uuid
        {
            get;
            set;
        }

        [DataMember]
        public string Subject
        {
            get;
            set;
        }

        [DataMember]
        public string Body
        {
            get;
            set;
        }

        [DataMember]
        public string Url
        {
            get;
            set;
        }

        /// <summary>
        /// Payload message set for Action on the service. 
        /// Value is null, if payload is not set.
        /// </summary>
        public JsonObject Payload
        {
            get;
            set;
        }

        /// <summary>
        /// Sets the beacon action type based on the given value.
        /// </summary>
        /// <param name="type">The type as integer.</param>
        public void SetType(int type)
        {
            switch (type)
            {
                case Constants.ActionTypeUrlMessage:
                    Type = BeaconActionType.UrlMessage;
                    break;
                case Constants.ActionTypeVisitWebsite:
                    Type = BeaconActionType.VisitWebsite;
                    break;
                case Constants.ActionTypeInApp:
                    Type = BeaconActionType.InApp;
                    break;
                default:
                    throw new ArgumentException("Invalid type (" + type + ")");
            }
        }

        /// <summary>
        /// Sets the beacon action type based on the given value.
        /// </summary>
        /// <param name="type">The type as string.</param>
        /// <returns>True, if the type was set. False otherwise.</returns>
        public bool SetType(string type)
        {
            bool wasSet = false;

            if (!string.IsNullOrEmpty(type))
            {
                if (type.Equals(BeaconActionType.InApp.ToString()))
                {
                    Type = BeaconActionType.InApp;
                    wasSet = true;
                }
                else if (type.Equals(BeaconActionType.UrlMessage.ToString()))
                {
                    Type = BeaconActionType.UrlMessage;
                    wasSet = true;
                }
                else if (type.Equals(BeaconActionType.VisitWebsite.ToString()))
                {
                    Type = BeaconActionType.VisitWebsite;
                    wasSet = true;
                }
            }

            return wasSet;
        }

        /// <summary>
        /// Validates the received action.
        /// 
        /// Requirements for each action type:
        /// - URL message: Mandatory: subject, body, URL
        /// - Visit website: Optional: subject, body. Mandatory URL
        /// - In-app: Optional: subject, body. Mandatory: URL
        /// </summary>
        /// <returns>True, if valid. False otherwise.</returns>
        public bool Validate()
        {
            bool valid = false;

            switch (Type) {
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
        /// For convenience. Tries to create a beacon action instance from the parameter of the
        /// given navigation event arguments.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>A newly created beacon action instance, if successful. Null in case of an error.</returns>
        public static BeaconAction FromNavigationEventArgs(NavigationEventArgs args)
        {
            if (args != null && args.Parameter != null && args.Parameter is string)
            {
                return BeaconAction.FromString(args.Parameter as string);
            }

            return null;
        }

        /// <summary>
        /// Creates a beacon action instance from the given string.
        /// </summary>
        /// <param name="beaconActionAsString">The beacon action as string.</param>
        /// <returns>A newly created beacon action instance, if successful. Null in case of an error.</returns>
        public static BeaconAction FromString(string beaconActionAsString)
        {
            BeaconAction beaconAction = null;

            if (!string.IsNullOrEmpty(beaconActionAsString))
            {
                string[] fields = beaconActionAsString.Split(FieldSeparator);

                if (fields.Length >= 2)
                {
                    beaconAction = new BeaconAction();

                    for (int i = 0; i < fields.Length; ++i)
                    {
                        if (fields[i].Trim().Length > 0)
                        {
                            switch (i)
                            {
                                case 0: // Id
                                    int id = 0;

                                    try
                                    {
                                        int.TryParse(fields[i], out id);
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    beaconAction.Id = id;
                                    break;
                                case 1: // Type
                                    beaconAction.SetType(fields[i]);
                                    break;
                                case 2: // Subject
                                    beaconAction.Subject = fields[i];
                                    break;
                                case 3: // Body
                                    beaconAction.Body = fields[i];
                                    break;
                                case 4: // Url
                                    beaconAction.Url = fields[i];
                                    break;
                                case 5: // Payload
                                    JsonObject payload = null;
                                    bool jsonObjectParsed = false;

                                    try
                                    {
                                        jsonObjectParsed = JsonObject.TryParse(fields[i], out payload);
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    if (jsonObjectParsed)
                                    {
                                        beaconAction.Payload = payload;
                                    }

                                    break;
                            }
                        }
                    }
                }
            }

            return beaconAction;
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
        /// Creates a toast notification instance with populates it with data associated to this
        /// beacon action.
        /// </summary>
        /// <returns>A newly created toast notification.</returns>
        public ToastNotification ToToastNotification()
        {
            return NotificationUtils.CreateToastNotification(this);
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
    }
}
