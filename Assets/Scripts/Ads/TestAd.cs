using System;
using LSDK.Mediation;
using UnityEngine;
using UnityEngine.UI;

public class TestAd : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private Button _showBannerButton;
    [SerializeField] private Button _hideBannerButton;

    [SerializeField] private Button _showInterstitialButton;
    [SerializeField] private Button _showRewardedButton;
    [SerializeField] private Button _showAppOpenButton;

    [SerializeField] private Button privacyButton;

    public GameObject InternetCheckerUI;


    private void Awake()
    {
        _showBannerButton.onClick.AddListener(ShowBanner);
        _hideBannerButton.onClick.AddListener(HideBanner);
        _showInterstitialButton.onClick.AddListener(ShowInterstitial);
        _showRewardedButton.onClick.AddListener(ShowRewarded);
        _showAppOpenButton.onClick.AddListener(ShowAppOpen);
        privacyButton.onClick.AddListener(ShowPrivacyOptions);
    }

    private void ShowPrivacyOptions()
    {
        MediationManager.Instance.ShowPrivacyOptionsForm(() =>
        {
            Debug.Log("User đã chỉnh xong consent.");
        });
    }

    void Start()
    {
        Debug.Log("lew_Hashed Device ID: " + SystemInfo.deviceUniqueIdentifier);

        ShowBanner();
        ShowAppOpen();
        privacyButton.gameObject.SetActive(MediationManager.Instance.IsPrivacyOptionsRequired);


    }
    private void OnEnable()
    {
        MediationManager.OnNetworkChanged += OnNetworkChanged;
    }
    private void OnDisable()
    {
        MediationManager.OnNetworkChanged -= OnNetworkChanged;
    }
    private void OnNetworkChanged(bool isNetworkAvailable)
    {
        Debug.Log("lew_OnNetworkChanged: " + isNetworkAvailable);
        InternetCheckerUI.SetActive(!isNetworkAvailable);
    }


    private void ShowRewardedInterstitial()
    {
        // MediationManager.Instance.ShowRewardedInterstitial();
    }

    private void ShowRewarded()
    {
        MediationManager.Instance.ShowRewarded("rewarded", null);
    }

    private void ShowInterstitial()
    {
        MediationManager.Instance.ShowInterstitial("interstitial", null);
    }

    private void HideBanner()
    {
        MediationManager.Instance.HideBanner();
    }

    public void ShowBanner()
    {
        Debug.Log("lew_ShowBanner");
        MediationManager.Instance.ShowBanner();
    }
    private void ShowAppOpen()
    {
        MediationManager.Instance.ShowAppOpen("appopen", null);
    }
}
