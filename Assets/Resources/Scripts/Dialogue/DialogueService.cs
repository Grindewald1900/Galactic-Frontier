using System;
using Assets.Resources.Scripts.Dialogue.Domain;
using Assets.Resources.Scripts.Quest;
using Assets.Resources.Scripts.UI.Nexus;

namespace Assets.Resources.Scripts.Dialogue
{
    /// <summary>Entry point for scripted NPC dialogue. Each script plays at most once per save.</summary>
    public static class DialogueService
    {
        public static bool IsPlaying => NpcDialogueOverlay.IsOpen;

        public static bool HasPlayed(string dialogueId) =>
            QuestService.HasPlayedDialogue(dialogueId);

        public static void Play(string dialogueId, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(dialogueId))
            {
                onComplete?.Invoke();
                return;
            }

            QuestService.EnsureLoaded();
            if (QuestService.HasPlayedDialogue(dialogueId))
            {
                onComplete?.Invoke();
                return;
            }

            var script = DialogueCatalog.Get(dialogueId);
            if (script == null || script.lines == null || script.lines.Count == 0)
            {
                UnityEngine.Debug.LogWarning("[DIALOGUE] Unknown or empty script: " + dialogueId);
                QuestService.MarkDialoguePlayed(dialogueId);
                onComplete?.Invoke();
                return;
            }

            QuestService.MarkDialoguePlayed(dialogueId);
            NpcDialogueOverlay.Show(script, onComplete);
        }

        public static void Close() => NpcDialogueOverlay.Close();
    }
}
