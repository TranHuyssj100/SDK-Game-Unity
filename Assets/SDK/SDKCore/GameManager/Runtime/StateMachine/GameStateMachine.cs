// Assets/SDK/SDKCore/GameManager/Runtime/StateMachine/GameStateMachine.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using LSDK.Core.Observer;

namespace LSDK.GameManager
{
    /// <summary>
    /// Engine của State Machine — pure C#, không phụ thuộc MonoBehaviour.
    /// <para>
    /// Workflow:
    /// <list type="number">
    ///   <item>Gọi <see cref="Register"/> để đăng ký tất cả <see cref="IGameState"/>.</item>
    ///   <item>Gọi <see cref="ChangeState"/> để chuyển trạng thái.</item>
    ///   <item>Gọi <see cref="Tick"/> từ MonoBehaviour.Update mỗi frame.</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class GameStateMachine
    {
        // ── Fields ──────────────────────────────────────────────

        private readonly Dictionary<GameStateType, IGameState> _states
            = new Dictionary<GameStateType, IGameState>();

        // ── Properties ──────────────────────────────────────────

        /// <summary>State đang active. Null trước lần ChangeState đầu tiên.</summary>
        public IGameState CurrentState { get; private set; }

        /// <summary>Loại state đang active.</summary>
        public GameStateType CurrentStateType
            => CurrentState?.StateType ?? GameStateType.None;

        // ── Events ───────────────────────────────────────────────

        /// <summary>
        /// Phát ngay sau khi state mới Enter() hoàn tất.
        /// Tham số: (previousState, newState).
        /// </summary>
        public event Action<GameStateType, GameStateType> OnStateChanged;

        // ── Register ─────────────────────────────────────────────

        /// <summary>
        /// Đăng ký một state vào máy trạng thái.
        /// Phải gọi trước khi dùng <see cref="ChangeState"/>.
        /// </summary>
        public void Register(IGameState state)
        {
            if (state == null)
            {
                Debug.LogWarning("[LSDK.GameManager] Register: state is null, bỏ qua.");
                return;
            }

            if (_states.ContainsKey(state.StateType))
                Debug.LogWarning($"[LSDK.GameManager] State '{state.StateType}' đã được đăng ký, ghi đè.");

            _states[state.StateType] = state;
        }

        // ── Transition ───────────────────────────────────────────

        /// <summary>
        /// Chuyển sang <paramref name="next"/>.
        /// Thứ tự: Exit() state hiện tại → gán CurrentState → Enter() state mới → phát event.
        /// </summary>
        /// <param name="next">State muốn chuyển đến.</param>
        /// <param name="ctx">Context truyền vào Enter(). Mặc định là context rỗng.</param>
        public void ChangeState(GameStateType next, GameStateContext ctx = null)
        {
            if (!_states.TryGetValue(next, out var nextState))
            {
                Debug.LogError($"[LSDK.GameManager] State '{next}' chưa được đăng ký. Gọi Register() trước.");
                return;
            }

            // Không chuyển sang chính state đang active (tránh re-enter vô tình)
            if (CurrentState != null && CurrentState.StateType == next)
            {
                Debug.LogWarning($"[LSDK.GameManager] Đang ở state '{next}' rồi, bỏ qua.");
                return;
            }

            var previous = CurrentStateType;

            // --- Exit ---
            if (CurrentState != null)
            {
                Debug.Log($"[LSDK.GameManager] Exit  ← {previous}");
                try { CurrentState.Exit(); }
                catch (Exception e) { Debug.LogException(e); }
            }

            // --- Swap ---
            CurrentState = nextState;

            // --- Enter ---
            Debug.Log($"[LSDK.GameManager] Enter → {next}");
            try { CurrentState.Enter(ctx ?? new GameStateContext()); }
            catch (Exception e) { Debug.LogException(e); }

            // --- Notify ---
            try { OnStateChanged?.Invoke(previous, next); }
            catch (Exception e) { Debug.LogException(e); }

            // Broadcast toàn cục qua Observer
            Observer.Publish(GameEvents.OnGameStateChanged,
                new GameStateChangedData { From = previous, To = next });
        }

        // ── Tick ─────────────────────────────────────────────────

        /// <summary>
        /// Gọi từ MonoBehaviour.Update mỗi frame.
        /// Forward deltaTime xuống state đang active.
        /// </summary>
        public void Tick(float deltaTime)
        {
            try { CurrentState?.Tick(deltaTime); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
