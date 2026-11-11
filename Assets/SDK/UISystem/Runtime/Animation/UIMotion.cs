namespace LSDK.UISystem
{
    // Chọn trên UIAnimator. Mặc định là Scale.
    // Slide* = cạnh màn hình mà panel vào từ đó (Show) / đi tới đó (Hide).
    public enum UIMotion
    {
        None = 0,
        Scale = 1,
        SlideTop = 2,
        SlideBottom = 3,
        SlideLeft = 4,
        SlideRight = 5
    }
}
