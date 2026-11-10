using System;
using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;

namespace LSDK.Mediation.AdMob
{
    /// <summary>
    /// Quản lý UMP consent (GDPR/CCPA) cho AdMob.
    /// Luồng: GatherConsent → update info → show form nếu cần → callback CanRequestAds.
    /// </summary>
    internal static class AdMobConsentManager
    {
        /// <summary>true nếu UMP yêu cầu app hiện nút mở Privacy Options.</summary>
        public static bool IsPrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        // ── Static refs giữ AndroidJavaProxy sống đến khi callback fire ──
        // Unity IL2CPP GC có thể thu hồi AndroidJavaProxy nếu không có strong reference.
        // UMP dùng AndroidJavaProxy cho OnConsentInfoUpdateSuccessListener và
        // OnConsentFormDismissedListener. Nếu proxy bị GC trước khi server phản hồi,
        // callback im lặng hoàn toàn (không lỗi, không log).
        private static Action<FormError> _updateCallbackRef;
        private static Action<FormError> _formCallbackRef;

        // ───────────────────────── Gather consent ─────────────────────────
        /// <summary>
        /// Xin consent lúc khởi động. Callback trả true nếu có thể request ad.
        /// Nếu skipConsent = true thì bỏ qua và callback ngay.
        /// </summary>
        public static void GatherConsent(MediationConfig config, Action<bool> onComplete)
        {
            if (config.skipConsent)
            {
                DebugUtils.Success("Consent: bỏ qua (skipConsent = true).");
                onComplete?.Invoke(true);
                return;
            }

            DebugUtils.Success($"Consent: GatherConsent bắt đầu. DebugGeo={config.consentDebugGeography}, " +
                               $"HashIds={config.consentTestDeviceHashedIds?.Count ?? 0}, " +
                               $"CurrentStatus={ConsentInformation.ConsentStatus}.");

            var requestParams = BuildRequestParams(config);

            DebugUtils.Success("Consent: gọi ConsentInformation.Update...");

            // Giữ strong reference — ngăn GC thu hồi AndroidJavaProxy bên trong UMP
            _updateCallbackRef = updateError =>
            {
                _updateCallbackRef = null;

                if (updateError != null)
                {
                    DebugUtils.Warning($"Consent update lỗi [{updateError.ErrorCode}]: {updateError.Message}. Dùng consent phiên trước.");
                    // Dùng status từ phiên trước nếu có, không block ad
                    onComplete?.Invoke(ConsentInformation.CanRequestAds());
                    return;
                }

                DebugUtils.Success($"Consent status: {ConsentInformation.ConsentStatus}. CanRequestAds: {ConsentInformation.CanRequestAds()}.");

                // Giữ strong reference cho form callback cũng vì lý do tương tự
                _formCallbackRef = formError =>
                {
                    _formCallbackRef = null;

                    if (formError != null)
                        DebugUtils.Warning($"Consent form lỗi [{formError.ErrorCode}]: {formError.Message}.");
                    else
                        DebugUtils.Success("Consent form hoàn tất.");

                    onComplete?.Invoke(ConsentInformation.CanRequestAds());
                };

                // Show form nếu cần (không làm gì nếu không cần)
                ConsentForm.LoadAndShowConsentFormIfRequired(_formCallbackRef);
            };

            DebugUtils.Success("Consent: ConsentInformation.Update đã được gọi, chờ callback...");
            ConsentInformation.Update(requestParams, _updateCallbackRef);
        }

        // ───────────────────────── Privacy Options ─────────────────────────
        /// <summary>
        /// Mở Privacy Options Form khi user bấm nút trong Settings.
        /// onComplete(errorMessage) — null nếu thành công.
        /// </summary>
        public static void ShowPrivacyOptionsForm(Action<string> onComplete)
        {
            ConsentForm.ShowPrivacyOptionsForm(formError =>
            {
                if (formError != null)
                {
                    DebugUtils.Warning($"Privacy options form lỗi [{formError.ErrorCode}]: {formError.Message}");
                    onComplete?.Invoke(formError.Message);
                }
                else
                {
                    DebugUtils.Success("Privacy options form hoàn tất.");
                    onComplete?.Invoke(null);
                }
            });
        }

        // ───────────────────────── Debug / Testing ─────────────────────────
        /// <summary>
        /// Reset toàn bộ consent state — CHỈ dùng khi test, không gọi trên production.
        /// </summary>
        public static void Reset()
        {
            ConsentInformation.Reset();
            DebugUtils.Warning("Consent: đã reset (chỉ dùng khi test!).");
        }

        // ───────────────────────── Helpers ─────────────────────────
        private static ConsentRequestParameters BuildRequestParams(MediationConfig config)
        {
            var request = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = config.childDirected == ChildDirectedMode.True
            };

            bool hasDebugDevices = config.consentTestDeviceHashedIds != null &&
                                   config.consentTestDeviceHashedIds.Count > 0;
            bool hasDebugGeo = config.consentDebugGeography != ConsentDebugGeography.Disabled;

            if (!hasDebugDevices && !hasDebugGeo) return request;

            var debugSettings = new ConsentDebugSettings();

            if (hasDebugDevices)
                debugSettings.TestDeviceHashedIds = new List<string>(config.consentTestDeviceHashedIds);

            switch (config.consentDebugGeography)
            {
                case ConsentDebugGeography.EEA:
                    debugSettings.DebugGeography = DebugGeography.EEA;
                    break;
                case ConsentDebugGeography.NotEEA:
                    debugSettings.DebugGeography = DebugGeography.Other;
                    break;
            }

            request.ConsentDebugSettings = debugSettings;
            return request;
        }
    }
}
