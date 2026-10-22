using System;
using System.Collections.Generic;
using System.Text;
using Assets.Resources.Scripts.Battle.Domain;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.UI.Nexus;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.Battle
{
    /// <summary>
    /// Bounded record of the presented fight for the BattleChrome side log.
    /// Lines are TMP rich text written at the real combat event points; player units are cyan, enemies red.
    /// The headless background simulation does not write here.
    /// </summary>
    public static class BattleLog
    {
        public const int MaxEntries = 200;

        private const string PlayerHex = "#38BDF8";
        private const string EnemyHex = "#F87171";
        private const string GoldHex = "#E8A832";
        private const string MutedHex = "#94A3B8";
        private const string CritHex = "#FBBF24";
        private const string StatusHex = "#A78BFA";
        private const string VictoryHex = "#34D399";

        private static readonly List<string> entries = new List<string>();
        private static Card currentActor;
        private static bool currentSpecial;

        public static IReadOnlyList<string> Entries => entries;

        /// <summary>Raised after a line is stored. BattleChrome redraws from <see cref="Entries"/>.</summary>
        public static event Action<string> Appended;

        public static event Action Cleared;

        public static void Clear()
        {
            entries.Clear();
            currentActor = null;
            Cleared?.Invoke();
        }

        public static void Add(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            entries.Add(line);
            if (entries.Count > MaxEntries)
                entries.RemoveRange(0, entries.Count - MaxEntries);
            Appended?.Invoke(line);
        }

        public static void BattleStarted(string encounterName, int players, int enemies)
        {
            Add(Color(GoldHex, UiText.T(
                $"Battle start: {encounterName} ({players} vs {enemies})",
                $"战斗开始：{encounterName}（我方 {players} vs 敌方 {enemies}）")));
        }

        public static void BattleResumed(int round)
        {
            Add(Color(MutedHex, UiText.T(
                $"Resumed live battle at round {round}. Rounds simulated off-screen are not itemized.",
                $"接续进行中的战斗（第 {round} 回合）。离开期间后台结算的回合不逐条记录。")));
        }

        public static void Round(int round)
        {
            Add(Color(GoldHex, UiText.BattleLogRound(round)));
        }

        /// <summary>Marks who is acting so the following hits are tagged normal attack or skill.</summary>
        public static void BeginAction(Card actor, bool special)
        {
            currentActor = actor;
            currentSpecial = special;
            if (special)
                Add(UiText.T(
                    $"{Unit(actor)} is at full energy and casts a skill",
                    $"{Unit(actor)} 能量已满，释放技能"));
        }

        public static void EndAction()
        {
            currentActor = null;
        }

        /// <summary>One line per attacker → target with every rolled hit, crit, and miss.</summary>
        public static void Hits(Card attacker, Card target, List<DamageEntity> damage)
        {
            if (target == null || damage == null) return;
            var parts = new List<string>();
            int total = 0;
            foreach (var d in damage)
            {
                if (d == null) continue;
                // Skill kits append a cosmetic 0-damage MISS popup; only rolled misses count.
                if (d.damageType == DamageType.MISS && !d.missed) continue;
                if (d.missed)
                {
                    parts.Add(Color(MutedHex, UiText.T("miss", "未命中")));
                    continue;
                }

                int amount = Mathf.RoundToInt(d.damageAmount);
                total += amount;
                parts.Add(d.criticalMultiplier > 1f
                    ? Color(CritHex, UiText.T($"{amount} crit", $"{amount} 暴击"))
                    : amount.ToString());
            }

            if (parts.Count == 0) return;
            bool skill = attacker != null && attacker == currentActor && currentSpecial;
            string tag = skill ? UiText.T("[Skill]", "[技能]") : UiText.T("[Attack]", "[普攻]");
            var sb = new StringBuilder();
            sb.Append(Unit(attacker)).Append(' ').Append(tag).Append(" → ").Append(Unit(target)).Append(": ");
            sb.Append(string.Join(UiText.T(", ", "、"), parts));
            if (total > 0)
                sb.Append(UiText.T($" = {total} dmg", $"，共 {total} 伤害"));
            Add(sb.ToString());
        }

        /// <summary>
        /// First application of a status only; re-applications just stack rounds and would flood the log.
        /// Status damage/control is not resolved per round by the current combat loop, so no tick numbers are shown.
        /// </summary>
        public static void StatusApplied(Card target, DebuffEntity debuff)
        {
            if (target == null || debuff == null) return;
            string status = StatusName(debuff);
            Add("  " + Color(StatusHex, UiText.T(
                $"{Unit(target)} is afflicted with {status} ({debuff.roundsRemaining} rounds)",
                $"{Unit(target)} 陷入{status}（{debuff.roundsRemaining} 回合）")));
        }

        public static void Defeated(Card card)
        {
            if (card == null) return;
            Add(UiText.T($"[Down] {Unit(card)} is destroyed", $"【阵亡】{Unit(card)} 被击毁"));
        }

        public static void BossPhase(Card card, string fromName, int fromIndex, EnemyPhase next, int toIndex)
        {
            if (next == null) return;
            string toName = UiText.T(next.displayNameEn, next.displayNameZh);
            string flavor = UiText.T(next.logEn, next.logZh);
            Add(Color(CritHex, UiText.T(
                $"[Boss phase {fromIndex + 1}→{toIndex + 1}] {fromName} becomes {toName}: {flavor}",
                $"【Boss 阶段 {fromIndex + 1}→{toIndex + 1}】{fromName} 转为 {toName}：{flavor}")));
            Add(Color(MutedHex, UiText.T(
                $"  HP {Mathf.RoundToInt(next.health)} · ATK {Mathf.RoundToInt(next.attack)} · DEF {Mathf.RoundToInt(next.defense)}",
                $"  生命 {Mathf.RoundToInt(next.health)} · 攻击 {Mathf.RoundToInt(next.attack)} · 防御 {Mathf.RoundToInt(next.defense)}")));
        }

        public static void RoundCapReached(int maxRound)
        {
            Add(Color(MutedHex, UiText.T(
                $"Round limit {maxRound} reached",
                $"已达回合上限 {maxRound}")));
        }

        public static void Outcome(BattleOutcome outcome, int rounds, int playersAlive, int enemiesAlive)
        {
            string result = outcome switch
            {
                BattleOutcome.Victory => Color(VictoryHex, UiText.T("Victory", "战斗胜利")),
                BattleOutcome.Defeat => Color(EnemyHex, UiText.T("Defeat", "战斗失败")),
                _ => Color(MutedHex, UiText.T("Battle ended", "战斗结束"))
            };
            Add(UiText.T(
                $"[Result] {result} · {rounds} rounds · allies left {playersAlive} · foes left {enemiesAlive}",
                $"【结果】{result} · 共 {rounds} 回合 · 我方存活 {playersAlive} · 敌方存活 {enemiesAlive}"));
        }

        /// <summary>Card-face name tinted by side.</summary>
        public static string Unit(Card card)
        {
            if (card == null) return "?";
            string name = card.nameText != null && !string.IsNullOrEmpty(card.nameText.text)
                ? card.nameText.text
                : card.cardEntity != null && !string.IsNullOrEmpty(card.cardEntity.cardName)
                    ? card.cardEntity.cardName
                    : card.cardEntity?.characterName.ToString() ?? "?";
            return Color(card.isPlayerCard ? PlayerHex : EnemyHex, name);
        }

        private static string StatusName(DebuffEntity debuff)
        {
            if (debuff.damageType == Status.DamageType.Burning) return UiText.T("Burning", "灼烧");
            if (debuff.controllType == Status.ControllType.Stunned) return UiText.T("Stunned", "眩晕");
            if (debuff.controllType == Status.ControllType.Frozen) return UiText.T("Frozen", "冰冻");
            return string.IsNullOrEmpty(debuff.name) ? debuff.type.ToString() : debuff.name;
        }

        private static string Color(string hex, string text) => $"<color={hex}>{text}</color>";
    }
}
