// Assets/SDK/FireBaseSDK/Editor/RemoteConfigHolder.cs
using UnityEngine;

namespace LSDK.Firebase.Editor
{
    /// <summary>
    /// ScriptableObject tạm trong memory để Unity vẽ RemoteConfigModule bằng inspector chuẩn.
    /// Không tạo asset.
    /// </summary>
    public sealed class RemoteConfigHolder : ScriptableObject
    {
        [SerializeReference] public RemoteConfigModule module;
    }
}
