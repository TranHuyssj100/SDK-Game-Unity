using LSDK.Firebase;
using UnityEngine;
public class CurrencyRemoteConfig : RemoteConfigModule
{
    [SerializeField] private int coinBaseValue = 10;
    public int CoinBaseValue
    {
        get => coinBaseValue;
        set => coinBaseValue = value;
    }

}
