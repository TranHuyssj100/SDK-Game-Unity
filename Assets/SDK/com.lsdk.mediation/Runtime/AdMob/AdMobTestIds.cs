namespace LSDK.Mediation.AdMob
{
    /// <summary>Ad unit test chính thức của Google. Chỉ dùng khi MediationConfig.useTestAds = true.</summary>
    internal static class AdMobTestIds
    {
        public static string Get(AdFormat format)
        {
#if UNITY_IOS
            switch (format)
            {
                case AdFormat.Banner: return "ca-app-pub-3940256099942544/2934735716";
                case AdFormat.Interstitial: return "ca-app-pub-3940256099942544/4411468910";
                case AdFormat.Rewarded: return "ca-app-pub-3940256099942544/1712485313";
                case AdFormat.AppOpen: return "ca-app-pub-3940256099942544/5575463023";
            }
#else
            switch (format)
            {
                case AdFormat.Banner: return "ca-app-pub-3940256099942544/6300978111";
                case AdFormat.Interstitial: return "ca-app-pub-3940256099942544/1033173712";
                case AdFormat.Rewarded: return "ca-app-pub-3940256099942544/5224354917";
                case AdFormat.AppOpen: return "ca-app-pub-3940256099942544/9257395921";
            }
#endif
            return string.Empty;
        }
    }
}
