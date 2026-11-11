#if UNITY_PURCHASING
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;

namespace LSDK.IAP
{
    /// <summary>
    /// Adapter cho Unity IAP v5 (StoreController).
    /// Viet theo tai lieu Unity IAP 5.x. Neu ban package khac doi ten API thi chi can sua file nay.
    /// </summary>
    public class UnityIAPAdapter : IStoreAdapter
    {
        public event Action<bool> OnInitialized;
        public event Action<IAPReceipt> OnPurchaseSucceeded;
        public event Action<string, IAPFailReason> OnPurchaseFailed;
        public event Action<IAPReceipt> OnPurchaseRestored;
        public event Action<string, bool, long> OnEntitlementUpdated;

        StoreController _controller;
        MonoBehaviour _runner;
        List<ProductDefinition> _defs;
        bool _connecting;

        readonly Dictionary<string, PendingOrder> _pending = new Dictionary<string, PendingOrder>();
        readonly Dictionary<string, Product> _products = new Dictionary<string, Product>();

        public void Initialize(IEnumerable<IAPProductDefinition> products, MonoBehaviour runner)
        {
            _runner = runner;
            _defs = products
                .Where(p => !string.IsNullOrEmpty(p.id))
                .Select(p => new ProductDefinition(p.id, p.GetStoreId(), Map(p.type)))
                .ToList();

            if (_controller == null)
            {
                _controller = UnityIAPServices.StoreController();
                _controller.OnPurchasePending += HandlePending;
                _controller.OnPurchaseFailed += HandleFailed;
                _controller.OnPurchaseDeferred += HandleDeferred;
                _controller.OnPurchasesFetched += HandlePurchasesFetched;
                _controller.OnProductsFetched += HandleProductsFetched;
                _controller.OnProductsFetchFailed += _ => OnInitialized?.Invoke(false);
                _controller.OnCheckEntitlement += HandleEntitlement;
            }

            if (!_connecting)
                _runner.StartCoroutine(ConnectRoutine());
        }

        IEnumerator ConnectRoutine()
        {
            _connecting = true;

            // IAP v5 can UGS init truoc
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                var init = UnityServices.InitializeAsync();
                yield return new WaitUntil(() => init.IsCompleted);
                if (init.IsFaulted)
                {
                    Debug.LogWarning($"[IAP] UnityServices init failed: {init.Exception?.GetBaseException().Message}");
                    _connecting = false;
                    OnInitialized?.Invoke(false);
                    yield break;
                }
            }
            else if (UnityServices.State == ServicesInitializationState.Initializing)
            {
                yield return new WaitUntil(() => UnityServices.State != ServicesInitializationState.Initializing);
            }

            var connect = _controller.Connect();
            yield return new WaitUntil(() => connect.IsCompleted);
            if (connect.IsFaulted)
            {
                Debug.LogWarning($"[IAP] Connect failed: {connect.Exception?.GetBaseException().Message}");
                _connecting = false;
                OnInitialized?.Invoke(false);
                yield break;
            }

            _controller.FetchProducts(_defs);
            _connecting = false;
        }

        void HandleProductsFetched(List<Product> products)
        {
            foreach (var p in products)
                _products[p.definition.id] = p;

            // Lay lai cac don chua xu ly (pending) -> se ban OnPurchasePending
            _controller.FetchPurchases();
            OnInitialized?.Invoke(true);
        }

        void HandlePending(PendingOrder order)
        {
            var productId = GetProductId(order.CartOrdered);
            if (string.IsNullOrEmpty(productId)) return;

            _pending[productId] = order;
            OnPurchaseSucceeded?.Invoke(BuildReceipt(order, productId, false));
        }

        static IAPReceipt BuildReceipt(Order order, string productId, bool restored)
        {
            var info = order.Info;
            var receipt = new IAPReceipt
            {
                productId = productId,
                storeProductId = GetProduct(order.CartOrdered)?.definition?.storeSpecificId,
                transactionId = info?.TransactionID,
                unityReceipt = info?.Receipt,
                packageName = Application.identifier,
                restored = restored
            };

            if (info?.Google != null)
            {
                receipt.store = IAPStoreName.GooglePlay;
                receipt.purchaseToken = info.Google.PurchaseToken;
                receipt.orderId = info.Google.OrderId;
            }
            else if (info?.Apple != null)
            {
                receipt.store = IAPStoreName.AppleAppStore;
                receipt.signedTransaction = info.Apple.jwsRepresentation;
                receipt.originalTransactionId = info.Apple.OriginalTransactionID;
            }
            else
            {
                receipt.store = Application.platform == RuntimePlatform.Android ? IAPStoreName.GooglePlay
                    : Application.platform == RuntimePlatform.IPhonePlayer ? IAPStoreName.AppleAppStore
                    : IAPStoreName.Unknown;
                if (receipt.store == IAPStoreName.GooglePlay)
                    receipt.purchaseToken = info?.TransactionID;
            }
            return receipt;
        }

        void HandleFailed(FailedOrder order)
        {
            var productId = GetProductId(order.CartOrdered);
            OnPurchaseFailed?.Invoke(productId, MapFail(order.FailureReason));
        }

