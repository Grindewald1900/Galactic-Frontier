using UnityEngine;
using GamePlayMode = Assets.Resources.Scripts.Market.Domain.PlayMode;

namespace Assets.Resources.Scripts.Market
{
    /// <summary>Session play mode. MVP defaults to Solo; Online enables player market later.</summary>
    public static class PlayModeService
    {
        public const string PrefKey = "galactic_frontier.play_mode";

        public static GamePlayMode Current
        {
            get
            {
                var v = PlayerPrefs.GetInt(PrefKey, (int)GamePlayMode.Solo);
                return v == (int)GamePlayMode.Online ? GamePlayMode.Online : GamePlayMode.Solo;
            }
            set => PlayerPrefs.SetInt(PrefKey, (int)value);
        }

        public static bool IsSolo => Current == GamePlayMode.Solo;
        public static bool IsOnline => Current == GamePlayMode.Online;

        /// <summary>Dev / future toggle. Persists via PlayerPrefs.</summary>
        public static void SetMode(GamePlayMode mode)
        {
            Current = mode;
            PlayerPrefs.Save();
            Debug.Log("[PLAYMODE] " + mode);
        }
    }
}
