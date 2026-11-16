using System;

namespace LSDK.Firebase
{
    /// <summary>Đặt trên field của RemoteConfigModule để đổi key Firebase thay vì dùng tên field.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RCKeyAttribute : Attribute
    {
        public string Key { get; }

        public RCKeyAttribute(string key)
        {
            Key = key ?? string.Empty;
        }
    }
}
