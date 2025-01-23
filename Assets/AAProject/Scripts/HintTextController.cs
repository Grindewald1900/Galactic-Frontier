using UnityEngine;
using TMPro;

public class HintTextController : MonoBehaviour
{
    public static HintTextController Instance;
    public TextMeshProUGUI hintText;
    public float displayDuration = 2f;
    public float floatAmplitude = 10f;
    public float floatSpeed = 1f;
    private Vector3 initialPosition;
    private float timer = 0f;
    private bool isShowing = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }
    void Start()
    {
        initialPosition = hintText.transform.localPosition;
        HideHint();
    }

    void Update()
    {
        if (isShowing)
        {
            timer += Time.deltaTime;
            float newY = initialPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            hintText.transform.localPosition = new Vector3(initialPosition.x, newY, initialPosition.z);

            if (timer >= displayDuration)
            {
                HideHint();
            }
        }
    }


    public void ShowHint(string message)
    {
        if (isShowing) return;
        hintText.text = message;
        hintText.gameObject.SetActive(true);
        isShowing = true;
        timer = 0f;
    }

    public void HideHint()
    {
        hintText.gameObject.SetActive(false);
        isShowing = false;
        hintText.transform.localPosition = initialPosition;
    }
}
