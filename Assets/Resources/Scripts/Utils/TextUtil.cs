using TMPro;
using UnityEngine;

public static class TextUtil
{

    public static bool SetText(TextMeshProUGUI gameObject, string text)
    {
        if (gameObject == null)
        {
            return false;
        }
        else
        {
            gameObject.text = text;
            return true;
        }
    }
}