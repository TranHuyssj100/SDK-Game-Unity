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
