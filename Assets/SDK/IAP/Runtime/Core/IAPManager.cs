using System;
using System.Collections;
using System.Collections.Generic;
using LSDK.GameData;
using UnityEngine;

// NullRewardGranter duoc dung khi IAPManager.Init() khong truyen granter.
// De cap qua that su, truyen GameDataRewardGranter (sample) hoac tu implement IRewardGranter.
namespace LSDK.IAP
{
    internal sealed class NullRewardGranter : IRewardGranter
    {
        public void Grant(IAPProductDefinition product)
        {
            Debug.LogWarning(
                $"[IAP] NullRewardGranter: san pham '{product.id}' (rewardKey='{product.rewardKey}') " +
                "khong duoc cap qua. Goi IAPManager.Instance.Init(yourGranter) voi mot IRewardGranter hop le. " +
                "Tham khao sample 'GameDataIntegration' de dung san co voi LSDK GameData."
            );
        }
    }
}

namespace LSDK.IAP
{
    /// <summary>
    /// Entry point cua module IAP. Singleton + Coroutine, state luu qua LSDK GameData (IAPData).
    /// Flow: Purchase -> store xac nhan -> Verify server (Product ID phai khop store) -> Grant -> SaveAll -> Confirm.
    /// </summary>
    [DisallowMultipleComponent]
    public class IAPManager : MonoBehaviour
    {
        #region Singleton
        private static IAPManager _instance;
        public static IAPManager Instance => _instance;
        public static bool HasInstance => _instance != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _instance = null; }
        #endregion

        [SerializeField] private IAPProductCatalog catalog;
        [SerializeField] private bool useMockInEditor = true;

        [Header("Server verify")]
        [Tooltip("Endpoint POST IAPReceipt. Trong = khong verify (chi nen dung khi dev).")]
        [SerializeField] private string validationUrl;
        [SerializeField, Min(1)] private int validationTimeoutSeconds = 15;
        [Tooltip("Bo qua verify cho don cua MockStoreAdapter (Editor).")]
        [SerializeField] private bool skipValidationForMock = true;

        public bool IsReady { get; private set; }

        /// <summary>ID tai khoan game, gui kem receipt de server gan don voi nguoi choi.</summary>
        public string UserId { get; set; }

        /// <summary>Ban sau moi giao dich (thanh cong hoac that bai).</summary>
        public event Action<IAPResult> OnPurchaseCompleted;
        public event Action<bool> OnReadyChanged;

        /// <summary>(productId, conHieuLuc) khi subscription duoc kich hoat hoac het han.</summary>
        public event Action<string, bool> OnSubscriptionChanged;

        private IStoreAdapter _store;
        private IRewardGranter _granter;
        private IReceiptValidator _validator;
        private IAPData _data;

        // Transaction dang cho server tra loi: store co the ban lai cung don (FetchPurchases + mua).
        private readonly HashSet<string> _validating = new HashSet<string>();
        // Bi server tu choi trong session nay: khong goi lai server moi lan store ban lai.
        private readonly HashSet<string> _rejected = new HashSet<string>();

        private Action<IAPResult> _pendingCallback;
        private string _pendingProductId;
        private bool _purchasing;
        [SerializeField] private bool _initialized;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>
        /// Goi 1 lan luc boot game. granter = null -> NullRewardGranter (chi log, khong cap qua).
        /// validator = null -> ServerReceiptValidator(validationUrl) neu co URL, khong thi bo qua verify.
        /// Phai goi truoc khi game cho phep mua, vi store co the ban pending purchase ngay sau khi init.
        /// </summary>
        public void Init(IRewardGranter granter = null, IStoreAdapter store = null, IReceiptValidator validator = null)
        {
            if (_initialized) return;
            if (catalog == null)
            {
                Debug.LogError("[IAP] Chua gan IAPProductCatalog.");
                return;
            }
            _initialized = true;

            // Dang ky module IAPData vao GameData (neu GameData da load thi load luon tu storage)
            _data = GameDataManager.Instance.Register<IAPData>();

            _granter = granter ?? new NullRewardGranter();
            _validator = validator
                         ?? (string.IsNullOrEmpty(validationUrl)
                             ? null
                             : new ServerReceiptValidator(validationUrl, validationTimeoutSeconds));
            if (_validator == null && !Application.isEditor)
                Debug.LogWarning("[IAP] Chua cau hinh verify server: qua se duoc cap chi dua tren callback cua store.");

            _store = store ?? CreateDefaultStore();
            _store.OnInitialized += HandleInitialized;
            _store.OnPurchaseSucceeded += HandleSuccess;
            _store.OnPurchaseFailed += HandleFail;
            _store.OnPurchaseRestored += HandleRestored;
            _store.OnEntitlementUpdated += HandleEntitlementUpdated;
            _store.Initialize(catalog.products, this);

            StartCoroutine(InitRetryRoutine());
        }

