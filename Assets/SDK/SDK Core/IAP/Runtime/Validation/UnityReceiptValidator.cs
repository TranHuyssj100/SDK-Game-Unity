using System;
using System.Reflection;
using UnityEngine;
#if UNITY_PURCHASING
using UnityEngine.Purchasing.Security;
#endif

namespace LSDK.IAP
{
    /// <summary>
    /// Xac thuc receipt tren thiet bi (client-side) bang CrossPlatformValidator cua Unity IAP.
    /// Khong can server backend. De trien khai nhung bao mat thap hon Server mode.
    ///
    /// TRUOC KHI DUNG:
    /// 1. Mo Window > Unity IAP > IAP Receipt Validation Obfuscator.
    /// 2. Dan Google Play License Key va bam "Obfuscate Google Play License Key".
    /// 3. Bam "Obfuscate Apple Receipt Signing" cho iOS.
    /// => Tao ra GooglePlayTangle.cs va AppleTangle.cs trong Assets/Scripts/UnityPurchasing/generated/
    ///
    /// GHI CHU:
    /// - Tren Editor / platform khong ho tro: bo qua validate (tra Valid).
    /// - Apple StoreKit 2 (iOS 15+): CrossPlatformValidator tra ve mang rong -> coi la Valid (Unity da verify phia store).
    /// - Khong chong duoc replay attack neu thiet bi bi root / jailbreak.
    /// </summary>
    public class UnityReceiptValidator : IReceiptValidator
    {
        private readonly byte[] _googleKey;
        private readonly byte[] _appleKey;

        /// <summary>
        /// De null de validator tu load key tu Tangle class da duoc generate boi Unity IAP Obfuscator.
        /// Truyen byte[] tuong minh de inject thu cong (unit test, override).
        /// </summary>
        public UnityReceiptValidator(byte[] googleKey = null, byte[] appleKey = null)
        {
            _googleKey = googleKey;
            _appleKey = appleKey;
        }

        public void Validate(IAPReceipt receipt, Action<ReceiptValidationResult> done)
        {
#if UNITY_PURCHASING
            var googleKey = _googleKey ?? LoadTangleData("GooglePlayTangle");
            var appleKey = _appleKey ?? LoadTangleData("AppleTangle");

            if (googleKey == null && appleKey == null)
            {
                done(ReceiptValidationResult.Error(
                    "[IAP] UnityReceiptValidator: Khong tim thay Tangle key. " +
                    "Hay chay Window > Unity IAP > IAP Receipt Validation Obfuscator de tao " +
                    "GooglePlayTangle.cs va AppleTangle.cs."));
                return;
            }

            try
            {
                // CrossPlatformValidator bo qua store neu key tuong ung la null.
                var validator = new CrossPlatformValidator(googleKey, appleKey, Application.identifier);
                var receipts = validator.Validate(receipt.unityReceipt);

                // Apple StoreKit 2 (iOS 15+) tra ve mang rong vi Unity da xu ly phia Apple:
                // coi nhu hop le va dung storeProductId tu client.
                if (receipts == null || receipts.Length == 0)
                {
                    done(ReceiptValidationResult.Valid(receipt.storeProductId, receipt.transactionId));
                    return;
                }

                var first = receipts[0];
                var productId = string.IsNullOrEmpty(first.productID)
                    ? receipt.storeProductId
                    : first.productID;
                done(ReceiptValidationResult.Valid(productId, first.transactionID));
            }
            catch (IAPSecurityException ex)
            {
                done(ReceiptValidationResult.Invalid("CrossPlatformValidator: " + ex.Message));
            }
            catch (NotImplementedException)
            {
                // SecurityStub dang chay (Editor / platform khong ho tro): bo qua verify.
                done(ReceiptValidationResult.Valid(receipt.storeProductId, receipt.transactionId));
            }
            catch (Exception ex)
            {
                done(ReceiptValidationResult.Error("UnityReceiptValidator ngoai le: " + ex.Message));
            }
#else
            // UNITY_PURCHASING chua bat -> khong lam duoc gi, bao loi de dev biet.
            done(ReceiptValidationResult.Error(
                "UnityReceiptValidator yeu cau UNITY_PURCHASING. Kiem tra Package Manager > In App Purchasing."));
#endif
        }

        /// <summary>
        /// Dung Reflection de doc key tu Tangle class da duoc generate (co the chua ton tai).
        /// Tra null neu class chua ton tai hoac IsPopulated = false (chua obfuscate).
        /// </summary>
        private static byte[] LoadTangleData(string className)
        {
            // Thu tim trong assembly cua package truoc, sau do Assembly-CSharp (project).
            var type =
                Type.GetType($"UnityEngine.Purchasing.Security.{className}, Unity.Purchasing.Security")
             ?? Type.GetType($"UnityEngine.Purchasing.Security.{className}, Assembly-CSharp");

            if (type == null) return null;

            // Neu IsPopulated = false -> class ton tai nhung chua co key that (placeholder rong).
            var populatedField = type.GetField("IsPopulated",
                BindingFlags.Public | BindingFlags.Static);
            if (populatedField != null && !(bool)populatedField.GetValue(null))
            {
                Debug.LogWarning(
                    $"[IAP] {className}.IsPopulated = false. " +
                    "Hay chay IAP Receipt Validation Obfuscator de nhap key that.");
                return null;
            }

            var dataMethod = type.GetMethod("Data",
                BindingFlags.Public | BindingFlags.Static);
            return dataMethod?.Invoke(null, null) as byte[];
        }
    }
}
