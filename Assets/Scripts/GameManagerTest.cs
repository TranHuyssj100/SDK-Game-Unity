using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using LSDK.Currency;
using LSDK.Firebase;
using LSDK.ToastSystem;
using LSDK.UISystem;
using UnityEngine;
using UnityEngine.UI;
public class GameManagerTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] UIId UIType;
    [SerializeField] Button OpenUIBtn;
    [SerializeField] Button CloseUIBtn;
    [Header("Currency")]
    public CurrencyType CurrencyType;
    [SerializeField] Button AddCurrencyBtn;
    [SerializeField] Button SubCurrencyBtn;
    [SerializeField] int amount;
    [Header("Toast")]
    [SerializeField] ToastType ToastType;
    [SerializeField] Button ShowToastBtn;



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
        ShowToastBtn.onClick.AddListener(ShowToast);
        AddCurrencyBtn.onClick.AddListener(AddCurrency);
        SubCurrencyBtn.onClick.AddListener(SubCurrency);
    }

    IEnumerator GetCoinBaseValue()
    {
        yield return new WaitUntil(() => FirebaseManager.Instance.IsInitialized);
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

    void ShowToast()
    {
        ToastPanel.Show("This is a toast message", ToastType);
    }
    void AddCurrency()
    {
        CurrencyManager.Instance.Add(CurrencyType, amount, transform); // null: bay từ giữa màn hình
    }
    void SubCurrency()
    {
        CurrencyManager.Instance.TrySpend(CurrencyType, amount);
    }
}
