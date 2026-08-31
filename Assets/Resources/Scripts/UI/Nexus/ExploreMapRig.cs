using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Google Maps-style smooth zoom/pan. Zooming out of a sector crosses into the universe map;
    /// returning to a sector requires Enter Sector, not zoom-in.
    /// </summary>
    internal sealed class ExploreMapRig : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler
    {
        public const float MinZoom = 0.35f;
        public const float MaxZoom = 3.4f;
        public const float LodUniverse = 0.82f;
        public const float SectorEnterZoom = 1.35f;
        public const float ZoomStep = 1.22f;
        public const string HostName = "ExploreMapHost";

        public ExploreScreen Screen;
        public RectTransform Viewport;
        public RectTransform Content;
        public TextMeshProUGUI ZoomLabel;

        public float Zoom = 1.15f;
        public float TargetZoom = 1.15f;
        public Vector2 Pan;
        public Vector2 TargetPan;

        /// <summary>Once the universe layer is shown, stay there until <see cref="OpenSectorView"/>.</summary>
        public bool UniverseLocked { get; private set; }

        private bool lastUniverse;
        private bool lodReady;

        public bool IsUniverseLod => UniverseLocked || Zoom <= LodUniverse;

        public void Snap(float zoom, Vector2 pan)
        {
            Zoom = TargetZoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            Pan = TargetPan = pan;
            Apply();
        }

        public void OpenSectorView(float zoom, Vector2 pan)
        {
            UniverseLocked = false;
            lastUniverse = false;
            lodReady = true;
            Snap(Mathf.Max(zoom, LodUniverse + 0.08f), pan);
        }

        public void NudgeZoom(float factor, Vector2 cursorInViewport)
        {
            float old = Mathf.Max(0.01f, TargetZoom);
            float next = Mathf.Clamp(old * factor, MinZoom, MaxZoom);
            TargetPan = cursorInViewport - (cursorInViewport - TargetPan) * (next / old);
            TargetZoom = next;
        }

        public void NudgeZoomCentered(float factor)
        {
            Vector2 center = Viewport == null
                ? Vector2.zero
                : new Vector2(Viewport.rect.width * 0.5f, Viewport.rect.height * 0.5f);
            NudgeZoom(factor, center);
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (eventData == null || Viewport == null) return;
            float wheel = eventData.scrollDelta.y;
            if (Mathf.Abs(wheel) < 0.01f) return;
            if (!ScreenToViewport(eventData, out var cursor))
                cursor = new Vector2(Viewport.rect.width * 0.5f, Viewport.rect.height * 0.5f);
            NudgeZoom(wheel > 0f ? ZoomStep : 1f / ZoomStep, cursor);
        }

        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData == null) return;
            float scale = Mathf.Max(0.01f, transform.lossyScale.x);
            TargetPan += new Vector2(eventData.delta.x, -eventData.delta.y) / scale;
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            float k = 1f - Mathf.Exp(-13f * dt);
            bool wasUniverse = IsUniverseLod;
            Zoom = Mathf.Lerp(Zoom, TargetZoom, k);
            Pan = Vector2.Lerp(Pan, TargetPan, k);
            if (Mathf.Abs(Zoom - TargetZoom) < 0.001f)
                Zoom = TargetZoom;
            Apply();

            if (!UniverseLocked && Zoom <= LodUniverse)
                UniverseLocked = true;

            bool universe = IsUniverseLod;
            if (!lodReady)
            {
                lastUniverse = universe;
                lodReady = true;
            }
            else if (universe != lastUniverse)
            {
                lastUniverse = universe;
                Screen?.OnMapLodChanged(universe);
            }

            if (ZoomLabel != null)
                ZoomLabel.text = UiText.ExploreZoomScale(Zoom, universe);
        }

        private void Apply()
        {
            if (Content == null) return;
            Content.localScale = new Vector3(Zoom, Zoom, 1f);
            Content.anchoredPosition = new Vector2(Pan.x, -Pan.y);
        }

        private bool ScreenToViewport(PointerEventData eventData, out Vector2 cursor)
        {
            cursor = Vector2.zero;
            if (Viewport == null) return false;
            Camera cam = eventData.pressEventCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    Viewport, eventData.position, cam, out var local))
                return false;
            // Viewport uses top-left pivot: local.y is negative downward.
            cursor = new Vector2(local.x, -local.y);
            return true;
        }
    }
}
