using UnityEngine;
using UnityEngine.UI;
using LSDK.UISystem;
using TMPro;

namespace LSDK.Loading
{
    /// <summary>
    /// Màn che fullscreen trên layer Blocker. Chỉ vẽ tiến độ; LoadingManager điều phối load.
    /// </summary>
    public class LoadingPanel : UIPanel
    {
        [SerializeField] Slider progressBar;
        [SerializeField] TextMeshProUGUI progressLabel;
        [SerializeField] TextMeshProUGUI statusLabel;
        [SerializeField] TextMeshProUGUI tipLabel;
        [SerializeField] Button dismissButton;
        [SerializeField] Color errorColor = new Color(1f, 0.45f, 0.4f, 1f);

        LoadingSession _session;
        Color _statusColor = Color.white;

        void Reset()
        {
            Layer = UILayer.Blocker;
            OpaqueBackground = true;
            CloseOnBack = false;
        }

        protected override void OnOpen(object data)
        {
            _session = data as LoadingSession;
            if (statusLabel != null) _statusColor = statusLabel.color;
            if (_session != null) _session.Changed += Refresh;
            Refresh();
        }

        protected override void OnClosed()
        {
            if (_session != null) _session.Changed -= Refresh;
            _session = null;
        }

        void OnDestroy()
        {
            if (_session != null) _session.Changed -= Refresh;
        }

        // Gắn vào nút thử lại / đóng khi load lỗi.
        public void Dismiss()
        {
            if (LoadingManager.Instance != null) LoadingManager.Instance.Dismiss();
        }

        void Refresh()
        {
            float progress = _session != null ? _session.Progress : 0f;
            bool failed = _session != null && _session.HasError;

            if (progressBar != null) progressBar.value = progress;
            if (progressLabel != null) progressLabel.text = Mathf.RoundToInt(progress * 100f) + "%";
            if (statusLabel != null)
            {
                statusLabel.text = failed ? _session.Error : (_session != null ? _session.Status : "");
                statusLabel.color = failed ? errorColor : _statusColor;
            }
            if (tipLabel != null) tipLabel.text = _session != null && !failed ? _session.Tip : "";
            if (dismissButton != null) dismissButton.gameObject.SetActive(failed);
        }
    }
}
