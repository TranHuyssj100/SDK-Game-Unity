// Assets/SDK/SDKCore/GameManager/Runtime/Core/GameDirector.cs
namespace LSDK.GameManager
{
    /// <summary>
    /// Manager đang active, không phụ thuộc class cụ thể trên scene.
    /// </summary>
    public static class GameDirector
    {
        /// <summary>Instance đang chạy. Null trước khi manager Awake và sau khi nó bị hủy.</summary>
        public static IGameDirector Current { get; internal set; }
    }
}
