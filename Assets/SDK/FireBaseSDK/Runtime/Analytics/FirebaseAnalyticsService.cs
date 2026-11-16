using Firebase.Analytics;

namespace LSDK.Firebase
{
    internal sealed class FirebaseAnalyticsService
    {
        private bool _enabled;

        public bool IsEnabled => _enabled;

        public void Enable()
        {
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            _enabled = true;
        }

        public void SetUserId(string userId)
        {
            if (!_enabled) return;
            FirebaseAnalytics.SetUserId(userId ?? string.Empty);
        }

        public void SetUserProperty(string name, string value)
        {
            if (!_enabled || string.IsNullOrEmpty(name)) return;
            FirebaseAnalytics.SetUserProperty(name, value ?? string.Empty);
        }

        public void LogEvent(string name)
        {
            if (!_enabled || string.IsNullOrEmpty(name)) return;
            FirebaseAnalytics.LogEvent(name);
        }

        public void LogEvent(string name, string parameterName, string parameterValue)
        {
            if (!_enabled || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(parameterName)) return;
            FirebaseAnalytics.LogEvent(name, parameterName, parameterValue ?? string.Empty);
        }

        public void LogEvent(string name, string parameterName, long parameterValue)
        {
            if (!_enabled || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(parameterName)) return;
            FirebaseAnalytics.LogEvent(name, parameterName, parameterValue);
        }

        public void LogEvent(string name, string parameterName, double parameterValue)
        {
            if (!_enabled || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(parameterName)) return;
            FirebaseAnalytics.LogEvent(name, parameterName, parameterValue);
        }

        public void LogAdRevenue(string adPlatform, string adSource, string adFormat, string adUnit, double value, string currency)
        {
            if (!_enabled) return;
            FirebaseAnalytics.LogEvent(
                FirebaseAnalytics.EventAdImpression,
                new Parameter(FirebaseAnalytics.ParameterAdPlatform, adPlatform ?? string.Empty),
                new Parameter(FirebaseAnalytics.ParameterAdSource, adSource ?? string.Empty),
                new Parameter(FirebaseAnalytics.ParameterAdFormat, adFormat ?? string.Empty),
                new Parameter(FirebaseAnalytics.ParameterAdUnitName, adUnit ?? string.Empty),
                new Parameter(FirebaseAnalytics.ParameterValue, value),
                new Parameter(FirebaseAnalytics.ParameterCurrency, string.IsNullOrEmpty(currency) ? "USD" : currency));
        }
    }
}
