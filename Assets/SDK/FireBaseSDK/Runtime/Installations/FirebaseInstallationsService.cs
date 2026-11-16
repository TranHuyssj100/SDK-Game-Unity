using System;
using System.Collections;
using System.Threading.Tasks;
using Firebase.Installations;

namespace LSDK.Firebase
{
    internal sealed class FirebaseInstallationsService
    {
        public string Id { get; private set; }

        public IEnumerator FetchId(Action<bool, string> done)
        {
            Task<string> task;
            try
            {
                task = FirebaseInstallations.DefaultInstance.GetIdAsync();
            }
            catch (Exception exception)
            {
                FirebaseLog.Exception(exception);
                done?.Invoke(false, null);
                yield break;
            }

            yield return FirebaseTasks.Wait(task);
            string error = FirebaseTasks.GetError(task);
            if (error != null)
            {
                FirebaseLog.Warning(error);
                done?.Invoke(false, null);
                yield break;
            }

            Id = task.Result ?? string.Empty;
            done?.Invoke(true, Id);
        }
    }
}
