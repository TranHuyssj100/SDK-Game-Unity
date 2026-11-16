using UnityEngine;
using LSDK.UISystem;
using UnityEngine.UI;
using LSDK.Firebase;
using System.Collections;
public class GameManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] UIId UIType;
    [SerializeField] Button OpenUIBtn;
    [SerializeField] Button CloseUIBtn;
    void Start()
    {
        OpenUIBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.Open(UIType);
        });
        CloseUIBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.Close(UIType);
        });
        StartCoroutine(nameof(GetCoinBaseValue));
    }

    IEnumerator GetCoinBaseValue()
    {
        yield return new WaitUntil(() => FirebaseManager.Instance.IsInitialized );
        Debug.Log("CoinBaseValue: " + RemoteConfigManager.Instance.Get<CurrencyRemoteConfig>().CoinBaseValue);
    }
}
