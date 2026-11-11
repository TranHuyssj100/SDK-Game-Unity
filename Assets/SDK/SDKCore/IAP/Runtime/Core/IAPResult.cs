namespace LSDK.IAP
{
    public enum IAPFailReason
    {
        None,
        NotInitialized,
        ProductNotFound,
        AlreadyOwned,
        PurchaseInProgress,
        UserCancelled,
        PaymentDeclined,
        ProductUnavailable,
        /// <summary>Thanh toan bi hoan (Ask to Buy, Google pending). Khi duyet xong se cap qua qua OnPurchaseCompleted.</summary>
        Deferred,
        /// <summary>Server verify tu choi: don gia, sai Product ID so voi store, da hoan tien...</summary>
        ValidationFailed,
        /// <summary>Khong verify duoc (mat mang, server loi). Don chua confirm, store gui lai lan mo app sau.</summary>
        ValidationError,
        Unknown
    }

    public struct IAPResult
    {
        public bool success;
        public string productId;
        public IAPFailReason reason;

        public static IAPResult Ok(string productId) =>
            new IAPResult { success = true, productId = productId, reason = IAPFailReason.None };

        public static IAPResult Fail(string productId, IAPFailReason reason) =>
            new IAPResult { success = false, productId = productId, reason = reason };
    }
}
