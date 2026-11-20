// Assets/SDK/SDKCore/Base/Runtime/SingletonRuntime.cs
using UnityEngine;

namespace LSDK.Core.Base
{
    /// <summary>
    /// Trạng thái dùng chung cho mọi <see cref="SingletonBehaviour{T}"/>.
    /// Lớp generic không dùng được RuntimeInitializeOnLoadMethod, nên cờ thoát app nằm ở đây.
    /// </summary>
    internal static class SingletonRuntime
    {
        static bool _hooked;

        /// <summary>true khi app hoặc Play mode đang thoát. Không tạo singleton mới lúc này.</summary>
        internal static bool Quitting { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Quitting = false;
            if (_hooked) return;
            _hooked = true;
            Application.quitting += OnQuitting;
        }

        static void OnQuitting()
        {
            Quitting = true;
        }
    }
}
