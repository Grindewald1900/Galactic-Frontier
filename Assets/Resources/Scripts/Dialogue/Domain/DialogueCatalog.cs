using System.Collections.Generic;
using UnityEngine;

namespace Assets.Resources.Scripts.Dialogue.Domain
{
    public static class DialogueCatalog
    {
        public const string ResourcePath = "Data/Dialogues";

        private static Dictionary<string, DialogueScriptDef> byId;
        private static bool loaded;

        public static DialogueScriptDef Get(string dialogueId)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(dialogueId) || byId == null)
                return null;
            return byId.TryGetValue(dialogueId, out var script) ? script : null;
        }

        public static void Reload()
        {
            loaded = false;
            byId = null;
            EnsureLoaded();
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            byId = new Dictionary<string, DialogueScriptDef>();

            var asset = UnityEngine.Resources.Load<TextAsset>(ResourcePath);
            if (asset != null && !string.IsNullOrEmpty(asset.text))
            {
                var file = JsonUtility.FromJson<DialogueCatalogFile>(asset.text);
                if (file?.dialogues != null)
                {
                    foreach (var script in file.dialogues)
                    {
                        if (script == null || string.IsNullOrEmpty(script.dialogueId))
                            continue;
                        byId[script.dialogueId] = script;
                    }
                }
            }

            foreach (var fallback in BuildFallback())
            {
                if (!byId.ContainsKey(fallback.dialogueId))
                    byId[fallback.dialogueId] = fallback;
            }
        }

        private static IEnumerable<DialogueScriptDef> BuildFallback()
        {
            yield return new DialogueScriptDef
            {
                dialogueId = "demo_bridge_intro",
                lines = new List<DialogueLineDef>
                {
                    new()
                    {
                        speakerNameEn = "Asra",
                        speakerNameZh = "阿斯拉",
                        portraitId = "Asra",
                        textEn = "Commander, welcome back to Frontier VII.",
                        textZh = "指挥官，欢迎回到第七前沿。"
                    },
                    new()
                    {
                        speakerNameEn = "Asra",
                        speakerNameZh = "阿斯拉",
                        portraitId = "Asra",
                        textEn = "The astral grid is stable for now — but entropy readings are climbing on the outer lanes.",
                        textZh = "航网暂时稳定，但外缘航线的熵读数正在攀升。"
                    },
                    new()
                    {
                        speakerNameEn = "Bridge AI",
                        speakerNameZh = "舰桥 AI",
                        portraitId = "",
                        textEn = "Tip: select a located node on Explore to challenge a region directly.",
                        textZh = "提示：在探索界面选中已定位节点，可直接挑战对应区域。"
                    },
                    new()
                    {
                        speakerNameEn = "Asra",
                        speakerNameZh = "阿斯拉",
                        portraitId = "Asra",
                        textEn = "When you're ready, open Formation and deploy your combat deck.",
                        textZh = "准备好了就打开编队，部署你的战斗卡组。"
                    }
                }
            };
        }
    }
}
