using UnityEngine;

namespace Assets.Resources.Scripts.Battle
{
    /// <summary>
    /// Keeps the fixed-size world-space battle canvas fully in view at any aspect ratio and grows the
    /// backdrop to cover the whole viewport. The battle camera does not clear, so any viewport area the
    /// backdrop leaves uncovered shows stale pixels.
    /// </summary>
    public class BattleViewportFit : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [Tooltip("World-space canvas whose full rect must stay visible.")]
        [SerializeField] private RectTransform content;
        [Tooltip("Decorative background inside the canvas, scaled up to cover the viewport.")]
        [SerializeField] private RectTransform backdrop;
        [Tooltip("Extra margin around the canvas, in canvas units.")]
        [SerializeField, Min(0f)] private float contentMargin;
        [SerializeField, Range(30f, 120f)] private float maxFieldOfView = 90f;

        private int lastWidth;
        private int lastHeight;
        private Vector3 backdropBaseScale;
        private bool captured;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (Screen.width == lastWidth && Screen.height == lastHeight) return;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            Apply();
        }

        private void Apply()
        {
            if (targetCamera == null || content == null || lastHeight <= 0) return;
            if (!captured && backdrop != null)
            {
                backdropBaseScale = backdrop.localScale;
                captured = true;
            }

            float unit = content.lossyScale.x;
            float width = (content.rect.width + 2f * contentMargin) * unit;
            float height = (content.rect.height + 2f * contentMargin) * unit;
            float aspect = targetCamera.aspect;
            float halfHeight = Mathf.Max(height * 0.5f, width * 0.5f / aspect);

            float visibleHeight;
            if (targetCamera.orthographic)
            {
                targetCamera.orthographicSize = halfHeight;
                visibleHeight = 2f * halfHeight;
            }
            else
            {
                Transform cam = targetCamera.transform;
                float distance = Mathf.Abs(Vector3.Dot(content.position - cam.position, cam.forward));
                if (distance <= 0.01f) return;
                float fov = Mathf.Min(maxFieldOfView, 2f * Mathf.Atan(halfHeight / distance) * Mathf.Rad2Deg);
                targetCamera.fieldOfView = fov;
                visibleHeight = 2f * distance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            }

            if (backdrop == null) return;
            Vector2 baseSize = Vector2.Scale(backdrop.rect.size, backdropBaseScale) * unit;
            if (baseSize.x <= 0f || baseSize.y <= 0f) return;
            float cover = Mathf.Max(1f, Mathf.Max(visibleHeight * aspect / baseSize.x, visibleHeight / baseSize.y));
            backdrop.localScale = backdropBaseScale * cover;
        }
    }
}
