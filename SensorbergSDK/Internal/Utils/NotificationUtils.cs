// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace SensorbergSDK.Internal.Utils
{
    public static class NotificationUtils
    {
        private static readonly string KeyLaunch = "launch";
        private static readonly string KeyText = "text";
        private static readonly string KeyToast = "toast";

        /// <summary>
        /// Creates a toast notification template and populates it with the given data.
        /// </summary>
        /// <param name="beaconActionType"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="url"></param>
        /// <returns>A newly created toast notification template.</returns>
        public static XmlDocument CreateToastTemplate(BeaconActionType beaconActionType, string subject, string body, string url)
        {
            XmlDocument toastTemplate = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText04);
            XmlNodeList toastTextAttributes = toastTemplate.GetElementsByTagName(KeyText);
            int textAttributeIndex = 0;

            if (!string.IsNullOrEmpty(subject))
            {
                toastTextAttributes[textAttributeIndex].InnerText = subject;
                textAttributeIndex++;
            }

            if (!string.IsNullOrEmpty(body))
            {
                toastTextAttributes[textAttributeIndex].InnerText = body;
                textAttributeIndex++;
            }

            if (!string.IsNullOrEmpty(url))
            {
                toastTextAttributes[textAttributeIndex].InnerText = url;
            }

            return toastTemplate;
        }

        /// <summary>
        /// Creates a toast notification instance based on the data of the given beacon action.
        /// </summary>
        /// <returns>A newly created toast notification.</returns>
        public static ToastNotification CreateToastNotification(BeaconAction beaconAction)
        {
            XmlDocument toastTemplate =
                CreateToastTemplate(beaconAction.Type, beaconAction.Subject, beaconAction.Body, beaconAction.Url);

            XmlAttribute urlAttribute = toastTemplate.CreateAttribute(KeyLaunch);

            string beaconActionAsString = beaconAction.ToString();
            urlAttribute.Value = beaconActionAsString;

            XmlNodeList toastElementList = toastTemplate.GetElementsByTagName(KeyToast);
            XmlElement toastElement = toastElementList[0] as XmlElement;

            if (toastElement != null)
            {
                toastElement.SetAttribute(KeyLaunch, beaconActionAsString);
            }

            return new ToastNotification(toastTemplate);
        }

        /// <summary>
        /// For testing and debugging.
        /// </summary>
        public static ToastNotification CreateToastNotification(string textLine1, string textLine2 = null, string textLine3 = null)
        {
            XmlDocument toastTemplate = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText04);
            XmlNodeList toastTextAttributes = toastTemplate.GetElementsByTagName(KeyText);
            int textAttributeIndex = 0;

            if (!string.IsNullOrEmpty(textLine1))
            {
                toastTextAttributes[textAttributeIndex].InnerText = textLine1;
                textAttributeIndex++;
            }

            if (!string.IsNullOrEmpty(textLine2))
            {
                toastTextAttributes[textAttributeIndex].InnerText = textLine2;
                textAttributeIndex++;
            }

            if (!string.IsNullOrEmpty(textLine3))
            {
                toastTextAttributes[textAttributeIndex].InnerText = textLine3;
            }

            return new ToastNotification(toastTemplate);
        }

        public static void ShowToastNotification(ToastNotification toastNotification)
        {
            if (toastNotification != null)
            {
                ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
            }
        }
    }
}
