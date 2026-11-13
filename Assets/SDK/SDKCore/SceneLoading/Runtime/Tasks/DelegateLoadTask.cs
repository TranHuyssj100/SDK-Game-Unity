using System;
using System.Collections;

namespace LSDK.Loading
{
    /// <summary>
    /// Bọc một IEnumerator có sẵn của game. Ném lỗi giữa hai lần yield được ghi vào Error.
    /// </summary>
    public sealed class DelegateLoadTask : ILoadTask
    {
        readonly string _label;
        readonly float _weight;
        readonly Func<IProgress<float>, IEnumerator> _routine;

        public DelegateLoadTask(string label, Func<IProgress<float>, IEnumerator> routine)
            : this(label, 1f, routine) { }

        public DelegateLoadTask(string label, float weight, Func<IProgress<float>, IEnumerator> routine)
        {
            _label = label ?? "";
            _weight = weight < 0f ? 0f : weight;
            _routine = routine;
        }

        public DelegateLoadTask(string label, float weight, Func<IEnumerator> routine)
            : this(label, weight, _ => routine != null ? routine() : null) { }

        public string Label => _label;
        public float Weight => _weight;
        public string Error { get; private set; }

        public IEnumerator Run(IProgress<float> progress)
        {
            if (_routine == null)
            {
                progress?.Report(1f);
                yield break;
            }

            IEnumerator inner;
            try { inner = _routine(progress); }
            catch (Exception ex)
            {
                Error = ex.Message;
                yield break;
            }

            if (inner == null)
            {
                progress?.Report(1f);
                yield break;
            }

            while (true)
            {
                object current;
                try
                {
                    if (!inner.MoveNext()) break;
                    current = inner.Current;
                }
                catch (Exception ex)
                {
                    Error = ex.Message;
                    yield break;
                }
                yield return current;
            }

            progress?.Report(1f);
        }
    }
}
