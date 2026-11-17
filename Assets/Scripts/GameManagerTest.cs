using UnityEngine;
using LSDK.UISystem;
using UnityEngine.UI;
using LSDK.Firebase;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        Debug.Log("CoinString: " + RemoteConfigManager.Instance.Get<CurrencyRemoteConfig>().CoinString);
        Debug.Log("CoinBool: " + RemoteConfigManager.Instance.Get<CurrencyRemoteConfig>().CoinBool);
        Debug.Log("CoinLong: " + RemoteConfigManager.Instance.Get<CurrencyRemoteConfig>().CoinLong);

        List<int> coinList = RemoteConfigManager.Instance.Get<CurrencyRemoteConfig>().CoinList;
        foreach (int coin in coinList)
        {
            Debug.Log("Coin: " + coin);
        }

        Dictionary<string, int> coinDictionary = RemoteConfigManager.Instance.Get<CurrencyRemoteConfig>().CoinDictionary;
        foreach (KeyValuePair<string, int> coin in coinDictionary)
        {
            Debug.Log("Coin: " + coin.Key + " : " + coin.Value);
        }   
    }
}
