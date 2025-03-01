using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;

namespace Assets.Resources.Scripts.Cards
{
    public class CardEffectManager : MonoBehaviour
    {
        public Camera mainCamera;
        public static CardEffectManager Instance;
        private const float scale = 0.5f;

        private void Awake()
        {
            Instance = this;
        }

        public void SpawnHitEffect(Card card)
        {
            Debug.Log("SpawnHitEffect");
            GameObject prefab = GetEffect();
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
            List<GameObject> effects = new()
            {
                UnityEngine.Resources.Load<GameObject>("Prefabs/Effect/Bleeding"),
                UnityEngine.Resources.Load<GameObject>("Prefabs/Effect/Burning"),
                UnityEngine.Resources.Load<GameObject>("Prefabs/Effect/Electrified"),
                UnityEngine.Resources.Load<GameObject>("Prefabs/Effect/Explosion"),
                UnityEngine.Resources.Load<GameObject>("Prefabs/Effect/Frozen"),
                UnityEngine.Resources.Load<GameObject>("Prefabs/Effect/Impact"),
                UnityEngine.Resources.Load<GameObject>("Prefabs/Effect/Poisoned"),
                UnityEngine.Resources.Load<GameObject>("Prefabs/Effect/Radiated")
            };

            return effects[UnityEngine.Random.Range(0, effects.Count)];
        }
    }
}