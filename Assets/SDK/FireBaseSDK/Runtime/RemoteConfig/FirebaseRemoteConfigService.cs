using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.RemoteConfig;

namespace LSDK.Firebase
{
    internal sealed class FirebaseRemoteConfigService
    {
        private bool _ready;

        public bool IsReady => _ready;

        /// <summary>Đặt defaults từ dictionary thu gom bởi RemoteConfigManager.</summary>
        public IEnumerator ApplyDefaultsRaw(Dictionary<string, object> defaults)
        {
            if (defaults == null || defaults.Count == 0)
            {
                _ready = true;
                yield break;
            }

            Task task;
            try
            {
                task = FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults);
            }
            catch (Exception exception)
            {
                FirebaseLog.Exception(exception);
                _ready = true;
                yield break;
            }

            yield return FirebaseTasks.Wait(task);
            string error = FirebaseTasks.GetError(task);
            if (error != null) FirebaseLog.Warning(error);
            _ready = true;
        }

        public IEnumerator FetchAndActivate(int cacheExpirationSeconds, Action<bool> done)
        {
            if (cacheExpirationSeconds < 0) cacheExpirationSeconds = 0;
            Task fetch;
            try
            {
                fetch = FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.FromSeconds(cacheExpirationSeconds));
            }
            catch (Exception exception)
            {
                FirebaseLog.Exception(exception);
                done?.Invoke(false);
                yield break;
            }

            yield return FirebaseTasks.Wait(fetch);
            string fetchError = FirebaseTasks.GetError(fetch);
            if (fetchError != null)
            {
                FirebaseLog.Warning(fetchError);
                done?.Invoke(false);
                yield break;
            }

            ConfigInfo info = FirebaseRemoteConfig.DefaultInstance.Info;
            if (info.LastFetchStatus == LastFetchStatus.Failure)
            {
                string reason = info.LastFetchFailureReason.ToString();
                if (info.LastFetchFailureReason == FetchFailureReason.Throttled)
                    reason += " đến " + info.ThrottledEndTime.ToLocalTime().ToString("HH:mm:ss");
                FirebaseLog.Warning("Remote Config fetch thất bại: " + reason);
                done?.Invoke(false);
                yield break;
            }

            FirebaseLog.Info(
                "Remote Config fetch " + info.LastFetchStatus
                + " lúc " + info.FetchTime.ToLocalTime().ToString("HH:mm:ss")
                + ". Cache " + cacheExpirationSeconds + "s. 0 = luôn tải lại.");

            Task<bool> activate;
            try
            {
                activate = FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
            }
            catch (Exception exception)
            {
                FirebaseLog.Exception(exception);
                done?.Invoke(false);
                yield break;
            }

            yield return FirebaseTasks.Wait(activate);
            string activateError = FirebaseTasks.GetError(activate);
            if (activateError != null)
            {
                FirebaseLog.Warning(activateError);
                done?.Invoke(false);
                yield break;
            }

            _ready = true;
            done?.Invoke(true);
        }

        public IEnumerator SetCustomSignals(IDictionary<string, string> signals, Action<bool> done)
        {
            var payload = new Dictionary<string, object>();
            if (signals != null)
            {
                foreach (KeyValuePair<string, string> pair in signals)
                {
                    if (string.IsNullOrEmpty(pair.Key)) continue;
                    payload[pair.Key] = pair.Value ?? string.Empty;
                }
            }

            Task task;
            try
            {
                task = FirebaseRemoteConfig.DefaultInstance.SetCustomSignalsAsync(payload);
            }
            catch (Exception exception)
            {
                FirebaseLog.Exception(exception);
                done?.Invoke(false);
                yield break;
            }

            yield return FirebaseTasks.Wait(task);
            string error = FirebaseTasks.GetError(task);
            if (error != null)
            {
                FirebaseLog.Warning(error);
                done?.Invoke(false);
                yield break;
            }

            done?.Invoke(true);
        }

        public string GetString(string key, string fallback)
        {
            ConfigValue value;
            if (!TryGet(key, out value)) return fallback;
            return value.StringValue;
        }

        public bool GetBool(string key, bool fallback)
        {
            ConfigValue value;
            if (!TryGet(key, out value)) return fallback;
            return value.BooleanValue;
        }

        public long GetLong(string key, long fallback)
        {
            ConfigValue value;
            if (!TryGet(key, out value)) return fallback;
            return value.LongValue;
        }

        public double GetDouble(string key, double fallback)
        {
            ConfigValue value;
            if (!TryGet(key, out value)) return fallback;
            return value.DoubleValue;
        }

        private bool TryGet(string key, out ConfigValue value)
        {
            value = default;
            if (!_ready || string.IsNullOrEmpty(key)) return false;
            try
            {
                value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
            }
            catch (Exception exception)
            {
                FirebaseLog.Exception(exception);
                return false;
            }

            return value.Source != ValueSource.StaticValue;
        }
    }
}
