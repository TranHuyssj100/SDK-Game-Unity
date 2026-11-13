using UnityEngine;
using LSDK.UISystem;
using UnityEngine.UI;
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
    }

   
}
