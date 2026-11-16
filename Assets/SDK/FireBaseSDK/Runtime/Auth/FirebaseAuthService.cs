using System;
using System.Collections;
using System.Threading.Tasks;
using Firebase.Auth;

namespace LSDK.Firebase
{
    internal sealed class FirebaseAuthService
    {
        private readonly Action<Action> _post;
        private bool _listening;

        public FirebaseAuthService(Action<Action> post)
        {
            _post = post;
        }

        public FirebaseUserInfo Current { get; private set; }
        public bool IsListening => _listening;

        public void Start(Action<FirebaseUserInfo> onChanged)
        {
            if (_listening) return;
            _onChanged = onChanged;
            _listening = true;
            FirebaseAuth.DefaultInstance.StateChanged += HandleStateChanged;
            Publish(FirebaseAuth.DefaultInstance.CurrentUser);
        }

        public void Stop()
        {
            if (!_listening) return;
            FirebaseAuth.DefaultInstance.StateChanged -= HandleStateChanged;
            _listening = false;
            _onChanged = null;
        }

        public void SignOut()
        {
            if (!_listening) return;
            FirebaseAuth.DefaultInstance.SignOut();
        }

        public IEnumerator SignInAnonymously(Action<bool, FirebaseUserInfo, string> done)
        {
            if (!_listening)
            {
                done?.Invoke(false, null, "Auth chưa bật.");
                yield break;
            }

            Task<AuthResult> task = FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync();
            yield return Run(task, done);
        }

        public IEnumerator SignInWithEmail(string email, string password, Action<bool, FirebaseUserInfo, string> done)
        {
            if (!CanUsePassword(email, password, done)) yield break;
            Task<AuthResult> task = FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(email, password);
            yield return Run(task, done);
        }

        public IEnumerator CreateUserWithEmail(string email, string password, Action<bool, FirebaseUserInfo, string> done)
        {
            if (!CanUsePassword(email, password, done)) yield break;
            Task<AuthResult> task = FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(email, password);
            yield return Run(task, done);
        }

        private Action<FirebaseUserInfo> _onChanged;

        private void HandleStateChanged(object sender, EventArgs args)
        {
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
            FirebaseUserInfo info = Map(user);
            _post(() => Publish(info));
        }

        private void Publish(FirebaseUser user)
        {
            Publish(Map(user));
        }

        private void Publish(FirebaseUserInfo info)
        {
            Current = info;
            Action<FirebaseUserInfo> handler = _onChanged;
            if (handler != null) handler(info);
        }

        private IEnumerator Run(Task<AuthResult> task, Action<bool, FirebaseUserInfo, string> done)
        {
            yield return FirebaseTasks.Wait(task);
            string error = FirebaseTasks.GetError(task);
            if (error != null)
            {
                FirebaseLog.Warning(error);
                done?.Invoke(false, null, error);
                yield break;
            }

            FirebaseUserInfo info = Map(task.Result == null ? null : task.Result.User);
            Current = info;
            done?.Invoke(info != null, info, null);
        }

        private static bool CanUsePassword(string email, string password, Action<bool, FirebaseUserInfo, string> done)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                done?.Invoke(false, null, "Email hoặc mật khẩu trống.");
                return false;
            }

            return true;
        }

        private static FirebaseUserInfo Map(FirebaseUser user)
        {
            if (user == null) return null;
            return new FirebaseUserInfo(user.UserId, user.Email, user.DisplayName, user.IsAnonymous);
        }
    }
}