        private IStoreAdapter CreateDefaultStore()
        {
#if UNITY_EDITOR
            if (useMockInEditor) return new MockStoreAdapter();
#endif
#if UNITY_PURCHASING
            return new UnityIAPAdapter();
#else
            Debug.LogWarning("[IAP] UNITY_PURCHASING chua bat, dung MockStoreAdapter.");
            return new MockStoreAdapter();
#endif
        }

        private void HandleInitialized(bool ok)
        {
            if (IsReady == ok) return;
            IsReady = ok;
            OnReadyChanged?.Invoke(ok);
            if (ok) _store.RefreshEntitlements();
        }

        // Retry khi mat mang luc khoi dong: 5s, 10s, 20s ... toi da 60s
        private IEnumerator InitRetryRoutine()
        {
            float wait = 5f;
            while (!IsReady)
            {
                yield return new WaitForSecondsRealtime(wait);
                if (IsReady) yield break;
                _store.Initialize(catalog.products, this);
                wait = Mathf.Min(wait * 2f, 60f);
            }
        }

        #region Public API
        public void Purchase(string productId, Action<IAPResult> callback = null)
        {
            var def = catalog != null ? catalog.Get(productId) : null;

            if (!IsReady)
            {
                Reject(productId, IAPFailReason.NotInitialized, callback);
                return;
            }
            if (def == null)
            {
                Reject(productId, IAPFailReason.ProductNotFound, callback);
                return;
            }
            if (_purchasing)
            {
                Reject(productId, IAPFailReason.PurchaseInProgress, callback); return;
            }
            if (def.type == IAPProductType.NonConsumable && IsOwned(productId))
            {
                Reject(productId, IAPFailReason.AlreadyOwned, callback);
                return;
            }
            if (def.type == IAPProductType.Subscription && IsSubscribed(productId))
            {
                Reject(productId, IAPFailReason.AlreadyOwned, callback);
                return;
            }

            _purchasing = true;
            _pendingProductId = productId;
            _pendingCallback = callback;
            _store.Purchase(productId);
        }

        public bool IsOwned(string productId) => _data != null && _data.IsOwned(productId);

        /// <summary>Subscription con hieu luc. Doc duoc ca khi store chua san sang, dua tren ban luu local.</summary>
        public bool IsSubscribed(string productId) => _data != null && _data.IsSubscribed(productId);

        public DateTime? GetSubscriptionExpireUtc(string productId) =>
            _data != null ? _data.GetSubscriptionExpireUtc(productId) : null;

        public string GetPrice(string productId) =>
            (_store != null && IsReady) ? _store.GetLocalizedPrice(productId) : string.Empty;

        public IAPProductDefinition GetProduct(string productId) => catalog != null ? catalog.Get(productId) : null;

        /// <summary>Hoi lai store (subscription con han, non-consumable da mua). Subscription doi thi ban OnSubscriptionChanged.</summary>
        public void RefreshEntitlements()
        {
            if (!IsReady) return;
            _store.RefreshEntitlements();
        }

        /// <summary>iOS bat buoc phai co nut Restore. Android tu restore luc init.</summary>
        public void Restore(Action<bool> done = null)
        {
            if (!IsReady) { done?.Invoke(false); return; }
            _store.RestorePurchases(ok =>
            {
                if (ok) _store.RefreshEntitlements();
                done?.Invoke(ok);
            });
        }
        #endregion

