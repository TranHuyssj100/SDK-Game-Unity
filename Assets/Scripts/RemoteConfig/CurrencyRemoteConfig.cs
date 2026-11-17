using System.Collections.Generic;
using LSDK.Firebase;
using UnityEngine;
public class CurrencyRemoteConfig : RemoteConfigModule
{
    [SerializeField] private int coinBaseValue = 10;
    [SerializeField] private long coinLong = 1000000000;
    [SerializeField] private string coinString = "coin";
    [SerializeField] private bool coinBool = true;
    [SerializeField] private List<int> coinList = new List<int>() { 1, 2, 3 };
    [SerializeField] private Dictionary<string, int> coinDictionary = new Dictionary<string, int>() { { "key1", 10 }, { "key2", 20 }, { "key3", 30 } };
    
    public int CoinBaseValue => coinBaseValue;
    public string CoinString => coinString;
    public List<int> CoinList => coinList;
    public Dictionary<string, int> CoinDictionary => coinDictionary;
    public bool CoinBool => coinBool;
    public long CoinLong => coinLong;
}
