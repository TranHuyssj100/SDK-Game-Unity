using LSDK.Core.ObjectPools;
using LSDK.GameData;
using LSDK.IAP;
using LSDK.UISystem.EditorTools;
using UnityEditor;
using UnityEngine;

namespace LSDK.Core.EditorTools
{
    public static class SDKCoreMenu
    {
        [MenuItem("Tools/LSDK/Create SDKCore", false, 0)]
        private static void Create()
        {
            var existing = FindRoot();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log("[SDKCore] Scene đã có SDKCore.");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Create SDKCore");

            var root = new GameObject("SDKCore");
            Undo.RegisterCreatedObjectUndo(root, "Create SDKCore");
            Undo.AddComponent<SDKCoreRoot>(root);

            CreateChild<GameDataManager>(root.transform, "GameDataManager");
            CreateChild<IAPManager>(root.transform, "IAPManager");
            CreateChild<PoolServer>(root.transform, "PoolServer");
            UISystemMenu.CreateUIRoot(root.transform, true);

            Selection.activeGameObject = root;
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        private static GameObject CreateChild<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create SDKCore");
            Undo.SetTransformParent(go.transform, parent, "Create SDKCore");
            Undo.AddComponent<T>(go);
            return go;
        }

        private static SDKCoreRoot FindRoot()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindAnyObjectByType<SDKCoreRoot>(FindObjectsInactive.Include);
#else
            return Object.FindObjectOfType<SDKCoreRoot>(true);
#endif
        }
    }
}
