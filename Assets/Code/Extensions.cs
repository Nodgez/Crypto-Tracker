using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    public static void On(this CanvasGroup canvasGrp)
    {
        canvasGrp.alpha = 1;
        canvasGrp.blocksRaycasts = true;
        canvasGrp.interactable = true;
    }

    public static void Off(this CanvasGroup canvasGrp)
    {
        canvasGrp.alpha = 0;
        canvasGrp.blocksRaycasts = false;
        canvasGrp.interactable = false;
    }

    public static void ClearChildren(this Transform t)
    {
        for (int i = t.childCount - 1; i > -1; i--)
        {
            GameObject.Destroy(t.GetChild(i).gameObject);
        }
    }
}
