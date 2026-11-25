// Assets/SDK/CurencyManager/Runtime/CurrencyRecord.cs
using System;
using UnityEngine;

namespace LSDK.Currency
{
    /// <summary>Số dư và hai đồng hồ của một loại tiền. Một mốc hồi cho cả hàng chờ.</summary>
    [Serializable]
    public sealed class CurrencyRecord
    {
        [Tooltip("Id trong catalog. Đây là nguồn thật của loại tiền.")]
        [SerializeField] private int _typeId;

        [SerializeField] private long _amount;
        [SerializeField] private bool _seeded;
        [SerializeField] private int _nextRegenUnix;

        [Header("Unlimited")]
        [Tooltip("true khi unlimited còn hạn. Được đồng bộ mỗi lần Refresh, hết hạn thì tự về false.")]
        [SerializeField] private bool _isUnlimited;
        [SerializeField] private long _unlimitedUntilUnix;

        /// <summary>JsonUtility cần constructor không tham số.</summary>
        public CurrencyRecord() { }

        internal CurrencyRecord(int typeId)
        {
            _typeId = typeId;
        }

        /// <summary>Id trong catalog.</summary>
        public int TypeId => _typeId;

        /// <summary>Số dư hiện tại. Sàn 0.</summary>
        public long Amount => _amount;

        /// <summary>Unix UTC giây của đơn vị kế tiếp. 0 nghĩa là không có lịch.</summary>
        public int NextRegenUnix => _nextRegenUnix;

        /// <summary>true sau lần ghi đầu. Profile mới được đánh dấu sẵn.</summary>
        public bool Seeded => _seeded;

        /// <summary>
        /// true khi unlimited còn hạn tại lần Refresh gần nhất. Đây là cờ lưu sẵn,
        /// muốn đúng theo một mốc bất kỳ thì dùng <c>CurrencyManager.IsUnlimitedActive</c>.
        /// </summary>
        public bool IsUnlimited => _isUnlimited;

        /// <summary>Unix UTC giây unlimited hết hạn.</summary>
        public long UnlimitedUntilUnix => _unlimitedUntilUnix;

        internal void SetAmount(long value) { _amount = value; }
        internal void SetNextRegenUnix(int value) { _nextRegenUnix = value; }
        internal void SetSeeded(bool value) { _seeded = value; }
        internal void SetUnlimitedUntilUnix(long value) { _unlimitedUntilUnix = value; }

        /// <summary>Ghi cờ unlimited. Trả true nếu giá trị đổi.</summary>
        internal bool SetUnlimited(bool value)
        {
            if (_isUnlimited == value) return false;
            _isUnlimited = value;
            return true;
        }
    }
}
