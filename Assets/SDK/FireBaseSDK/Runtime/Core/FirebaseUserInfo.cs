namespace LSDK.Firebase
{
    /// <summary>Ảnh chụp user Auth. Null nghĩa là đã đăng xuất.</summary>
    public sealed class FirebaseUserInfo
    {
        public FirebaseUserInfo(string userId, string email, string displayName, bool isAnonymous)
        {
            UserId = userId ?? string.Empty;
            Email = email ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            IsAnonymous = isAnonymous;
        }

        public string UserId { get; }
        public string Email { get; }
        public string DisplayName { get; }
        public bool IsAnonymous { get; }
    }
}
