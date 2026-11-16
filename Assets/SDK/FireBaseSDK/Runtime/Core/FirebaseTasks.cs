using System;
using System.Collections;
using System.Threading.Tasks;

namespace LSDK.Firebase
{
    /// <summary>Chờ Task của Firebase trên main thread bằng coroutine.</summary>
    internal static class FirebaseTasks
    {
        public static IEnumerator Wait(Task task)
        {
            if (task == null) yield break;
            while (!task.IsCompleted)
                yield return null;
        }

        public static string GetError(Task task)
        {
            if (task == null) return "Task null.";
            if (task.IsCanceled) return "Tác vụ bị hủy.";
            if (!task.IsFaulted) return null;
            Exception ex = task.Exception == null ? null : task.Exception.GetBaseException();
            return ex == null ? "Tác vụ lỗi." : ex.Message;
        }
    }
}
