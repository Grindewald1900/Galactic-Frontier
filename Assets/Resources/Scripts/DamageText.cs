using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;

public class DamageText : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float floatSpeed = 50f;     // 上浮速度
    public float duration = 1f;        // 持续时间（秒）
    public float fadeSpeed = 2f;       // 淡出速度
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Color criticalColor;
    private float criticalScaleMultiplier = 3f;


    void Start()
    {
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
        }
        originalPosition = transform.position;
        originalScale = transform.localScale;
        gameObject.SetActive(false);
    }

    public void SetDamageText(string damageText, Color textColor, float scale = 1f)
    {
        gameObject.SetActive(true);
        transform.position = originalPosition;
        textMesh.color = textColor;
        transform.localScale = originalScale;
        criticalScaleMultiplier = scale;

        if (textMesh != null)
        {
            textMesh.text = damageText;
            PlayDamageAnimation();
        }
    }

    private void PlayDamageAnimation()
    {
        // 获取当前的初始 scale
        Vector3 startScale = transform.localScale;
        // 计算目标 scale：如果暴击，则放大 criticalScaleMultiplier 倍，否则保持不变
        Vector3 targetScale = startScale * criticalScaleMultiplier;
        // 计算上浮目标位置：移动距离 = floatSpeed * duration
        Vector3 targetPosition = transform.position + Vector3.up * floatSpeed * duration;

        // 创建 DOTween 序列
        Sequence seq = DOTween.Sequence();

        // 1. 上浮动画（整个 duration 内进行线性移动）
        seq.Join(transform.DOMove(targetPosition, duration).SetEase(Ease.Linear));

        // 2. 如果暴击，则在整个持续时间内进行缩放过渡
        // if (isCritical)
        // {
        seq.Join(transform.DOScale(targetScale, duration).SetEase(Ease.OutQuad));
        // }

        // 淡出动画：使用 DOTween.To 来 tween 颜色 alpha，从1降到0，延迟 duration * 0.5 秒开始
        seq.Join(
            DOTween.To(() => textMesh.color.a,
                       x => textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, x),
                       0f, duration * 0.5f)
                   .SetDelay(duration * 0.5f)
        );
        // 4. 动画结束后，将 GameObject 隐藏或销毁
        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }

}
