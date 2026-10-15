using LSDK.IAP;
using UnityEngine;
using UnityEngine.UI;

public class IAPButton : MonoBehaviour
{
    [SerializeField] string productId;
    [SerializeField] Button button;

    void Start()
    {
    }

    void OnEnable()
    {
        button.onClick.AddListener(OnButtonClick);
    }

    void OnDisable()
    {
        button.onClick.RemoveListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        IAPManager.Instance.Purchase(productId, callback =>
        {
            if (callback.success)
            {
                OnPurchaseSuccess(callback);
            }
            else
            {
                OnPurchaseFailed(callback);
            }
        });
    }

    void OnPurchaseSuccess(IAPResult result)
    {
        Debug.Log("Purchase success: " + result.productId);
    }

    void OnPurchaseFailed(IAPResult result)
    {
        Debug.Log("Purchase failed: " + result.productId);
    }
}
