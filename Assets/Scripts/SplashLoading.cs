using UnityEngine;
using LSDK.Loading;

public class SplashLoading : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadingManager.Instance.LoadScene("TestSDK");
    }


}
