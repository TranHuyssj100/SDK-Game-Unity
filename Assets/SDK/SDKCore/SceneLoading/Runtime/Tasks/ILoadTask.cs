using System;
using System.Collections;

namespace LSDK.Loading
{
    /// <summary>
    /// Một việc load chạy dưới màn che. Progress 0-1 là tiến độ của riêng task này.
    /// Error rỗng nghĩa là thành công.
    /// </summary>
    public interface ILoadTask
    {
        string Label { get; }
        float Weight { get; }
        string Error { get; }
        IEnumerator Run(IProgress<float> progress);
    }
}
