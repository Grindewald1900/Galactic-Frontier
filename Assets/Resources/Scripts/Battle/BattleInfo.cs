using UnityEngine;
using TMPro;
using DG.Tweening;

// Information shows up at center of screen, e.g. "Battle Start" "Round 1"
public class BattleInfo : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI battleInfoText; // Reference to UI text
    [SerializeField] private float animationDuration = 1.0f;
    public bool isBattleInfoActive = false;
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

    public void PlayBattleInfoAnimation(string info)
    {
        if (battleInfoText == null) return;
        if (isBattleInfoActive) return;
        isBattleInfoActive = true;
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
                        isBattleInfoActive = false;
                    });
            });
    }
}
