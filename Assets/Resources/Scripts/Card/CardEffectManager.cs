using UnityEngine;
using CartoonFX;
using System.Collections.Generic;
public class CardEffectManager : MonoBehaviour
{
    public Camera mainCamera;

    public static CardEffectManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnHitEffect(Card card, Vector2 offset)
    {
        Debug.Log("SpawnHitEffect");
        GameObject prefab = GetEffect();
        if (prefab == null)
        {
            Debug.Log("GetEffect is null");
        }
        else
        {
            Debug.Log("GetEffect is not null");
        }
        float scale = 0.5f;
        prefab.transform.localScale = new Vector3(scale, scale, scale);
        //TODO: 需要根据offset调整位置
        Vector3 camPos = mainCamera.transform.position;
        Vector3 cardPos = card.transform.position + new Vector3(0, -2, 0);
        GameObject effect = Instantiate(prefab, card.transform.position + new Vector3(0, 0, -10), Quaternion.identity);
        Vector3 effectPos = PositionUtil.GetEffectPosition(camPos, cardPos, effect.transform.position, 0.5f);
        effect.transform.localPosition = effectPos;
        Destroy(effect, 3f);
        // {
        //     var ps = effect.GetComponent<ParticleSystem>();
        //     if (ps.isEmitting)
        //     {
        //         ps.Stop(true);
        //     }
        //     else
        //     {
        //         if (!effect.gameObject.activeSelf)
        //         {
        //             effect.SetActive(true);
        //         }
        //         else
        //         {
        //             ps.Play(true);
        //             var cfxrEffects = effect.GetComponentsInChildren<CFXR_Effect>();
        //             foreach (var cfxr in cfxrEffects)
        //             {
        //                 cfxr.ResetState();
        //             }
        //         }
        //     }
        // }
    }

    private GameObject GetEffect()
    {
        List<GameObject> effects = new List<GameObject>();
        effects.Add(Resources.Load<GameObject>("Prefabs/Effect/Burning"));
        effects.Add(Resources.Load<GameObject>("Prefabs/Effect/Explosion"));
        effects.Add(Resources.Load<GameObject>("Prefabs/Effect/Poisoned"));
        effects.Add(Resources.Load<GameObject>("Prefabs/Effect/Impact"));
        return effects[Random.Range(0, effects.Count)];
    }
}