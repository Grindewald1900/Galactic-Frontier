using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.Dialogue.Domain
{
    [Serializable]
    public sealed class DialogueLineDef
    {
        public string speakerNameEn = "";
        public string speakerNameZh = "";
        public string portraitId = "";
        public string textEn = "";
        public string textZh = "";
    }

    [Serializable]
    public sealed class DialogueScriptDef
    {
        public string dialogueId = "";
        public List<DialogueLineDef> lines = new List<DialogueLineDef>();
    }

    [Serializable]
    public sealed class DialogueCatalogFile
    {
        public List<DialogueScriptDef> dialogues = new List<DialogueScriptDef>();
    }
}
