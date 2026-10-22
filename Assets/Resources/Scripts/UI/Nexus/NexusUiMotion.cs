using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Light press scale and a short appear scale. No extra animation packages.</summary>
    internal sealed class NexusUiMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool Press = true;
        public bool Appear;

        Vector3 baseScale = Vector3.one;
        float appearTime;

        void OnEnable()
        {
            baseScale = transform.localScale;
            if (baseScale.sqrMagnitude < 0.001f)
                baseScale = Vector3.one;
            appearTime = 0f;
            if (Appear)
                transform.localScale = baseScale * 0.94f;
        }

        void Update()
        {
            if (!Appear || appearTime >= 0.16f)
                return;

            appearTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(appearTime / 0.16f);
            transform.localScale = baseScale * Mathf.Lerp(0.94f, 1f, t);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Press)
                return;
            transform.localScale = baseScale * 0.96f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!Press)
                return;
            transform.localScale = baseScale;
        }
    }
}
