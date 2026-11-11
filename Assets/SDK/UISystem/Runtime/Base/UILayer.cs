namespace LSDK.UISystem
{
    // Giá trị = index trong UIManager.layerRoots
    public enum UILayer
    {
        Screen = 0,   // stack, chỉ 1 screen hiện
        Popup = 1,    // stack, chồng lên screen
        Overlay = 2,  // toast, tooltip... không stack
        Blocker = 3   // loading, fullscreen block...
    }
}
