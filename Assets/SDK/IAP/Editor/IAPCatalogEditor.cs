using System.Collections.Generic;
using UnityEditor;

namespace LSDK.IAP.EditorTools
{
    [CustomEditor(typeof(IAPProductCatalog))]
    public class IAPCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var catalog = (IAPProductCatalog)target;
            var seen = new HashSet<string>();

            foreach (var p in catalog.products)
            {
                if (p == null) continue;

                if (string.IsNullOrEmpty(p.id))
                {
                    EditorGUILayout.HelpBox("Co san pham chua dien ID.", MessageType.Error);
                    continue;
                }
                if (!seen.Add(p.id))
                    EditorGUILayout.HelpBox($"ID bi trung: {p.id}", MessageType.Error);

                if (string.IsNullOrEmpty(p.googleId) && string.IsNullOrEmpty(p.appleId))
                    EditorGUILayout.HelpBox($"'{p.id}': chua co googleId/appleId, se dung ID noi bo lam store ID.", MessageType.Warning);
            }
        }
    }
}
