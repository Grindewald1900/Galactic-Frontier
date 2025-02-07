using UnityEngine;
using TMPro;
using System.Collections;

public class DamageText : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float floatSpeed = 50f;     // 上浮速度
    public float duration = 1f;        // 持续时间（秒）
    public float fadeSpeed = 2f;       // 淡出速度

    private Color originalColor;
    private Vector3 originalPosition;

    void Start()

    {
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
        }
        originalColor = textMesh.color;
        originalPosition = transform.position;
        gameObject.SetActive(false);
    }



    public void SetDamageValue(int damage)
    {
        gameObject.SetActive(true);
        transform.position = originalPosition;
        if (textMesh != null)
        {
            textMesh.text = $"-{damage}";
            StartCoroutine(FloatUpAndFade());
        }
    }


    private IEnumerator FloatUpAndFade()
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;

            // 上浮
            transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);

            // 在后半段开始淡出
            if (timer > duration * 0.5f)
            {
                float alpha = Mathf.Lerp(1f, 0f, (timer - duration * 0.5f) * fadeSpeed);
                textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }
            yield return null;
        }
        gameObject.SetActive(false);

    }
}
