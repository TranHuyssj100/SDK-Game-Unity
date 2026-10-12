using System;
using UnityEngine;

namespace LSDK.GameData
{
    /// <summary>
    /// Base cho mọi nhóm data. Mỗi module = 1 key JSON riêng trong storage.
    /// Yêu cầu: [Serializable] + có constructor mặc định (không tham số).
    /// </summary>
    [Serializable]
    public abstract class GameDataModule
    {
        [SerializeField] private int version;

        /// <summary>Key lưu trữ, lấy từ GameDataKeys.</summary>
        public abstract string Key { get; }

        /// <summary>Version schema hiện tại của code. Tăng lên khi đổi cấu trúc data.</summary>
        public virtual int CurrentVersion => 1;

        /// <summary>Version của data đã lưu (đọc từ JSON).</summary>
        public int Version => version;

        /// <summary>true = có thay đổi chưa ghi xuống storage.</summary>
        public bool IsDirty { get; internal set; }

        /// <summary>Bắn mỗi khi data đổi (UI có thể subscribe).</summary>
        public event Action<GameDataModule> Changed;

        /// <summary>Gọi sau mỗi lần sửa data.</summary>
        public void Save()
        {
            IsDirty = true;
            Changed?.Invoke(this);
        }

        /// <summary>Đặt giá trị mặc định cho lần chạy đầu tiên.</summary>
        public void InitializeDefaults()
        {
            version = CurrentVersion;
            OnCreated();
        }

        internal void SetVersion(int value) { version = value; }
        internal void MigrateStep(int fromVersion) { OnMigrate(fromVersion); }

        /// <summary>Set default value ở đây (lần đầu chưa có save).</summary>
        protected virtual void OnCreated() { }

        /// <summary>Nâng data từ fromVersion lên fromVersion + 1.</summary>
        protected virtual void OnMigrate(int fromVersion) { }

        /// <summary>Sau khi load xong (hoặc reset).</summary>
        public virtual void OnLoaded() { }

        /// <summary>Ngay trước khi serialize.</summary>
        public virtual void OnBeforeSave() { }
    }
}
