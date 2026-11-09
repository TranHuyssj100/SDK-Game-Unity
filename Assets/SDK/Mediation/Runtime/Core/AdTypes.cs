namespace LSDK.Mediation
{
    /// <summary>Các định dạng quảng cáo SDK hỗ trợ.</summary>
    public enum AdFormat
    {
        Banner = 0,
        Interstitial = 1,
        Rewarded = 2,
        AppOpen = 3
    }

    /// <summary>Bên cung cấp ad. KHÔNG đổi thứ tự (được serialize trong ScriptableObject).</summary>
    public enum AdNetworkType
    {
        AdMob = 0,
        IronSource = 1,
        AppLovin = 2,
        UnityAds = 3
    }

    public enum BannerPosition
    {
        Top = 0,
        Bottom = 1
    }

    public enum ChildDirectedMode
    {
        Unspecified = 0,
        True = 1,
        False = 2
    }

    /// <summary>Giả lập vùng địa lý để test UMP consent form (chỉ dùng khi debug).</summary>
    public enum ConsentDebugGeography
    {
        /// <summary>Không giả lập — dùng vị trí thực của thiết bị.</summary>
        Disabled = 0,
        /// <summary>Giả lập user ở EEA (GDPR). Form consent sẽ hiện.</summary>
        EEA = 1,
        /// <summary>Giả lập user ngoài EEA. Form consent không hiện.</summary>
        NotEEA = 2
    }

    /// <summary>Kết quả trả về cho callback khi show fullscreen ad.</summary>
    public enum AdShowResult
    {
        /// <summary>Interstitial/AppOpen: đã show và đóng. Rewarded: user đã nhận thưởng (callback gọi SAU khi đóng ad).</summary>
        Completed,
        /// <summary>Rewarded: user đóng ad trước khi nhận thưởng.</summary>
        NotCompleted,
        /// <summary>Chưa có ad nào sẵn sàng.</summary>
        NotReady,
        /// <summary>Show lỗi từ network.</summary>
        Failed,
        /// <summary>Bị chặn: cooldown, đã mua remove ads, đang có ad khác hiển thị...</summary>
        Blocked
    }

    public sealed class MediationError
    {
        public readonly int Code;
        public readonly string Message;

        public MediationError(int code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString() => $"[{Code}] {Message}";
    }

    /// <summary>Thông tin doanh thu mỗi impression - đẩy sang TrackingManager (Adjust/Firebase/AppsFlyer...).</summary>
    public struct AdRevenueInfo
    {
        public AdNetworkType Network;
        public AdFormat Format;
        public string Placement;
        public string AdUnitId;
        /// <summary>Nguồn thực sự đã fill ad (vd AdMob waterfall/bidding trả về "AppLovin").</summary>
        public string AdSourceName;
        /// <summary>Doanh thu của impression này (đơn vị tiền tệ gốc, đã chia 1e6).</summary>
        public double Revenue;
        public string Currency;
        public string Precision;
    }
}
