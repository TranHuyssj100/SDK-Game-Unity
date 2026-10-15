using LSDK.Core.Observer;
using LSDK.GameData;
using UnityEngine;
using LSDK.IAP;

public class TestSDK : MonoBehaviour
{
    void Start()
    {
        Observer.Publish(EventConstants.TEST_EVENT);
        IAPManager.Instance.Init();
    }

    public void OnEnable()
    {
        Observer.Subscribe(EventConstants.TEST_EVENT, OnTestEvent);
    }
    public void OnDisable()
    {
        Observer.Unsubscribe(EventConstants.TEST_EVENT, OnTestEvent);
    }

    public void OnTestEvent()
    {
        Debug.Log("TestEvent");
    }
    public void OnTestEventParameter(string data)
    {
        Debug.Log("TestEventParameter: " + data);
    }
}