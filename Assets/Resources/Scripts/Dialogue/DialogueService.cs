using System;
using Assets.Resources.Scripts.Dialogue.Domain;
using Assets.Resources.Scripts.UI.Nexus;

namespace Assets.Resources.Scripts.Dialogue
{
    /// <summary>Entry point for scripted NPC dialogue playback.</summary>
    public static class DialogueService
    {
        public static bool IsPlaying => NpcDialogueOverlay.IsOpen;

        public static void Play(string dialogueId, Action onComplete = null)
        {
            var script = DialogueCatalog.Get(dialogueId);
            if (script == null || script.lines == null || script.lines.Count == 0)
            {
                UnityEngine.Debug.LogWarning("[DIALOGUE] Unknown or empty script: " + dialogueId);
                onComplete?.Invoke();
                return;
            }

            NpcDialogueOverlay.Show(script, onComplete);
        }

        public static void Close() => NpcDialogueOverlay.Close();
    }
}
