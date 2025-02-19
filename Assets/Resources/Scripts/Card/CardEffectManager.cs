using UnityEngine;
using UnityEngine.UI;
using CartoonFX;
using System.Collections.Generic;
using DG.Tweening;
public class CardEffectManager : MonoBehaviour
{
    public Camera mainCamera;
    private Outline outlineComponent;
    public static CardEffectManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnHitEffect(Card card, Vector2 offset)
    {
        Debug.Log("SpawnHitEffect");
        GameObject prefab = GetEffect();
        float scale = 0.5f;
        prefab.transform.localScale = new Vector3(scale, scale, scale);
        //TODO: 需要根据offset调整位置
        Vector3 camPos = mainCamera.transform.position;
        Vector3 cardPos = card.transform.position + new Vector3(0, -2, 0);
        GameObject effect = Instantiate(prefab, card.transform.position + new Vector3(0, 0, -10), Quaternion.identity);
        Vector3 effectPos = PositionUtil.GetEffectPosition(camPos, cardPos, effect.transform.position, 0.5f);
        effect.transform.localPosition = effectPos;
        Destroy(effect, 1.5f);
    }

    private GameObject GetEffect()
    {
        List<GameObject> effects = new List<GameObject>();
        effects.Add(Resources.Load<GameObject>("Prefabs/Effect/Bleeding"));
        effects.Add(Resources.Load<GameObject>("Prefabs/Effect/Burning"));
        effects.Add(Resources.Load<GameObject>("Prefabs/Effect/Electrified"));
        effects.Add(Resources.Load<GameObject>("Prefabs/Effect/Explosion"));
        effects.Add(Resources.Load<GameObject>("Prefabs/Effect/Frozen"));
        effects.Add(Resources.Load<GameObject>("Prefabs/Effect/Impact"));
        effects.Add(Resources.Load<GameObject>("Prefabs/Effect/Poisoned"));
        effects.Add(Resources.Load<GameObject>("Prefabs/Effect/Radiated"));

        return effects[Random.Range(0, effects.Count)];
    }
}