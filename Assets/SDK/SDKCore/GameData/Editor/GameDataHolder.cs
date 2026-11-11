using UnityEngine;

namespace LSDK.GameData.EditorTools
{
    /// <summary>
    /// ScriptableObject tạm (chỉ nằm trong memory) để Unity vẽ module bằng inspector chuẩn
    /// thông qua [SerializeReference]. Không tạo asset.
    /// </summary>
    public sealed class GameDataHolder : ScriptableObject
    {
        [SerializeReference] public GameDataModule module;
    }
}