        #region Internal
        private void HandleSuccess(IAPReceipt receipt)
        {
            var productId = receipt?.productId;
            var def = catalog.Get(productId);
            if (def == null)
            {
                Debug.LogWarning($"[IAP] San pham la '{productId}' khong co trong catalog, confirm de khoi ket.");
                _store.ConfirmPurchase(productId);
                FinishPurchase(IAPResult.Fail(productId, IAPFailReason.ProductNotFound));
                return;
            }

            long subExpire = def.type == IAPProductType.Subscription
                ? _store.GetSubscriptionExpireUtcTicks(productId)
                : 0;
            if (IsAlreadyHandled(receipt, def, subExpire))
            {
                ConfirmDuplicate(productId);
                return;
            }

            var key = ReceiptKey(receipt);
            if (_validating.Contains(key)) return;
            if (_rejected.Contains(key))
            {
                // Khong confirm: neu that su la don that bi tu choi nham, Google tu hoan tien sau 3 ngay.
                FinishPurchase(IAPResult.Fail(productId, IAPFailReason.ValidationFailed));
                return;
            }

            Verify(receipt, def, key, result =>
            {
                if (result.status != ReceiptValidationStatus.Valid)
                {
                    FinishPurchase(IAPResult.Fail(productId, result.status == ReceiptValidationStatus.Invalid
                        ? IAPFailReason.ValidationFailed
                        : IAPFailReason.ValidationError));
                    return;
                }

                long expire = result.expireUtcTicks > 0 ? result.expireUtcTicks : subExpire;
                // Doi server co the lau: store da ban lai / don khac da cap trong luc cho.
                if (IsAlreadyHandled(receipt, def, expire))
                {
                    ConfirmDuplicate(productId);
                    return;
                }
                GrantAndConfirm(receipt, def, expire);
            });
        }

        private bool IsAlreadyHandled(IAPReceipt receipt, IAPProductDefinition def, long subExpire)
        {
            // Transaction da xu ly, non-consumable da so huu,
            // hoac ky subscription cu da het han (iOS restore / cai lai game phat lai lich su).
            return _data.IsProcessed(receipt.transactionId)
                   || (def.type == IAPProductType.NonConsumable && _data.IsOwned(def.id))
                   || (subExpire > 0 && subExpire <= DateTime.UtcNow.Ticks);
        }

        private void ConfirmDuplicate(string productId)
        {
            _store.ConfirmPurchase(productId);
            if (IsPendingProduct(productId)) FinishPurchase(IAPResult.Ok(productId));
        }

        private void GrantAndConfirm(IAPReceipt receipt, IAPProductDefinition def, long subExpire)
        {
            var productId = def.id;
            try
            {
                _granter.Grant(def);
            }
            catch (Exception e)
            {
                // Khong confirm -> store se gui lai lan sau de thu cap tiep
                Debug.LogError($"[IAP] Grant that bai cho {productId}: {e}");
                FinishPurchase(IAPResult.Fail(productId, IAPFailReason.Unknown));
                return;
            }

            if (def.type == IAPProductType.NonConsumable)
                _data.AddOwned(productId);
            if (def.type == IAPProductType.Subscription)
                ApplySubscription(productId, true, subExpire, false);
            _data.MarkProcessed(receipt.transactionId);

            // GameData mac dinh debounce 1.5s -> ep ghi ngay (gom ca CurrencyData vua duoc cap qua) TRUOC khi confirm
            GameDataManager.Instance.SaveAll();

            _store.ConfirmPurchase(productId);
            FinishPurchase(IAPResult.Ok(productId));
        }

        private void HandleRestored(IAPReceipt receipt)
        {
            var def = catalog.Get(receipt?.productId);
            if (def == null || def.type != IAPProductType.NonConsumable || _data.IsOwned(def.id)) return;

            var key = ReceiptKey(receipt);
            if (_validating.Contains(key) || _rejected.Contains(key)) return;

            Verify(receipt, def, key, result =>
            {
                if (result.status != ReceiptValidationStatus.Valid || _data.IsOwned(def.id)) return;

                try
                {
                    _granter.Grant(def);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[IAP] Grant that bai khi khoi phuc {def.id}: {e}");
                    return;
                }
                _data.AddOwned(def.id);
                GameDataManager.Instance.SaveAll();
            });
        }

