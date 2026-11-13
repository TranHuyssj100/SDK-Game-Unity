// Assets/SDK/SDKCore/GameManager/Runtime/StateMachine/GameStateType.cs
using System;
using System.Collections.Generic;

namespace LSDK.GameManager
{
    /// <summary>
    /// Định danh một Game State — có thể mở rộng từ bất kỳ project nào.
    /// <para>
    /// <b>SDK built-in states</b> được khai báo như static readonly fields bên dưới.
    /// </para>
    /// <para>
    /// <b>Thêm state mới cho từng game</b> (không sửa SDK):
    /// <code>
    /// // Trong game project, tạo file CustomGameStates.cs:
    /// public static class CustomGameStates
    /// {
    ///     public static readonly GameStateType Shop     = GameStateType.Register("Shop");
    ///     public static readonly GameStateType Tutorial = GameStateType.Register("Tutorial");
    /// }
    ///
    /// // Rồi đăng ký vào GameManager:
    /// _sm.Register(new ShopState());
    /// GameManager.Instance.ChangeStateTo(CustomGameStates.Shop);
    /// </code>
    /// </para>
    /// </summary>
    public sealed class GameStateType : IEquatable<GameStateType>
    {
        // ── Built-in States ──────────────────────────────────────

        /// <summary>Chưa khởi tạo (trạng thái mặc định ban đầu).</summary>
        public static readonly GameStateType None          = new GameStateType(0, "None");

        /// <summary>Đang boot: load service, preload asset thiết yếu.</summary>
        public static readonly GameStateType Booting       = new GameStateType(1, "Booting");

        /// <summary>Đang hiển thị Main Menu.</summary>
        public static readonly GameStateType MainMenu      = new GameStateType(2, "MainMenu");

        /// <summary>Đang load scene level.</summary>
        public static readonly GameStateType LoadingGame   = new GameStateType(3, "LoadingGame");

        /// <summary>Gameplay đang chạy.</summary>
        public static readonly GameStateType Playing       = new GameStateType(4, "Playing");

        /// <summary>Game tạm dừng.</summary>
        public static readonly GameStateType Paused        = new GameStateType(5, "Paused");

        /// <summary>Player hoàn thành level.</summary>
        public static readonly GameStateType LevelComplete = new GameStateType(6, "LevelComplete");

        /// <summary>Player thua / game over.</summary>
        public static readonly GameStateType GameOver      = new GameStateType(7, "GameOver");

        // ── Registry ─────────────────────────────────────────────

        // ID tiếp theo dành cho các custom state từ game project (bắt đầu từ 100)
        private static int _nextCustomId = 100;

        // Tra cứu ngược từ id → instance (hữu ích để debug / serialize)
        private static readonly Dictionary<int, GameStateType> _registry
            = new Dictionary<int, GameStateType>
            {
                { 0, None },
                { 1, Booting },
                { 2, MainMenu },
                { 3, LoadingGame },
                { 4, Playing },
                { 5, Paused },
                { 6, LevelComplete },
                { 7, GameOver },
            };

        // ── Properties ───────────────────────────────────────────

        /// <summary>ID số nguyên duy nhất.</summary>
        public int Id { get; }

        /// <summary>Tên dễ đọc, dùng để log / debug.</summary>
        public string Name { get; }

        // ── Constructor (private — tạo qua Register hoặc built-in) ──

        private GameStateType(int id, string name)
        {
            Id   = id;
            Name = name ?? id.ToString();
        }

        // ── Public factory ───────────────────────────────────────

        /// <summary>
        /// Đăng ký một state type mới cho game project.
        /// Gọi một lần khi khởi động, lưu kết quả vào static readonly field.
        /// </summary>
        /// <param name="name">Tên dễ đọc (không trùng với tên đã có để tránh nhầm lẫn).</param>
        /// <returns>Instance GameStateType mới, ID tự tăng từ 100.</returns>
        /// <example>
        /// <code>
        /// public static readonly GameStateType Shop = GameStateType.Register("Shop");
        /// </code>
        /// </example>
        public static GameStateType Register(string name)
        {
            var instance = new GameStateType(_nextCustomId++, name);
            _registry[instance.Id] = instance;
            return instance;
        }

        /// <summary>
        /// Tra cứu GameStateType theo ID.
        /// Trả về <see cref="None"/> nếu không tìm thấy.
        /// </summary>
        public static GameStateType FromId(int id)
            => _registry.TryGetValue(id, out var result) ? result : None;

        // ── Equality ─────────────────────────────────────────────

        /// <inheritdoc/>
        public bool Equals(GameStateType other) => other is not null && Id == other.Id;

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as GameStateType);

        /// <inheritdoc/>
        public override int GetHashCode() => Id;

        /// <summary>So sánh bằng theo ID.</summary>
        public static bool operator ==(GameStateType a, GameStateType b)
        {
            if (a is null) return b is null;
            return a.Equals(b);
        }

        /// <summary>So sánh khác theo ID.</summary>
        public static bool operator !=(GameStateType a, GameStateType b) => !(a == b);

        /// <inheritdoc/>
        public override string ToString() => $"{Name}({Id})";
    }
}
