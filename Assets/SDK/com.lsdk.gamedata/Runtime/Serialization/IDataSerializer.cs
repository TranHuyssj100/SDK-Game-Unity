namespace LSDK.GameData
{
    public interface IDataSerializer
    {
        string Serialize(object data);

        /// <summary>Đổ JSON vào instance có sẵn (không tạo mới, giữ nguyên reference).</summary>
        void Populate(string json, object target);
    }
}
