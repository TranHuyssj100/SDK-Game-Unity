using System;

namespace LSDK.IAP
{
    public enum ReceiptValidationStatus
    {
        /// <summary>Store xac nhan don hop le.</summary>
        Valid,

        /// <summary>Don gia, sai san pham, da hoan tien, token da dung... Khong cap qua, khong confirm.</summary>
        Invalid,

        /// <summary>Mat mang / server loi. Khong cap qua, khong confirm: store gui lai lan mo app sau.</summary>
        Error
    }

    public struct ReceiptValidationResult
    {
        public ReceiptValidationStatus status;

        /// <summary>SKU ma Google / Apple tra ve cho don nay (khong phai SKU client gui).</summary>
        public string storeProductId;

        public string transactionId;

        /// <summary>Han subscription theo store (UTC ticks). 0 = khong co / khong phai subscription.</summary>
        public long expireUtcTicks;

        public string message;

        public static ReceiptValidationResult Valid(string storeProductId, string transactionId = null, long expireUtcTicks = 0) =>
            new ReceiptValidationResult
            {
                status = ReceiptValidationStatus.Valid,
                storeProductId = storeProductId,
                transactionId = transactionId,
                expireUtcTicks = expireUtcTicks
            };

        public static ReceiptValidationResult Invalid(string message) =>
            new ReceiptValidationResult { status = ReceiptValidationStatus.Invalid, message = message };

        public static ReceiptValidationResult Error(string message) =>
            new ReceiptValidationResult { status = ReceiptValidationStatus.Error, message = message };
    }

    /// <summary>
    /// Xac thuc don voi store truoc khi cap qua. done phai duoc goi dung 1 lan, tren main thread.
    /// </summary>
    public interface IReceiptValidator
    {
        void Validate(IAPReceipt receipt, Action<ReceiptValidationResult> done);
    }
}
