// Assets/SDK/CurencyManager/Runtime/CurrencyPanel.cs
using System.Collections;
using System.Collections.Generic;
using LSDK.UISystem;
using UnityEngine;

namespace LSDK.Currency
{
    /// <summary>
    /// Thanh tiền trên layer Overlay. Mỗi <see cref="CurrencyItem"/> con hiện một loại trong catalog.
    /// Panel chỉ vẽ. Việc cộng đơn vị hồi do <see cref="CurrencyManager"/> làm.
    /// Đồng hồ tick mỗi giây chỉ khi còn ô đang chờ hồi.
    /// </summary>
    public  class CurrencyPanel : UIPanel
    {
        readonly List<CurrencyItem> _items = new List<CurrencyItem>();
        Coroutine _tick;
        WaitForSecondsRealtime _wait;
        CurrencyManager _wallet;

        /// <summary>Mở thanh tiền qua UIManager. Panel phải có trong UIRegistry.</summary>
        public static void Open()
        {
            var ui = UIManager.Instance;
            if (ui == null)
            {
                CurrencyLog.Error("Chưa có UIManager.");
                return;
            }

            ui.Open<CurrencyPanel>();
        }

        void Awake()
        {
            CacheItems();
        }

        void OnEnable()
        {
            if (_items.Count == 0) CacheItems();

            _wallet = CurrencyManager.Instance;
            if (_wallet != null)
                _wallet.AmountChanged += OnAmountChanged;
            PaintAll();
        }

        void OnDisable()
        {
            if (_wallet != null)
                _wallet.AmountChanged -= OnAmountChanged;
            _wallet = null;
            StopTick();
        }

        void OnAmountChanged(CurrencyAmountChanged change)
        {
            PaintAll();
        }

        // Vẽ mọi ô. Ô không đổi thì không dựng chuỗi. Còn ô đang chờ hồi thì giữ tick.
        void PaintAll()
        {
            bool waiting = false;
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item != null && item.Paint()) waiting = true;
            }

            if (waiting && _tick == null && isActiveAndEnabled)
                _tick = StartCoroutine(Tick());
        }

        void CacheItems()
        {
            _items.Clear();
            GetComponentsInChildren(true, _items);
        }

        IEnumerator Tick()
        {
            if (_wait == null) _wait = new WaitForSecondsRealtime(1f);

            while (true)
            {
                _wait.Reset();
                yield return _wait;

                bool waiting = false;
                for (int i = 0; i < _items.Count; i++)
                {
                    var item = _items[i];
                    if (item != null && item.Paint()) waiting = true;
                }

                if (!waiting) break;
            }

            _tick = null;
        }

        void StopTick()
        {
            if (_tick == null) return;
            StopCoroutine(_tick);
            _tick = null;
            if (_wait != null) _wait.Reset();
        }
    }
}
