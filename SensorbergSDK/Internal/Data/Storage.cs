using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace SensorbergSDK.Internal
{
    public class Storage
    {
        private SQLiteAsyncConnection _db = null;

        private static Storage _instance;
        public static Storage Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Storage();
                }

                return _instance;
            }
        }

        private Storage()
        {
            _db = new SQLiteAsyncConnection("sensorberg.db");
        }

        //Creates dabases if they don't exist already
        public async Task CreateDBAsync()
        {
            await _db.CreateTableAsync<DBHistoryEvent>();
            await _db.CreateTableAsync<DBHistoryAction>();
            await _db.CreateTableAsync<DBDelayedAction>();
            await _db.CreateTableAsync<DBBackgroundEventsHistory>();
            await _db.CreateTableAsync<DBBeaconActionFromBackground>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resolvedAction"></param>
        /// <param name="dueTime"></param>
        /// <param name="beaconPid"></param>
        /// <param name="eventTypeDetectedByDevice"></param>
        /// <returns></returns>
        public async Task SaveDelayedActionAsync(
            ResolvedAction resolvedAction, DateTimeOffset dueTime, string beaconPid, BeaconEventType eventTypeDetectedByDevice)
        {
            string actionAsString = ResolvedAction.Serialize(resolvedAction);

            DBDelayedAction delayedAction = new DBDelayedAction()
            {
                ResolvedAction = actionAsString,
                DueTime = dueTime,
                BeaconPid = beaconPid,
                EventTypeDetectedByDevice = (int)eventTypeDetectedByDevice,
                Executed = false
            };

            await _db.InsertAsync(delayedAction);
        }

        /// <summary>
        /// Returns delayed actions which should be executed now or maxDelayFromNowInSeconds
        /// seconds in the future.
        /// </summary>
        /// <param name="maxDelayFromNowInSeconds"></param>
        /// <returns></returns>
        public async Task<IList<DBDelayedAction>> GetSerializedDelayedActionsAsync(int maxDelayFromNowInSeconds = 1000)
        {
            IList<DBDelayedAction> actions = new List<DBDelayedAction>();
            DateTimeOffset maxDelayfromNow = DateTimeOffset.Now.AddSeconds(maxDelayFromNowInSeconds);

            var query = _db.Table<DBDelayedAction>().Where(v => v.DueTime < maxDelayfromNow && v.Executed.Equals(false));

            await query.ToListAsync().ContinueWith((t) =>
            {
                actions = t.Result;
            });

            return actions;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxDelayFromNowInSeconds"></param>
        /// <returns></returns>
        public async Task<IList<DelayedActionData>> GetDelayedActionsAsync(int maxDelayFromNowInSeconds = 1000)
        {
            IList<DBDelayedAction> serializedActions = await GetSerializedDelayedActionsAsync(maxDelayFromNowInSeconds);
            IList<DelayedActionData> deserializedActions = new List<DelayedActionData>();

            foreach (DBDelayedAction serializedAction in serializedActions)
            {
                DelayedActionData deserializedAction = new DelayedActionData();
                deserializedAction.Id = serializedAction.Id;
                deserializedAction.resolvedAction = ResolvedAction.Deserialize(serializedAction.ResolvedAction);
                deserializedAction.dueTime = serializedAction.DueTime;
                deserializedAction.beaconPid = serializedAction.BeaconPid;

                try
                {
                    deserializedAction.eventTypeDetectedByDevice = (BeaconEventType)serializedAction.EventTypeDetectedByDevice;
                }
                catch (Exception)
                {
                    deserializedAction.eventTypeDetectedByDevice = BeaconEventType.None;
                }

                deserializedActions.Add(deserializedAction);
            }

            return deserializedActions;
        }

        public async Task SetDelayedActionAsExecutedAsync(int delayedActionId)
        {
            await _db.ExecuteAsync("UPDATE DBDelayedAction SET Executed = 1 WHERE Id = ?", delayedActionId);
        }


        public async Task SaveHistoryActionAsync(string eidIn, string pidIn, DateTimeOffset dtIn, int triggerIn)
        {
            DBHistoryAction action = new DBHistoryAction() { delivered = false, eid = eidIn, pid = pidIn, dt = dtIn, trigger = triggerIn };
            await _db.InsertAsync(action);
        }

        public async Task SaveHistoryEventsAsync(string pidIn, DateTimeOffset dtIn, int triggerIn)
        {
            DBHistoryEvent actions = new DBHistoryEvent() { delivered = false, pid = pidIn, dt = dtIn, trigger = triggerIn };
            await _db.InsertAsync(actions);
        }

        public async Task<DBHistoryAction> GetActionAsync(string eidIn)
        {
            DBHistoryAction result = null;

            var query = _db.Table<DBHistoryAction>().Where(v => v.eid.Equals(eidIn));

            await query.ToListAsync().ContinueWith((t) =>
            {
                foreach (var action in t.Result)
                {
                    result = action;
                    break;
                }
            });

            return result;
        }

        public async Task<IList<DBHistoryAction>> GetActionsAsync(string eidIn)
        {
            IList<DBHistoryAction> result = null;
            var query = _db.Table<DBHistoryAction>().Where(v => v.eid.Equals(eidIn));
            result = await query.ToListAsync();
            return result;
        }


        public async Task<IList<HistoryAction>> GetUndeliveredActionsAsync()
        {
            IList<HistoryAction> actions = new List<HistoryAction>();

            var query = _db.Table<DBHistoryAction>().Where(v => v.delivered.Equals(false));

            try
            {
                await query.ToListAsync().ContinueWith((t) =>
                {
                    foreach (var action in t.Result)
                    {
                        actions.Add(new HistoryAction(action));
                    }
                });
            }
            catch (AggregateException ex)
            {
                System.Diagnostics.Debug.WriteLine("Storage.GetUndeliveredActionsAsync(): " + ex.Message);
            }

            return actions;
        }

        public async Task<IList<HistoryEvent>> GetUndeliveredEventsAsync()
        {
            IList<HistoryEvent> events = new List<HistoryEvent>();

            var query = _db.Table<DBHistoryEvent>().Where(v => v.delivered.Equals(false));
            await query.ToListAsync().ContinueWith((t) =>
            {
                foreach (var evnt in t.Result)
                {
                    events.Add(new HistoryEvent(evnt));
                }
            });

            return events;
        }
        public async Task<IList<DBBackgroundEventsHistory>> GetBeaconBackgroundEventsHistory(string pid)
        {
            IList<DBBackgroundEventsHistory> events = new List<DBBackgroundEventsHistory>();

            var query = _db.Table<DBBackgroundEventsHistory>().Where(v => v.BeaconPid.Equals(pid));
            await query.ToListAsync().ContinueWith((t) =>
            {
                foreach (var evnt in t.Result)
                {
                    events.Add(evnt);
                }
            });

            return events;
        }
        public async Task SaveBeaconActionFromBackgroundAsync(BeaconAction action)
        {
            var beaconString = ActionFactory.Serialize(action);
            var payload = "";
            if (action.Payload != null)
            {
                payload = action.Payload.ToString();
            }
            DBBeaconActionFromBackground dbAction = new DBBeaconActionFromBackground() { BeaconAction = beaconString,Payload = payload };
            await _db.InsertAsync(dbAction);
        }

        /// <summary>
        /// Returns the beacon actions, which have been resolved in the background, but not handled
        /// yet by the user. The returned actions are deleted from the database.
        /// </summary>
        /// <returns>The pending beacon actions resolved by the background task.</returns>
        public async Task<IList<BeaconAction>> GetBeaconActionsFromBackgroundAsync()
        {
            List<BeaconAction> beaconActions = new List<BeaconAction>();
            var query = _db.Table<DBBeaconActionFromBackground>();

            await query.ToListAsync().ContinueWith(async (t) =>
            {
                foreach (var dbBeaconAction in t.Result)
                {
                    BeaconAction beaconAction = ActionFactory.Deserialize(dbBeaconAction.BeaconAction);
                    JsonObject payload;

                    if (JsonObject.TryParse(dbBeaconAction.Payload, out payload))
                    {
                        beaconAction.Payload = payload;
                    }

                    beaconActions.Add(beaconAction);
                    await _db.DeleteAsync(dbBeaconAction);
                }
            });

            return beaconActions;
        }

        public async Task SaveBeaconBackgroundEvent(string pidIn,BeaconEventType triggerIn )
        {
            int eventType = (int)triggerIn;
            DateTimeOffset eventTime = DateTimeOffset.Now;
            DBBackgroundEventsHistory actions = new DBBackgroundEventsHistory() { BeaconPid = pidIn, EventType = eventType, EventTime = eventTime };
            await _db.InsertAsync(actions);
        }
        public async Task UpdateBeaconBackgroundEventAsync(string pidIn, BeaconEventType triggerIn)
        {
            int eventType = (int)triggerIn;
            DateTimeOffset eventTime = DateTimeOffset.Now;
            DBBackgroundEventsHistory backgroundEvent = new DBBackgroundEventsHistory() { BeaconPid = pidIn, EventType = eventType, EventTime = eventTime };
            await _db.UpdateAsync(backgroundEvent);
        }
        public async Task DeleteBackgroundEventAsync(string pidIn)
        {
            DBBackgroundEventsHistory backgroundEvent = new DBBackgroundEventsHistory() { BeaconPid = pidIn };
            await _db.DeleteAsync(backgroundEvent);
        }
        public async Task UpdateBackgroundEventAsync(string pidIn,BeaconEventType eventType)
        {
            int type = (int)eventType;
            DateTimeOffset eventTime = DateTimeOffset.Now;
            DBBackgroundEventsHistory backgroundEvent = new DBBackgroundEventsHistory() { BeaconPid = pidIn, EventTime = eventTime, EventType= type };
            await _db.UpdateAsync(backgroundEvent);
        }

        public async Task SetEventsAsDeliveredAsync()
        {
            await _db.ExecuteAsync("UPDATE DBHistoryEvent SET delivered = 1");
        }

        public async Task SetActionsAsDeliveredAsync()
        {
            await _db.ExecuteAsync("UPDATE DBHistoryAction SET delivered = 1");
        }

        /// <summary>
        /// Cleans old entries from the database
        /// </summary>
        /// <returns></returns>
        public async Task CleanDatabaseAsync()
        {
            try
            {
                Int64 twoHoursAgo = DateTimeOffset.Now.AddHours(-2).UtcTicks;
                Int64 dayAgo = DateTimeOffset.Now.AddDays(-1).UtcTicks;

                //Cleans delivered items that are older than one day from DB
                await _db.ExecuteAsync("DELETE FROM DBHistoryAction WHERE delivered = 1 AND dt < ?", dayAgo);
                await _db.ExecuteAsync("DELETE FROM DBHistoryEvent WHERE delivered = 1 AND dt < ?", dayAgo);

                //Cleans backgroundHistory that is older than 2 hours
                await _db.ExecuteAsync("DELETE FROM DBBackgroundEventsHistory WHERE EventTime < ?", twoHoursAgo);
            } catch (Exception)
            {
                
            }
        }
    }
}
