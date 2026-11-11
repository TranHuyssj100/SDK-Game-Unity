using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class DOTweenAddon : MonoBehaviour
{
    public DOTweenAnimation[] Tweens;
    private void OnEnable()
    {
        foreach (var item in Tweens)
        {
            if (item != null)
                item.DORestart();
        }
    }
}
