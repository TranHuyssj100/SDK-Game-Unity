namespace LSDK.GameData
{
    /// <summary>Tầng lưu trữ. Đổi sang file / cloud save chỉ cần implement interface này.</summary>
    public interface IGameDataStorage
    {
        bool HasKey(string key);
        string Read(string key);
        void Write(string key, string value);
        void Delete(string key);

        /// <summary>Ghi xuống đĩa (PlayerPrefs.Save).</summary>
        void Flush();
    }
}
