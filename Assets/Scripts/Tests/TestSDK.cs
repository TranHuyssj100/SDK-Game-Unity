using UnityEngine;
using Highmood.Sdk.Core;

public class TestSDK : MonoBehaviour
{
    public void OnEnable()
    {
        Observer.Subscribe(EventConstants.TEST_EVENT, OnTestEvent);
    }


    public void OnDisable()
    {
        Observer.Unsubscribe(EventConstants.TEST_EVENT, OnTestEvent);
    }
    void Start()
    {
        Observer.Publish(EventConstants.TEST_EVENT);
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