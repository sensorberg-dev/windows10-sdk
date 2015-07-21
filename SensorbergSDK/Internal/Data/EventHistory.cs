using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Net;
using System.Threading;

namespace SensorbergSDK.Internal
{
    /// <summary>
    /// Event storage. It stores all past beacon events and actions associated with the events.
    /// </summary>
    public sealed class EventHistory
    {
        private AutoResetEvent _asyncWaiter;
        private Storage _storage;

        public EventHistory()
        {
            _asyncWaiter = new AutoResetEvent(true);
            _storage = Storage.Instance;
        }

        /// <summary>
        /// If sendOnlyOnce is true for resolved action, fuction will check from the history if the
        /// action is already presented for the user.
        /// </summary>
        /// <param name="resolvedAction"></param>
        /// <returns>True ,if action type is SendOnlyOnce, and it has been shown already. Otherwise false.</returns>
        public async Task<bool> CheckSendOnlyOnceAsync(ResolvedAction resolvedAction)
        {
            bool sendonlyOnce = false;

            if (resolvedAction.SendOnlyOnce)
            {
                try
                {
                    _asyncWaiter.WaitOne();
                    DBHistoryAction dbHistoryAction = await _storage.GetActionAsync(resolvedAction.BeaconAction.Uuid);

                    if (dbHistoryAction != null)
                    {
                        sendonlyOnce = true;
                    }

                }
                finally
                {
                    _asyncWaiter.Set();
                }
            }

            return sendonlyOnce;
        }

        /// <summary>
        /// If supressionTime is set for the action, fuction will check from the history if the
        /// action is already presented during the supression time.
        /// </summary>
        /// <param name="resolvedAction"></param>
        /// <returns>True only if action should be supressed.</returns>
        public async Task<bool> ShouldSupressAsync(ResolvedAction resolvedAction)
        {
            bool suppress = false;

            if (resolvedAction.SupressionTime > 0)
            {
                try
                {
                    _asyncWaiter.WaitOne();
                    IList<DBHistoryAction> dbHistoryActions = await _storage.GetActionsAsync(resolvedAction.BeaconAction.Uuid);

                    if (dbHistoryActions != null)
                    {
                        foreach (var dbHistoryAction in dbHistoryActions)
                        {
                            var action_timestamp = dbHistoryAction.dt.AddSeconds(resolvedAction.SupressionTime);

                            if (action_timestamp > DateTimeOffset.Now)
                            {
                                suppress = true;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    _asyncWaiter.Set();
                }
            }

            return suppress;
        }

        /// <summary>
        /// Stores a beacon event to the database.
        /// </summary>
        /// <param name="eventArgs"></param>
        public IAsyncAction SaveBeaconEventAsync(BeaconEventArgs eventArgs)
        {
            return _storage.SaveHistoryEventsAsync(eventArgs.Beacon.Pid, eventArgs.Timestamp, (int)eventArgs.EventType).AsAsyncAction();
        }

        /// <summary>
        /// Stores a resolved and executed action to the database.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <param name="beaconAction"></param>
        public IAsyncAction SaveExecutedResolvedActionAsync(ResolvedActionsEventArgs eventArgs, BeaconAction beaconAction)
        {
            return _storage.SaveHistoryActionAsync(
                beaconAction.Uuid, eventArgs.BeaconPid, DateTime.Now, (int)eventArgs.BeaconEventType).AsAsyncAction();
        }

        /// <summary>
        /// For convenience.
        /// </summary>
        /// <param name="beaconAction"></param>
        /// <param name="beaconPid"></param>
        /// <param name="beaconActionType"></param>
        /// <returns></returns>
        public IAsyncAction SaveExecutedResolvedActionAsync(BeaconAction beaconAction, string beaconPid, BeaconEventType beaconEventType)
        {
            return _storage.SaveHistoryActionAsync(
                beaconAction.Uuid, beaconPid, DateTime.Now, (int)beaconEventType).AsAsyncAction();
        }

        /// <summary>
        /// Checks if there are new events or actions in the history and sends them to the server.
        /// </summary>
        public IAsyncAction FlushHistoryAsync()
        {
            Func<Task> action = async () =>
            {
                try
                {
                    History history = new History();
                    history.actions = await _storage.GetUndeliveredActionsAsync();
                    history.events = await _storage.GetUndeliveredEventsAsync();

                    if ((history.events != null && history.events.Count > 0) || (history.actions != null && history.actions.Count > 0))
                    {
                        MemoryStream stream1 = new MemoryStream();
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(History));
                        ser.WriteObject(stream1, history);
                        stream1.Position = 0;
                        StreamReader sr = new StreamReader(stream1);

                        HttpClient httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Add(Constants.XApiKey, SDKData.Instance.ApiKey);
                        httpClient.DefaultRequestHeaders.Add(Constants.Xiid, SDKData.Instance.DeviceId);
                        var content = new StringContent(sr.ReadToEnd(), Encoding.UTF8, "application/json");

                        HttpResponseMessage responseMessage = await httpClient.PostAsync(new Uri(Constants.LayoutApiUriAsString), content);

                        if (responseMessage.StatusCode == HttpStatusCode.OK)
                        {
                            //TODO: When the server is ready move lines from the below here. Server needs to answer 400 OK for us
                            //to set events and actions as delivered state
                        }

                        if ((history.events != null && history.events.Count > 0))
                        {
                            await _storage.SetEventsAsDeliveredAsync();
                        }

                        if (history.actions != null && history.actions.Count > 0)
                        {
                            await _storage.SetActionsAsDeliveredAsync();
                        }
                    }
                }
                catch (Exception)
                {
                }

            };

            return action().AsAsyncAction();
        }
    }
}


