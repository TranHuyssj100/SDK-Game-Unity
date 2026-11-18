// Assets/SDK/SDKCore/GameManager/Runtime/Core/ReadOnlyAttribute.cs
using UnityEngine;

namespace LSDK.GameManager
{
    /// <summary>Hiện field trên Inspector nhưng không cho sửa.</summary>
    public sealed class ReadOnlyAttribute : PropertyAttribute
    {
    }
}
