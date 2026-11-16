using Firebase.Crashlytics;

namespace LSDK.Firebase
{
    internal sealed class FirebaseCrashlyticsService
    {
        private bool _enabled;

        public bool IsEnabled => _enabled;

        public void Enable()
        {
            Crashlytics.IsCrashlyticsCollectionEnabled = true;
            _enabled = true;
        }

        public void SetUserId(string userId)
        {
            if (!_enabled) return;
            Crashlytics.SetUserId(userId ?? string.Empty);
        }

        public void SetCustomKey(string key, string value)
        {
            if (!_enabled || string.IsNullOrEmpty(key)) return;
            Crashlytics.SetCustomKey(key, value ?? string.Empty);
        }

        public void Log(string message)
        {
            if (!_enabled || string.IsNullOrEmpty(message)) return;
            Crashlytics.Log(message);
        }

        public void RecordException(System.Exception exception)
        {
            if (!_enabled || exception == null) return;
            Crashlytics.LogException(exception);
        }
    }
}
