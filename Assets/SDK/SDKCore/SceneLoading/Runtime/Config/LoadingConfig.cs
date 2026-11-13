using UnityEngine;

namespace LSDK.Loading
{
    [CreateAssetMenu(menuName = "LSDK/Loading/Loading Config", fileName = "LoadingConfig")]
    public class LoadingConfig : ScriptableObject
    {
        [Min(0f)]
        [Tooltip("Thời gian tối thiểu màn che ở trạng thái kín, tính từ lúc animation show xong.")]
        public float minimumCoveredTime = 0.6f;

        [Min(0f)]
        [Tooltip("Tốc độ thanh tiến độ đuổi theo giá trị thật, đơn vị 0-1 mỗi giây (unscaled). 0 = nhảy ngay.")]
        public float progressSmoothSpeed = 1.5f;

        [Tooltip("Câu hiện trên LoadingPanel lúc mở.")]
        public string[] tips;

        public bool randomTip = true;
    }
}
