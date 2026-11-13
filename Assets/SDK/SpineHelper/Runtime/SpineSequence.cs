using System;
using System.Collections.Generic;

namespace LSDK.SpineAnim
{
    /// <summary>Một bước trong sequence.</summary>
    [Serializable]
    public class SpineSequenceStep
    {
        public string animation;

        /// <summary>1 = chạy 1 lần, n = lặp n lần rồi sang bước tiếp, 0 = lặp vô hạn (sequence dừng ở bước này).</summary>
        public int loopCount = 1;

        /// <summary>Thời gian mix từ animation trước. -1 = dùng defaultMix của controller.</summary>
        public float mixDuration = -1f;

        public float timeScale = 1f;
    }

    /// <summary>Chuỗi animation chạy liên tục trên 1 track.</summary>
    [Serializable]
    public class SpineSequence
    {
        public string name = "New Sequence";
        public int track = 0;

        /// <summary>Hết bước cuối thì quay lại bước đầu.</summary>
        public bool loop = false;

        public List<SpineSequenceStep> steps = new List<SpineSequenceStep>();
    }
}
