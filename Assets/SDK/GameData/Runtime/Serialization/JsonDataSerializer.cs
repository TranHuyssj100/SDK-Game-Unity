using UnityEngine;

namespace LSDK.GameData
{
    /// <summary>
    /// JsonUtility: nhanh, không cần package. Không hỗ trợ Dictionary / polymorphism lồng nhau.
    /// Cần thì tạo NewtonsoftDataSerializer implement IDataSerializer rồi SetBackend().
    /// </summary>
    public sealed class JsonDataSerializer : IDataSerializer
    {
        private readonly bool _prettyPrint;

        public JsonDataSerializer(bool prettyPrint = false)
        {
            _prettyPrint = prettyPrint;
        }

        public string Serialize(object data)
        {
            return JsonUtility.ToJson(data, _prettyPrint);
        }

        public void Populate(string json, object target)
        {
            JsonUtility.FromJsonOverwrite(json, target);
        }
    }
}
 