using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Progression.Domain;
using Assets.Resources.Scripts.Utils;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Shared XP bar / deck-power helpers for shell and card inspect panels.</summary>
    internal static class NexusProgressUi
    {
        public sealed class XpBar
        {
            public TextMeshProUGUI Label;
            public RectTransform FillRect;
            public Image Fill;
            public Image Track;
        }

        public static int ActiveCombatPower()
        {
            var cards = CardListManager.Instance?.cardEntities;
            if (cards == null) return 0;
            if (DataUtil.Instance != null)
                DeckService.EnsureLoaded(DataUtil.Instance, cards);
            var members = DeckService.GetActiveCombatMembers(cards);
            var power = 0;
            if (members == null) return 0;
            foreach (var member in members)
            {
                if (member != null)
                    power += Mathf.RoundToInt(member.power);
            }

            return power;
        }

        public static float CommanderXpRatio(PlayerEntity player, out float current, out float needed)
        {
            current = 0f;
            needed = 1f;
            if (player == null) return 0f;
            var level = Mathf.Max(1, player.level);
            current = Mathf.Max(0f, player.commanderExp);
            needed = player.commanderExpToNext > 0f
                ? player.commanderExpToNext
                : ProgressionRules.CommanderExpToNext(level);
            if (needed <= 0f) needed = 1f;
            return Mathf.Clamp01(current / needed);
        }

        public static float CombatXpRatio(CardEntity card, out float current, out float needed, out float stored)
        {
            current = 0f;
            needed = 1f;
            stored = 0f;
            if (card == null) return 0f;
            card.EnsureProgressionDefaults();
            current = Mathf.Max(0f, card.CurrentExp);
            needed = card.ExpToNextLevel > 0f
                ? card.ExpToNextLevel
                : ProgressionRules.CombatExpToNext(card.Level);
            stored = Mathf.Max(0f, card.storedCombatXp);
            if (needed <= 0f) needed = 1f;
            return Mathf.Clamp01(current / needed);
        }

        public static float ProfessionXpRatio(CardEntity card, ProfessionSkill skill, out float current, out float needed, out float stored)
        {
            current = 0f;
            needed = 1f;
            stored = 0f;
            if (card == null || skill == ProfessionSkill.None) return 0f;
            card.EnsureProgressionDefaults();
            current = Mathf.Max(0f, card.GetProfessionXp(skill));
            needed = ProgressionRules.ProfessionExpToNext(card.GetProfessionLevel(skill));
            stored = Mathf.Max(0f, card.GetProfessionStoredXp(skill));
            if (needed <= 0f) needed = 1f;
            return Mathf.Clamp01(current / needed);
        }

        public static XpBar DrawBar(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            string label,
            Color fillColor,
            float ratio)
        {
            return DrawBarInternal(
                parent, name, position, size, label, fillColor, ratio,
                labelBeside: false, labelWidth: 0f);
        }

        /// <summary>Skill info on the left, fill bar on the right (card detail rows).</summary>
        public static XpBar DrawInlineBar(
            Transform parent,
            string name,
            Vector2 position,
            float totalWidth,
            float barHeight,
            float labelWidth,
            string label,
            Color fillColor,
            float ratio)
        {
            return DrawBarInternal(
                parent, name, position,
                new Vector2(totalWidth, barHeight),
                label, fillColor, ratio,
                labelBeside: true, labelWidth: labelWidth);
        }

        private static XpBar DrawBarInternal(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            string label,
            Color fillColor,
            float ratio,
            bool labelBeside,
            float labelWidth)
        {
            ratio = Mathf.Clamp01(ratio);
            float trackX = position.x;
            float trackW = size.x;
            float labelH = labelBeside ? Mathf.Max(16f, size.y) : 16f;

            if (labelBeside)
            {
                labelWidth = Mathf.Clamp(labelWidth, 80f, size.x - 40f);
                trackX = position.x + labelWidth + 8f;
                trackW = Mathf.Max(24f, size.x - labelWidth - 8f);
            }

            var track = NexusUiFactory.CreateBox(
                parent, name + "Track",
                new Vector2(trackX, position.y + (labelBeside ? (labelH - size.y) * 0.5f : 0f)),
                new Vector2(trackW, size.y),
                NexusTheme.WithAlpha(NexusTheme.Border, 0.55f));
            var fillGo = new GameObject(name + "Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(track.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(ratio, 1f);
            fillRect.offsetMin = new Vector2(1f, 1f);
            fillRect.offsetMax = new Vector2(ratio >= 0.999f ? -1f : 0f, -1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            var fill = fillGo.GetComponent<Image>();
            fill.color = fillColor;
            fill.raycastTarget = false;
            fill.type = Image.Type.Simple;

            Vector2 labelPos = labelBeside
                ? position
                : new Vector2(position.x, position.y + size.y + 1f);
            Vector2 labelSize = labelBeside
                ? new Vector2(labelWidth, labelH)
                : new Vector2(size.x, 16f);

            var labelText = NexusUiFactory.CreateText(
                parent,
                name + "Label",
                label,
                labelPos,
                labelSize,
                labelBeside ? 11f : 11f,
                NexusTheme.Text,
                labelBeside ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Left);
            labelText.overflowMode = TextOverflowModes.Ellipsis;

            return new XpBar
            {
                Label = labelText,
                FillRect = fillRect,
                Fill = fill,
                Track = track.GetComponent<Image>()
            };
        }

        public static void ApplyBar(XpBar bar, string label, float ratio)
        {
            if (bar == null) return;
            ratio = Mathf.Clamp01(ratio);
            if (bar.Label != null)
                bar.Label.text = label ?? "";
            if (bar.FillRect != null)
            {
                bar.FillRect.anchorMax = new Vector2(ratio, 1f);
                bar.FillRect.offsetMax = new Vector2(ratio >= 0.999f ? -1f : 0f, -1f);
            }
            else if (bar.Fill != null)
            {
                bar.Fill.type = Image.Type.Filled;
                bar.Fill.fillAmount = ratio;
            }
        }

        public static string FormatXpLabel(string title, int level, float current, float needed, float stored = 0f)
        {
            if (stored > 0f)
                return $"{title}  Lv.{level}  {current:0}/{needed:0}  +{stored:0}";
            return $"{title}  Lv.{level}  {current:0}/{needed:0}";
        }

        public static string FormatXpLabelWithSource(
            string title, int level, float current, float needed, string source, float stored = 0f)
        {
            string core = FormatXpLabel(title, level, current, needed, stored);
            if (string.IsNullOrEmpty(source))
                return core;
            return $"{core}  ·  {source}";
        }

        public static string FormatCommanderXpLabel(int level, float current, float needed) =>
            UiText.CommanderLvXp(level, current, needed);

        public static float DrawCardXpStack(
            Transform parent,
            CardEntity card,
            Vector2 origin,
            float width,
            float rowGap = 22f,
            List<XpBar> into = null)
        {
            if (card == null) return 0f;
            into?.Clear();
            float y = origin.y;
            float labelWidth = Mathf.Clamp(width * 0.58f, 160f, 280f);
            const float barH = 10f;

            CombatXpRatio(card, out var combatCur, out var combatNeed, out var combatStored);
            var combatBar = DrawInlineBar(
                parent, "CombatXp",
                new Vector2(origin.x, y),
                width, barH, labelWidth,
                FormatXpLabelWithSource(
                    UiText.CombatLevelLabel,
                    card.Level,
                    combatCur,
                    combatNeed,
                    UiText.GrowthSourceCombat,
                    combatStored),
                NexusTheme.Gold,
                Mathf.Clamp01(combatCur / Mathf.Max(1f, combatNeed)));
            into?.Add(combatBar);
            y += rowGap;

            foreach (var skill in new[]
            {
                ProfessionSkill.Gather,
                ProfessionSkill.Craft,
                ProfessionSkill.Scan,
                ProfessionSkill.Navigate,
                ProfessionSkill.Logistics
            })
            {
                ProfessionXpRatio(card, skill, out var cur, out var need, out var stored);
                var bar = DrawInlineBar(
                    parent, skill + "Xp",
                    new Vector2(origin.x, y),
                    width, barH, labelWidth,
                    FormatXpLabelWithSource(
                        UiText.ProfessionSkillFull(skill.ToString()),
                        card.GetProfessionLevel(skill),
                        cur,
                        need,
                        UiText.GrowthSource(skill.ToString()),
                        stored),
                    SkillColor(skill),
                    Mathf.Clamp01(cur / Mathf.Max(1f, need)));
                into?.Add(bar);
                y += rowGap;
            }

            return y - origin.y;
        }

        public static void ApplyCardXpBars(CardEntity card, IList<XpBar> bars)
        {
            if (card == null || bars == null || bars.Count == 0)
                return;

            int i = 0;
            CombatXpRatio(card, out var combatCur, out var combatNeed, out var combatStored);
            if (i < bars.Count)
            {
                ApplyBar(
                    bars[i],
                    FormatXpLabelWithSource(
                        UiText.CombatLevelLabel,
                        card.Level,
                        combatCur,
                        combatNeed,
                        UiText.GrowthSourceCombat,
                        combatStored),
                    Mathf.Clamp01(combatCur / Mathf.Max(1f, combatNeed)));
                i++;
            }

            foreach (var skill in new[]
            {
                ProfessionSkill.Gather,
                ProfessionSkill.Craft,
                ProfessionSkill.Scan,
                ProfessionSkill.Navigate,
                ProfessionSkill.Logistics
            })
            {
                if (i >= bars.Count) break;
                ProfessionXpRatio(card, skill, out var cur, out var need, out var stored);
                ApplyBar(
                    bars[i],
                    FormatXpLabelWithSource(
                        UiText.ProfessionSkillFull(skill.ToString()),
                        card.GetProfessionLevel(skill),
                        cur,
                        need,
                        UiText.GrowthSource(skill.ToString()),
                        stored),
                    Mathf.Clamp01(cur / Mathf.Max(1f, need)));
                i++;
            }
        }

        private static Color SkillColor(ProfessionSkill skill) => skill switch
        {
            ProfessionSkill.Gather => NexusTheme.Green,
            ProfessionSkill.Craft => NexusTheme.Gold,
            ProfessionSkill.Scan => NexusTheme.Cyan,
            ProfessionSkill.Navigate => NexusTheme.Purple,
            ProfessionSkill.Logistics => NexusTheme.DimText,
            _ => NexusTheme.MutedText
        };
    }
}
