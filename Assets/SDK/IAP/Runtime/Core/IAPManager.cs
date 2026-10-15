using System;
using System.Collections;
using LSDK.GameData;
using UnityEngine;

namespace LSDK.IAP
{
    /// <summary>
    /// Entry point cua module IAP. Singleton + Coroutine, state luu qua LSDK GameData (IAPData).
    /// Flow: Purchase -> store xac nhan -> Grant -> SaveAll -> Confirm.
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

        public bool IsReady { get; private set; }

        /// <summary>Ban sau moi giao dich (thanh cong hoac that bai).</summary>
        public event Action<IAPResult> OnPurchaseCompleted;
        public event Action<bool> OnReadyChanged;

        /// <summary>(productId, conHieuLuc) khi subscription duoc kich hoat hoac het han.</summary>
        public event Action<string, bool> OnSubscriptionChanged;

        private IStoreAdapter _store;
        private IRewardGranter _granter;
        private IAPData _data;

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
        /// Goi 1 lan luc boot game. granter = null -> dung GameDataRewardGranter (coin/gem vao CurrencyData).
        /// Phai goi truoc khi game cho phep mua, vi store co the ban pending purchase ngay sau khi init.
        /// </summary>
        public void Init(IRewardGranter granter = null, IStoreAdapter store = null)
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

            _granter = granter ?? new GameDataRewardGranter();
            _store = store ?? CreateDefaultStore();
            _store.OnInitialized += HandleInitialized;
            _store.OnPurchaseSucceeded += HandleSuccess;
            _store.OnPurchaseFailed += HandleFail;
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
        private void HandleSuccess(string transactionId, string productId)
        {
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

            // Khong cap lan nua: transaction da xu ly, non-consumable da so huu,
            // hoac ky subscription cu da het han (iOS restore / cai lai game phat lai lich su).
            bool duplicate = _data.IsProcessed(transactionId)
                             || (def.type == IAPProductType.NonConsumable && _data.IsOwned(productId))
                             || (subExpire > 0 && subExpire <= DateTime.UtcNow.Ticks);
            if (duplicate)
            {
                _store.ConfirmPurchase(productId);
                if (IsPendingProduct(productId)) FinishPurchase(IAPResult.Ok(productId));
                return;
            }

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
            _data.MarkProcessed(transactionId);

            // GameData mac dinh debounce 1.5s -> ep ghi ngay (gom ca CurrencyData vua duoc cap qua) TRUOC khi confirm
            GameDataManager.Instance.SaveAll();

            _store.ConfirmPurchase(productId);
            FinishPurchase(IAPResult.Ok(productId));
        }

        private void HandleFail(string productId, IAPFailReason reason)
        {
            FinishPurchase(IAPResult.Fail(productId, reason));
        }

        private void HandleEntitlementUpdated(string productId, bool active, long expireUtcTicks)
        {
            var def = catalog.Get(productId);
            if (def == null) return;

            if (def.type == IAPProductType.Subscription)
            {
                ApplySubscription(productId, active, expireUtcTicks, true);
                return;
            }

            // Non-consumable da confirm tu truoc (cai lai game, may moi) -> khong co transaction moi nen cap tai day.
            // Khong thu hoi khi NotEntitled de tranh mat do khi store tra sai.
            if (def.type != IAPProductType.NonConsumable || !active || _data.IsOwned(productId)) return;

            try
            {
                _granter.Grant(def);
            }
            catch (Exception e)
            {
                Debug.LogError($"[IAP] Grant that bai khi khoi phuc {productId}: {e}");
                return;
            }
            _data.AddOwned(productId);
            GameDataManager.Instance.SaveAll();
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
