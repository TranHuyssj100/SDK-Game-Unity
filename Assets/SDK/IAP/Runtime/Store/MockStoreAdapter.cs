using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LSDK.IAP
{
    /// <summary>Store gia de test flow trong Editor, khong can build len may.</summary>
    public class MockStoreAdapter : IStoreAdapter
    {
        public enum Behaviour { Success, UserCancel, Fail }

        public static Behaviour NextResult = Behaviour.Success;
        public static float Delay = 0.5f;

        /// <summary>Han gia khi mua subscription thanh cong trong Editor.</summary>
        public static TimeSpan SubscriptionDuration = TimeSpan.FromDays(30);

        public event Action<bool> OnInitialized;
        public event Action<string, string> OnPurchaseSucceeded;
        public event Action<string, IAPFailReason> OnPurchaseFailed;
        public event Action<string, bool, long> OnEntitlementUpdated;

        static readonly Dictionary<string, long> ExpireUtcTicks = new Dictionary<string, long>();
        static MockStoreAdapter _active;

        MonoBehaviour _runner;
        readonly HashSet<string> _ids = new HashSet<string>();
        readonly Dictionary<string, IAPProductType> _types = new Dictionary<string, IAPProductType>();

        /// <summary>Dat han UTC de test het han / gia han. Ban OnEntitlementUpdated neu adapter dang chay.</summary>
        public static void SetSubscriptionExpire(string productId, DateTime expireUtc)
        {
            if (string.IsNullOrEmpty(productId)) return;
            long ticks = expireUtc.Kind == DateTimeKind.Utc
                ? expireUtc.Ticks
                : expireUtc.ToUniversalTime().Ticks;
            ExpireUtcTicks[productId] = ticks;
            bool active = ticks > DateTime.UtcNow.Ticks;
            _active?.OnEntitlementUpdated?.Invoke(productId, active, ticks);
        }

        public void Initialize(IEnumerable<IAPProductDefinition> products, MonoBehaviour runner)
        {
            _active = this;
            _runner = runner;
            _ids.Clear();
            _types.Clear();
            foreach (var p in products)
            {
                if (p == null || string.IsNullOrEmpty(p.id)) continue;
                _ids.Add(p.id);
                _types[p.id] = p.type;
            }
            _runner.StartCoroutine(InitRoutine());
        }

        IEnumerator InitRoutine()
        {
            yield return new WaitForSecondsRealtime(Delay);
            OnInitialized?.Invoke(true);
        }

        public void Purchase(string productId)
        {
            _runner.StartCoroutine(PurchaseRoutine(productId));
        }

        IEnumerator PurchaseRoutine(string productId)
        {
            yield return new WaitForSecondsRealtime(Delay);

            if (!_ids.Contains(productId))
            {
                OnPurchaseFailed?.Invoke(productId, IAPFailReason.ProductUnavailable);
                yield break;
            }

            switch (NextResult)
            {
                case Behaviour.Success:
                    if (_types.TryGetValue(productId, out var type) && type == IAPProductType.Subscription)
                        ExpireUtcTicks[productId] = DateTime.UtcNow.Add(SubscriptionDuration).Ticks;
                    OnPurchaseSucceeded?.Invoke(Guid.NewGuid().ToString(), productId);
                    break;
                case Behaviour.UserCancel:
                    OnPurchaseFailed?.Invoke(productId, IAPFailReason.UserCancelled);
                    break;
                default:
                    OnPurchaseFailed?.Invoke(productId, IAPFailReason.PaymentDeclined);
                    break;
            }
        }

        public void ConfirmPurchase(string productId)
        {
            Debug.Log($"[IAP/Mock] Confirm {productId}");
        }

        public void RestorePurchases(Action<bool> done)
        {
            _runner.StartCoroutine(RestoreRoutine(done));
        }

        IEnumerator RestoreRoutine(Action<bool> done)
        {
            yield return new WaitForSecondsRealtime(Delay);
            done?.Invoke(true);
        }

        public string GetLocalizedPrice(string productId) => "$0.99";

        public void RefreshEntitlements()
        {
            foreach (var pair in _types)
            {
                if (pair.Value != IAPProductType.Subscription) continue;
                if (!ExpireUtcTicks.TryGetValue(pair.Key, out var ticks)) continue;
                OnEntitlementUpdated?.Invoke(pair.Key, ticks > DateTime.UtcNow.Ticks, ticks);
            }
        }

        public long GetSubscriptionExpireUtcTicks(string productId)
        {
            return ExpireUtcTicks.TryGetValue(productId, out var ticks) ? ticks : 0;
        }
    }
}
