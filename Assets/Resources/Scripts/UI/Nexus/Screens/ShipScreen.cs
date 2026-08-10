using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Minimal ship bay: level, stats, module upgrades (P2.2).</summary>
    internal sealed class ShipScreen
    {
        private readonly Transform root;
        private readonly System.Action onClose;

        private ShipScreen(Transform root, System.Action onClose)
        {
            this.root = root;
            this.onClose = onClose;
        }

        public GameObject Root => root.gameObject;

        public static ShipScreen Build(Transform parent, System.Action onClose)
        {
            var panel = NexusUiFactory.CreatePanel(
                parent, "Ship Screen", NexusTheme.Background,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var screen = new ShipScreen(panel.transform, onClose);
            screen.Rebuild();
            return screen;
        }

        public void Rebuild()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            if (DataUtil.Instance != null)
                ShipService.EnsureLoaded(DataUtil.Instance);

            var ship = ShipService.State ?? ShipRules.CreateStarterShip();
            var stats = ShipRules.GetEffectiveStats(ship);

            NexusUiFactory.CreateText(
                root, "Title", UiText.ShipBayTitle,
                new Vector2(28f, 20f), new Vector2(600f, 36f), 22f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            NexusUiFactory.CreateText(
                root, "Subtitle",
                $"{ship.displayName} · Lv.{ship.level} · Credits {DataUtil.Instance?.currentPlayer?.creditPoints ?? 0}",
                new Vector2(28f, 60f), new Vector2(900f, 24f), 13f, NexusTheme.MutedText);

            NexusUiFactory.CreateText(
                root, "Stats",
                $"Range {stats.range}  Energy {stats.energy}  Hull {stats.hull}  Entropy {stats.entropyResist}\n" +
                $"Cargo {stats.cargo}  Scan {stats.scan}  Life {stats.lifeSupport}",
                new Vector2(28f, 100f), new Vector2(1100f, 60f), 14f, NexusTheme.Cyan);

            NexusUiFactory.CreateButton(
                root, "UpgradeLevel",
                UiText.UpgradeShipLevel(
                    ShipRules.ScrapCostForShipLevel(ship.level + 1),
                    ShipRules.CreditCostForShipLevel(ship.level + 1)),
                new Vector2(28f, 180f), new Vector2(420f, 44f),
                () =>
                {
                    var r = ShipService.TryUpgradeShipLevel();
                    Debug.Log("[SHIP] level upgrade: " + (r.Success ? "ok" : r.Message));
                    Rebuild();
                },
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f), NexusTheme.Gold, 13f);

            float y = 240f;
            foreach (var def in ShipModuleCatalog.All)
            {
                int level = 0;
                if (ship.modules != null)
                {
                    foreach (var m in ship.modules)
                    {
                        if (m != null && m.moduleId == def.moduleId)
                            level = m.level;
                    }
                }

                string label =
                    $"{UiText.T(def.displayNameEn, def.displayNameZh)} Lv.{level}  (+{def.primaryStat})  " +
                    $"[{ShipRules.ScrapCostForModule(def.moduleId, level + 1)} scrap / {ShipRules.CreditCostForModule(def.moduleId, level + 1)}₵]";

                string captured = def.moduleId;
                NexusUiFactory.CreateButton(
                    root, "Mod " + def.moduleId, label,
                    new Vector2(28f, y), new Vector2(1100f, 40f),
                    () =>
                    {
                        var r = ShipService.TryUpgradeModule(captured);
                        Debug.Log("[SHIP] module " + captured + ": " + (r.Success ? "ok" : r.Message));
                        Rebuild();
                    },
                    NexusTheme.SurfaceRaised, NexusTheme.Text, 12f);
                y += 48f;
            }

            NexusUiFactory.CreateButton(
                root, "Back", UiText.T("Back", "返回"),
                new Vector2(28f, 900f), new Vector2(200f, 44f),
                () => onClose?.Invoke(),
                NexusTheme.SurfaceRaised, NexusTheme.MutedText, 14f);
        }
    }
}