        /// <summary>
        /// Goi validator roi kiem tra lai Product ID store tra ve phai trung SKU trong catalog.
        /// Khong co validator (hoac don Mock khi skipValidationForMock) -> coi nhu hop le.
        /// </summary>
        private void Verify(IAPReceipt receipt, IAPProductDefinition def, string key, Action<ReceiptValidationResult> done)
        {
            receipt.storeProductId = def.GetStoreId();
            receipt.productType = def.type.ToString();
            receipt.userId = UserId;

            bool skip = _validator == null || (skipValidationForMock && receipt.store == IAPStoreName.Mock);
            if (skip)
            {
                done(ReceiptValidationResult.Valid(receipt.storeProductId, receipt.transactionId));
                return;
            }

            _validating.Add(key);
            _validator.Validate(receipt, result =>
            {
                _validating.Remove(key);
                if (this == null || _data == null) return;

                result = CheckStoreMatch(receipt, result);
                if (result.status == ReceiptValidationStatus.Invalid)
                    _rejected.Add(key);
                if (result.status != ReceiptValidationStatus.Valid)
                    Debug.LogWarning($"[IAP] Verify {receipt.productId} ({receipt.store}) {result.status}: {result.message}");

                done(result);
            });
        }

        private static ReceiptValidationResult CheckStoreMatch(IAPReceipt receipt, ReceiptValidationResult result)
        {
            if (result.status != ReceiptValidationStatus.Valid) return result;

            if (string.IsNullOrEmpty(result.storeProductId))
                return ReceiptValidationResult.Invalid("Server khong tra productId cua store.");

            if (!string.Equals(result.storeProductId, receipt.storeProductId, StringComparison.Ordinal))
                return ReceiptValidationResult.Invalid(
                    $"Product ID store '{result.storeProductId}' khac catalog '{receipt.storeProductId}'.");

            if (!string.IsNullOrEmpty(result.transactionId) && !string.IsNullOrEmpty(receipt.transactionId)
                && !string.Equals(result.transactionId, receipt.transactionId, StringComparison.Ordinal))
                return ReceiptValidationResult.Invalid("Transaction server tra ve khac don dang xu ly.");

            return result;
        }

        private static string ReceiptKey(IAPReceipt receipt) =>
            string.IsNullOrEmpty(receipt.transactionId) ? receipt.productId : receipt.transactionId;

        private void HandleFail(string productId, IAPFailReason reason)
        {
            FinishPurchase(IAPResult.Fail(productId, reason));
        }

        private void HandleEntitlementUpdated(string productId, bool active, long expireUtcTicks)
        {
            var def = catalog.Get(productId);
            if (def == null) return;

            // Non-consumable khoi phuc di qua HandleRestored (co receipt de verify).
            if (def.type == IAPProductType.Subscription)
                ApplySubscription(productId, active, expireUtcTicks, true);
        }

        private void ApplySubscription(string productId, bool active, long expireUtcTicks, bool saveImmediately)
        {
            if (_data == null || string.IsNullOrEmpty(productId)) return;

            bool was = _data.IsSubscribed(productId);
            bool changed = _data.SetSubscription(productId, active, expireUtcTicks);
            if (saveImmediately && changed)
                GameDataManager.Instance.SaveAll();

            bool now = _data.IsSubscribed(productId);
            if (was != now)
                OnSubscriptionChanged?.Invoke(productId, now);
        }

        private void Reject(string productId, IAPFailReason reason, Action<IAPResult> callback)
        {
            var r = IAPResult.Fail(productId, reason);
            callback?.Invoke(r);
            OnPurchaseCompleted?.Invoke(r);
        }

        private bool IsPendingProduct(string productId) => _purchasing && productId == _pendingProductId;

        // Giao dich do store tu ban (pending luc khoi dong, Ask to Buy duoc duyet) khong duoc tra vao callback
        // cua lan mua dang cho san pham khac. productId rong = store khong ro san pham, coi la cua lan dang cho.
        private void FinishPurchase(IAPResult result)
        {
            if (_purchasing && (string.IsNullOrEmpty(result.productId) || result.productId == _pendingProductId))
            {
                _purchasing = false;
                _pendingProductId = null;
                var cb = _pendingCallback;
                _pendingCallback = null;
                cb?.Invoke(result);
            }
            OnPurchaseCompleted?.Invoke(result);
        }
        #endregion
    }
}
