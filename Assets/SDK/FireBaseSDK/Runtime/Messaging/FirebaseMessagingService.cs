using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Messaging;

namespace LSDK.Firebase
{
    internal sealed class FirebaseMessagingService
    {
        private readonly Action<Action> _post;
        private bool _listening;

        public FirebaseMessagingService(Action<Action> post)
        {
            _post = post;
        }

        public string RegistrationId { get; private set; }
        public bool IsListening => _listening;

        public void Start(Action<string> onRegistration, Action<FirebasePushMessage> onMessage)
        {
            if (_listening) return;
            _onRegistration = onRegistration;
            _onMessage = onMessage;
            FirebaseMessaging.RegistrationReceived += HandleRegistration;
            FirebaseMessaging.MessageReceived += HandleMessage;
            _listening = true;
        }

        public void Stop()
        {
            if (!_listening) return;
            FirebaseMessaging.RegistrationReceived -= HandleRegistration;
            FirebaseMessaging.MessageReceived -= HandleMessage;
            _listening = false;
            _onRegistration = null;
            _onMessage = null;
        }

        public void SetAutoRegister(bool enabled)
        {
            FirebaseMessaging.RegistrationOnInitEnabled = enabled;
        }

        public IEnumerator Register(Action<bool> done)
        {
            yield return Run(FirebaseMessaging.RegisterAsync(), done, "Đăng ký FCM lỗi.");
        }

        public IEnumerator RequestPermission(Action<bool> done)
        {
            yield return Run(FirebaseMessaging.RequestPermissionAsync(), done, "Xin quyền thông báo lỗi.");
        }

        public IEnumerator Subscribe(string topic, Action<bool> done)
        {
            if (!IsTopicValid(topic, done)) yield break;
            yield return Run(FirebaseMessaging.SubscribeAsync(topic), done, "Subscribe topic lỗi.");
        }

        public IEnumerator Unsubscribe(string topic, Action<bool> done)
        {
            if (!IsTopicValid(topic, done)) yield break;
            yield return Run(FirebaseMessaging.UnsubscribeAsync(topic), done, "Unsubscribe topic lỗi.");
        }

        private Action<string> _onRegistration;
        private Action<FirebasePushMessage> _onMessage;

        private void HandleRegistration(object sender, RegistrationReceivedEventArgs args)
        {
            string id = args == null ? string.Empty : args.InstallationId;
            _post(() =>
            {
                RegistrationId = id ?? string.Empty;
                Action<string> handler = _onRegistration;
                if (handler != null) handler(RegistrationId);
            });
        }

        private void HandleMessage(object sender, MessageReceivedEventArgs args)
        {
            if (args == null || args.Message == null) return;
            FirebaseMessage source = args.Message;
            var data = new Dictionary<string, string>();
            if (source.Data != null)
            {
                foreach (KeyValuePair<string, string> pair in source.Data)
                    data[pair.Key] = pair.Value;
            }

            string title = string.Empty;
            string body = string.Empty;
            if (source.Notification != null)
            {
                title = source.Notification.Title;
                body = source.Notification.Body;
            }

            var message = new FirebasePushMessage(source.From, source.MessageId, title, body, data);
            _post(() =>
            {
                Action<FirebasePushMessage> handler = _onMessage;
                if (handler != null) handler(message);
            });
        }

        private static bool IsTopicValid(string topic, Action<bool> done)
        {
            if (!string.IsNullOrEmpty(topic)) return true;
            done?.Invoke(false);
            return false;
        }

        private static IEnumerator Run(Task task, Action<bool> done, string failLog)
        {
            yield return FirebaseTasks.Wait(task);
            string error = FirebaseTasks.GetError(task);
            if (error != null)
            {
                FirebaseLog.Warning(failLog + " " + error);
                done?.Invoke(false);
                yield break;
            }

            done?.Invoke(true);
        }
    }
}
