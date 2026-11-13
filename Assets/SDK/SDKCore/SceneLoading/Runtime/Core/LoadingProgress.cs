using UnityEngine;

namespace LSDK.Loading
{
    /// <summary>
    /// Thanh tiến độ đuổi theo giá trị thật, để scene đứng ở 0.9 không làm thanh nhảy.
    /// </summary>
    public sealed class LoadingProgress
    {
        public float Target { get; private set; }
        public float Display { get; private set; }

        public void Reset()
        {
            Target = 0f;
            Display = 0f;
        }

        public void SetTarget(float value) => Target = Mathf.Clamp01(value);

        public void Snap() => Display = Target;

        public float Tick(float speed, float deltaTime)
        {
            if (speed <= 0f) Display = Target;
            else Display = Mathf.MoveTowards(Display, Target, speed * deltaTime);
            return Display;
        }
    }
}
