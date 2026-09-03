using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus.Tutorial
{
    public enum TutorialGuideTargetKind
    {
        NavGroup = 0,
        NavScreen = 1,
        UiAnchor = 2
    }

    public sealed class TutorialGuideDef
    {
        public string guideId;
        public string chain;
        public string stepId;
        public string requiredScreen;
        public string hideOnScreen;
        public TutorialGuideTargetKind targetKind;
        public string targetKey;
        public string dialogueId;
        public int order;
    }

    public sealed class TutorialGuideAnchor
    {
        public RectTransform Rect;
        public Button Button;
    }
}
