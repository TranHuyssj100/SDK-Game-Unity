// Assets/SDK/CurencyManager/Runtime/CurrencyFlyIcon.cs
using DG.Tweening;
using LSDK.Core.ObjectPools;
using UnityEngine;
using UnityEngine.UI;

namespace LSDK.Currency
{
    /// <summary>
    /// Icon bay từ điểm nhận tới ô tiền. Prefab này đăng ký trong PoolDatabase.
    /// Sprite lấy từ <see cref="CurrencyDefinition.Icon"/> lúc spawn.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public sealed class CurrencyFlyIcon : MonoBehaviour, IPooledObject
    {
        [SerializeField] private Image _image;

        RectTransform _rect;
        Tween _tween;
        CurrencyManager _host;
        CurrencyItem _item;
        RectTransform _layer;
        Camera _layerCamera;
        RectTransform _target;
        Camera _targetCamera;
        Vector2 _lastEnd;
        float _arc;

        void Awake()
        {
            _rect = (RectTransform)transform;
            if (_image == null) _image = GetComponent<Image>();
        }

        /// <summary>Pool vừa đưa object ra.</summary>
        public void OnObjectGet()
        {
            if (_rect == null) _rect = (RectTransform)transform;
            if (_image == null) _image = GetComponent<Image>();
            if (_rect != null) _rect.localScale = Vector3.one;
        }

        /// <summary>Pool sắp cất object. Dừng tween đang chạy.</summary>
        public void OnObjectReturn()
        {
            _host = null;
            _item = null;
            Cancel();
        }

        /// <summary>Đổi một điểm sang local của <paramref name="layer"/>.</summary>
        internal static bool TryToLocal(RectTransform layer, Camera layerCamera, Transform point, Camera pointCamera, out Vector2 local)
        {
            local = default;
            if (layer == null) return false;

            Vector2 screen = point == null
                ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
                : (Vector2)RectTransformUtility.WorldToScreenPoint(pointCamera, point.position);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, layerCamera, out local);
        }

        /// <summary>Camera dùng để đổi tọa độ. Overlay trả null. Object thế giới dùng Camera.main.</summary>
        internal static Camera CameraOf(Transform point)
        {
            if (point == null) return null;
            var canvas = point.GetComponentInParent<Canvas>();
            if (canvas == null) return Camera.main;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        /// <summary>
        /// Bay từ <paramref name="from"/> tới <paramref name="target"/>.
        /// <paramref name="host"/> được báo trước khi object trở lại pool.
        /// </summary>
        internal void Launch(
            Sprite sprite,
            RectTransform layer,
            Camera layerCamera,
            Vector2 from,
            RectTransform target,
            Camera targetCamera,
            CurrencyReceiveConfig config,
            int index,
            CurrencyManager host,
            CurrencyItem item)
        {
            Cancel();
            _host = host;
            _item = item;
            _layer = layer;
            _layerCamera = layerCamera;
            _target = target;
            _targetCamera = targetCamera;

            if (_rect == null) _rect = (RectTransform)transform;
            if (_image == null) _image = GetComponent<Image>();
            if (_image != null)
            {
                _image.raycastTarget = false;
                if (sprite != null) _image.sprite = sprite;
                _image.enabled = _image.sprite != null;
            }

            _rect.SetParent(layer, false);
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.localRotation = Quaternion.identity;
            _rect.localScale = new Vector3(0.4f, 0.4f, 1f);
            float size = config != null ? config.IconSize : 72f;
            _rect.sizeDelta = new Vector2(size, size);
            _rect.anchoredPosition = from;
            _rect.SetAsLastSibling();
            gameObject.layer = layer.gameObject.layer;

            _lastEnd = from;
            if (target != null && TryToLocal(layer, layerCamera, target, targetCamera, out var initial))
                _lastEnd = initial;

            float spread = config != null ? config.Spread : 0f;
            Vector2 burst = spread <= 0f ? from : from + UnityEngine.Random.insideUnitCircle * spread;
            float arc = config != null ? config.Arc : 0f;
            _arc = UnityEngine.Random.value < 0.5f ? -arc : arc;

            float burstDuration = config != null ? config.BurstDuration : 0.22f;
            float flyDuration = config != null ? config.FlyDuration : 0.55f;
            float stagger = config != null ? config.Stagger : 0f;
            Ease flyEase = config != null ? config.FlyEase : Ease.InCubic;
            bool unscaled = config == null || config.UseUnscaledTime;

            var seq = DOTween.Sequence().SetTarget(this);
            if (index > 0 && stagger > 0f)
                seq.AppendInterval(index * stagger);

            if (burstDuration > 0f)
                seq.Append(Move(from, burst, burstDuration, Ease.OutQuad, false, unscaled));
            else
            {
                _rect.anchoredPosition = burst;
                _rect.localScale = Vector3.one;
            }

            seq.Append(Move(burst, Vector2.zero, flyDuration, flyEase, true, unscaled));
            seq.SetUpdate(unscaled);
            seq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            seq.OnComplete(Finish);
            _tween = seq;
        }

        Tween Move(Vector2 from, Vector2 to, float duration, Ease ease, bool curve, bool unscaled)
        {
            float progress = 0f;
            return DOTween.To(() => progress, value =>
            {
                progress = value;
                Vector2 end = curve ? _lastEnd : to;
                if (curve && _target != null
                    && TryToLocal(_layer, _layerCamera, _target, _targetCamera, out var live))
                {
                    end = live;
                    _lastEnd = live;
                }

                _rect.anchoredPosition = curve
                    ? Bezier(from, Control(from, end, _arc), end, value)
                    : Vector2.LerpUnclamped(from, end, value);

                if (!curve)
                    _rect.localScale = Vector3.LerpUnclamped(new Vector3(0.4f, 0.4f, 1f), Vector3.one, value);
            }, 1f, duration < 0.01f ? 0.01f : duration).SetEase(ease).SetUpdate(unscaled).SetTarget(this);
        }

        void Finish()
        {
            _tween = null;
            var host = _host;
            var item = _item;
            _host = null;
            _item = null;
            if (_rect != null) _rect.localScale = Vector3.one;
            if (host != null) host.NotifyIconArrived(this, item);

            var pool = PoolServer.HasInstance ? PoolServer.Instance : null;
            if (pool != null) pool.ReturnObject(gameObject);
        }

        void Cancel()
        {
            if (_tween != null && _tween.IsActive())
            {
                var tween = _tween;
                _tween = null;
                tween.Kill(false);
            }
            _tween = null;
            if (_rect != null) _rect.localScale = Vector3.one;
        }

        static Vector2 Bezier(Vector2 a, Vector2 control, Vector2 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * control + t * t * c;
        }

        static Vector2 Control(Vector2 a, Vector2 c, float arc)
        {
            Vector2 mid = (a + c) * 0.5f;
            if (Mathf.Abs(arc) < 0.001f) return mid;

            Vector2 dir = c - a;
            float len = dir.magnitude;
            Vector2 perp = len < 0.001f ? Vector2.up : new Vector2(-dir.y / len, dir.x / len);
            return mid + perp * arc;
        }
    }
}
