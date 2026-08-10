using System.Collections;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Main;
using Assets.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Figma-style chrome for BattleScene under Option A: keep Unity auto-combat,
    /// add NEXUS frame, round HUD, battle log strip, and return navigation.
    /// </summary>
    public sealed class BattleChrome : MonoBehaviour
    {
        private TextMeshProUGUI roundLabel;
        private TextMeshProUGUI modeLabel;
        private TextMeshProUGUI logLabel;
        private int lastRound = -1;

        private IEnumerator Start()
        {
            LocalizationUtil.Initialize();
            yield return null;
            Build();
            StartCoroutine(PollRound());
        }

        private void Build()
        {
            Canvas chrome = NexusUiFactory.CreateCanvas("Battle Chrome", 120, true);
            chrome.transform.SetParent(transform, false);

            NexusUiFactory.CreatePanel(
                chrome.transform,
                "Top Bar",
                NexusTheme.Surface,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -NexusTheme.TopBarHeight),
                Vector2.zero,
                true);

            NexusUiFactory.CreatePanel(
                chrome.transform,
                "Status Bar",
                NexusTheme.Surface,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                Vector2.zero,
                new Vector2(0f, NexusTheme.StatusBarHeight),
                true);

            GameObject top = chrome.transform.Find("Top Bar").gameObject;
            int region = PlayerPrefs.GetInt("nexus_last_region", 2);
            NexusUiFactory.CreateText(
                top.transform,
                "Brand",
                UiText.BattleBreadcrumb,
                new Vector2(18f, 8f),
                new Vector2(420f, 20f),
                11f,
                NexusTheme.Gold,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            NexusUiFactory.CreateText(
                top.transform,
                "Region",
                UiText.SectorName(Mathf.Clamp(region, 0, 4)),
                new Vector2(18f, 26f),
                new Vector2(420f, 18f),
                12f,
                NexusTheme.MutedText);

            roundLabel = NexusUiFactory.CreateText(
                top.transform,
                "Round",
                UiText.Turn(0),
                new Vector2(860f, 8f),
                new Vector2(200f, 32f),
                22f,
                NexusTheme.Gold,
                TextAlignmentOptions.Center,
                FontStyles.Bold);

            modeLabel = NexusUiFactory.CreateText(
                top.transform,
                "Mode",
                UiText.AutoBattle,
                new Vector2(1100f, 14f),
                new Vector2(200f, 22f),
                12f,
                NexusTheme.Cyan,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            NexusUiFactory.CreateIcon(
                top.transform,
                "BattleIcon",
                NexusCardVisual.UiIcon("Battle"),
                new Vector2(1310f, 8f),
                new Vector2(32f, 32f),
                NexusTheme.Gold);

            NexusUiFactory.CreateButton(
                top.transform,
                "Pause Return",
                UiText.ReturnToBridge,
                new Vector2(1680f, 8f),
                new Vector2(200f, 32f),
                ReturnToBridge,
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.14f),
                NexusTheme.Gold,
                13f);

            GameObject status = chrome.transform.Find("Status Bar").gameObject;
            NexusUiFactory.CreateText(
                status.transform,
                "Hints",
                UiText.BattleStatusHint,
                new Vector2(16f, 2f),
                new Vector2(1400f, 18f),
                10f,
                NexusTheme.DimText);

            GameObject log = NexusUiFactory.CreatePanel(
                chrome.transform,
                "Battle Log",
                NexusTheme.WithAlpha(NexusTheme.Surface, 0.92f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(-220f, NexusTheme.StatusBarHeight + 8f),
                new Vector2(-8f, -(NexusTheme.TopBarHeight + 8f)),
                false);
            var outline = log.AddComponent<Outline>();
            outline.effectColor = NexusTheme.BorderSoft;
            outline.effectDistance = new Vector2(1f, -1f);

            NexusUiFactory.CreateText(
                log.transform,
                "Header",
                UiText.BattleLogHeader,
                new Vector2(12f, 10f),
                new Vector2(180f, 22f),
                11f,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            logLabel = NexusUiFactory.CreateText(
                log.transform,
                "Body",
                UiText.BattleLogStart,
                new Vector2(12f, 40f),
                new Vector2(190f, 800f),
                11f,
                NexusTheme.Text);
            logLabel.textWrappingMode = TextWrappingModes.Normal;
            logLabel.overflowMode = TextOverflowModes.Truncate;

            StyleBattleInfoIfPresent();
        }

        private static void StyleBattleInfoIfPresent()
        {
            if (BattleInfo.Instance == null)
                return;

            foreach (TextMeshProUGUI text in BattleInfo.Instance.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.font = NexusUiFactory.GetRuntimeFont();
                text.color = NexusTheme.Gold;
            }
        }

        private IEnumerator PollRound()
        {
            while (enabled)
            {
                int round = BattleController.Instance != null ? BattleController.Instance.CurrentRound : 0;
                if (round != lastRound && round > 0)
                {
                    lastRound = round;
                    if (roundLabel != null)
                        roundLabel.text = UiText.Turn(round);
                    AppendLog(UiText.BattleLogRound(round));
                }

                if (GameStatusManager.Instance != null && !GameStatusManager.Instance.IsBattle && lastRound > 0)
                {
                    if (modeLabel != null)
                        modeLabel.text = UiText.BattleEnd;
                    AppendLog(UiText.BattleLogEnded);
                    yield break;
                }

                yield return new WaitForSeconds(0.25f);
            }
        }

        private void AppendLog(string line)
        {
            if (logLabel == null) return;
            logLabel.text = $"{logLabel.text}\n{line}";
        }

        private static void ReturnToBridge()
        {
            BattleSceneExit.ReturnToBridge();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ReturnToBridge();
        }
    }
}
