using UnityEngine;
using TMPro;

public class AdaptiveTextColor : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Camera mainCamera;

    void Start()
    {

        Color backgroundColor = mainCamera.backgroundColor;


        float luminance = CalculateLuminance(backgroundColor);


        if (luminance > 0.5f)
        {

            text.color = Color.black;
        }
        else
        {

            text.color = Color.white;
        }
    }


    float CalculateLuminance(Color color)
    {
        return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
    }
}