        // Ask to Buy / Google pending: khong co Pending hay Failed. Khi duoc duyet store ban OnPurchasePending.
        void HandleDeferred(DeferredOrder order)
        {
            OnPurchaseFailed?.Invoke(GetProductId(order.CartOrdered), IAPFailReason.Deferred);
        }

        // Don da confirm tu truoc chi ve qua day, khong qua OnPurchasePending.
        void HandlePurchasesFetched(Orders orders)
        {
            var confirmed = orders?.ConfirmedOrders;
            if (confirmed == null) return;

            long now = DateTime.UtcNow.Ticks;
            foreach (var order in confirmed)
            {
                var def = GetProduct(order.CartOrdered)?.definition;
                if (def == null) continue;

                if (def.type == ProductType.NonConsumable)
                {
                    OnPurchaseRestored?.Invoke(BuildReceipt(order, def.id, true));
                }
                else if (def.type == ProductType.Subscription)
                {
                    long expire = ReadExpireTicks(order);
                    if (expire > 0 && expire <= now) continue;
                    OnEntitlementUpdated?.Invoke(def.id, true, expire);
                }
            }
        }

        static Product GetProduct(ICart cart)
        {
            var items = cart?.Items();
            return (items != null && items.Count > 0) ? items[0].Product : null;
        }

        static string GetProductId(ICart cart) => GetProduct(cart)?.definition?.id;

        public void Purchase(string productId)
        {
            _controller.PurchaseProduct(productId);
        }

        public void ConfirmPurchase(string productId)
        {
            if (_pending.TryGetValue(productId, out var order))
            {
                _controller.ConfirmPurchase(order);
                _pending.Remove(productId);
            }
        }

        public void RestorePurchases(Action<bool> done)
        {
            _controller.RestoreTransactions((ok, _) => done?.Invoke(ok));
        }

        public string GetLocalizedPrice(string productId)
        {
            return _products.TryGetValue(productId, out var p) ? p.metadata.localizedPriceString : string.Empty;
        }

        public void RefreshEntitlements()
        {
            if (_controller == null) return;
            foreach (var pair in _products)
            {
                if (pair.Value?.definition == null) continue;
                if (pair.Value.definition.type != ProductType.Subscription) continue;
                _controller.CheckEntitlement(pair.Value);
            }
        }

        public long GetSubscriptionExpireUtcTicks(string productId)
        {
            if (!string.IsNullOrEmpty(productId) && _pending.TryGetValue(productId, out var order))
            {
                long fromOrder = ReadExpireTicks(order);
                if (fromOrder > 0) return fromOrder;
            }

            return _products.TryGetValue(productId, out var product) ? TryExpireFromProduct(product) : 0;
        }

        void HandleEntitlement(Entitlement entitlement)
        {
            var product = entitlement?.Product;
            if (product?.definition == null || product.definition.type != ProductType.Subscription) return;
            if (entitlement.Status == EntitlementStatus.Unknown) return;

            bool active = entitlement.Status == EntitlementStatus.FullyEntitled
                          || entitlement.Status == EntitlementStatus.EntitledButNotFinished;
            long expire = active ? ReadExpireTicks(entitlement.Order) : 0;
            if (active && expire <= 0)
                expire = TryExpireFromProduct(product);
            // Store bao con han nhung receipt cu (grace period / chua cap nhat) -> tin store, bo ngay
            if (active && expire > 0 && expire <= DateTime.UtcNow.Ticks)
                expire = 0;

            OnEntitlementUpdated?.Invoke(product.definition.id, active, expire);
        }

        static long ReadExpireTicks(Order order)
        {
            var infos = order?.Info?.PurchasedProductInfo;
            if (infos == null) return 0;

            long best = 0;
            for (int i = 0; i < infos.Count; i++)
            {
                var sub = infos[i]?.subscriptionInfo;
                if (sub == null) continue;
                long ticks = ToUtcTicks(sub.GetExpireDate());
                if (ticks > best) best = ticks;
            }
            return best;
        }

        static long TryExpireFromProduct(Product product)
        {
            if (product == null) return 0;
            try
            {
                var info = new SubscriptionInfoHelper(product, null).GetSubscriptionInfo();
                return ToUtcTicks(info.GetExpireDate());
            }
            catch (Exception)
            {
                return 0;
            }
        }

        static long ToUtcTicks(DateTime value)
        {
            if (value.Ticks <= 0) return 0;
            if (value.Kind == DateTimeKind.Local) value = value.ToUniversalTime();
            else if (value.Kind == DateTimeKind.Unspecified)
                value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return value.Ticks;
        }

        static ProductType Map(IAPProductType t)
        {
            switch (t)
            {
                case IAPProductType.NonConsumable: return ProductType.NonConsumable;
                case IAPProductType.Subscription: return ProductType.Subscription;
                default: return ProductType.Consumable;
            }
        }

        static IAPFailReason MapFail(PurchaseFailureReason r)
        {
            switch (r)
            {
                case PurchaseFailureReason.UserCancelled: return IAPFailReason.UserCancelled;
                case PurchaseFailureReason.PaymentDeclined: return IAPFailReason.PaymentDeclined;
                case PurchaseFailureReason.ProductUnavailable: return IAPFailReason.ProductUnavailable;
                default: return IAPFailReason.Unknown;
            }
        }
    }
}
#endif
