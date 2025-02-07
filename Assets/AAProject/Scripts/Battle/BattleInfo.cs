using UnityEngine;
using TMPro;
using DG.Tweening;

public class BattleInfo : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI battleInfoText; // Reference to UI text
    [SerializeField] private float animationDuration = 1.0f;
    private Vector3 originalScale;
    public static BattleInfo Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (battleInfoText != null)
        {
            originalScale = battleInfoText.transform.localScale;
            battleInfoText.gameObject.SetActive(false);
        }
    }

    public void PlayBattleStartAnimation(string info)
    {
        if (battleInfoText == null) return;
        battleInfoText.text = info;
        battleInfoText.gameObject.SetActive(true);
        battleInfoText.transform.localScale = Vector3.zero;

        // Scale up with bounce
        battleInfoText.transform.DOScale(originalScale, animationDuration)
            .SetEase(Ease.OutBounce)
            .OnComplete(() =>
            {
                // Wait and scale down
                battleInfoText.transform.DOScale(Vector3.zero, animationDuration * 0.5f)
                    .SetEase(Ease.InBack)
                    .SetDelay(0.5f)
                    .OnComplete(() =>
                    {
                        battleInfoText.gameObject.SetActive(false);
                    });
            });
    }
}
