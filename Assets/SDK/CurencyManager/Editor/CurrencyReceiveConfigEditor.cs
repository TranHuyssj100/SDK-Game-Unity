// Assets/SDK/CurencyManager/Editor/CurrencyReceiveConfigEditor.cs
using System.Collections.Generic;
using LSDK.Core.ObjectPools;
using UnityEditor;
using UnityEngine;

namespace LSDK.Currency.Editor
{
    /// <summary>Inspector config nhận tiền. Ô Pool lấy danh sách từ PoolDatabase.</summary>
    [CustomEditor(typeof(CurrencyReceiveConfig))]
    public sealed class CurrencyReceiveConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "_poolId");
            DrawPool(serializedObject.FindProperty("_poolId"));
            serializedObject.ApplyModifiedProperties();
        }

        static void DrawPool(SerializedProperty poolId)
        {
            if (poolId == null) return;

            var labels = new List<string>();
            var ids = new List<int>();
            labels.Add("0 (chưa gắn)");
            ids.Add(0);

            string[] guids = AssetDatabase.FindAssets("t:PoolDatabaseSO");
            for (int g = 0; g < guids.Length; g++)
            {
                var db = AssetDatabase.LoadAssetAtPath<PoolDatabaseSO>(AssetDatabase.GUIDToAssetPath(guids[g]));
                if (db == null || db.entries == null) continue;
                for (int i = 0; i < db.entries.Count; i++)
                {
                    var entry = db.entries[i];
                    if (entry == null || entry.id <= 0) continue;
                    labels.Add(db.name + " / " + entry.Name + " #" + entry.id);
                    ids.Add(entry.id);
                }
            }

            int current = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == poolId.intValue)
                {
                    current = i;
                    break;
                }
            }

            if (poolId.intValue > 0 && current == 0)
            {
                labels.Add(poolId.intValue + " (không có trong PoolDatabase)");
                ids.Add(poolId.intValue);
                current = ids.Count - 1;
            }

            int next = EditorGUILayout.Popup("Pool", current, labels.ToArray());
            if (next >= 0 && next < ids.Count)
                poolId.intValue = ids[next];

            if (poolId.intValue <= 0)
                EditorGUILayout.HelpBox("Chạy Tools → LSDK → Currency → Create Receive Setup để tạo prefab bay và gắn pool.", MessageType.Info);
        }
    }
}
