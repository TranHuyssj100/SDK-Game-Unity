// Assets/SDK/SDKCore/ToastSystem/Runtime/ToastType.cs
namespace LSDK.ToastSystem
{
    /// <summary>Loại toast. Mỗi loại một màu chữ trên <see cref="ToastItem"/>.</summary>
    public enum ToastType
    {
        /// <summary>Chữ trắng.</summary>
        Normal = 0,

        /// <summary>Chữ xanh.</summary>
        Success = 1,

        /// <summary>Chữ vàng.</summary>
        Warning = 2,

        /// <summary>Chữ đỏ.</summary>
        Error = 3
    }
}
