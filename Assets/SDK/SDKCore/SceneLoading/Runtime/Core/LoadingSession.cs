using System;

namespace LSDK.Loading
{
    /// <summary>
    /// Dữ liệu UIManager truyền vào LoadingPanel.OnOpen.
    /// </summary>
    public sealed class LoadingSession
    {
        public float Progress { get; private set; }
        public string Status { get; private set; }
        public string Tip { get; private set; }
        public string Error { get; private set; }
        public bool HasError => !string.IsNullOrEmpty(Error);

        public event Action Changed;

        public LoadingSession(string tip, string status = "Loading...")
        {
            Tip = tip ?? "";
            Status = string.IsNullOrEmpty(status) ? "Loading..." : status;
        }

        internal void SetProgress(float value)
        {
            value = value < 0f ? 0f : (value > 1f ? 1f : value);
            if (Progress == value) return;
            Progress = value;
            Changed?.Invoke();
        }

        internal void SetStatus(string value)
        {
            value = string.IsNullOrEmpty(value) ? "Loading..." : value;
            if (Status == value) return;
            Status = value;
            Changed?.Invoke();
        }

        internal void SetError(string message)
        {
            Error = message ?? "";
            Changed?.Invoke();
        }
    }
}
