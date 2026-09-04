using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using static Assets.Resources.Scripts.Cards.CardDataManager;

namespace Assets.Resources.Scripts.Entity
{
    [Serializable]
    public class PlayerEntity
    {
        public string playerName; // Player name
        public string playerID; // Player ID
        public string selectedTitle; // Selected title
        public string saveDate; // Save date
        public float commanderExp;
        public float commanderExpToNext;
        public int level; // Commander level
        public int combatPower; // Combat Power
        public int creditPoints; // Number of credit points (alias: credits)
        /// <summary>Bound credits — NPC/exchange only; cannot enter player market later.</summary>
        public int creditsBound;
        public int explorationProgress; // Exploration degree
        public int skillCount; // Number of skills
        public CharacterTier tier; // Rating (S/A/B/C/D)
        public List<string> titles; // Title
        public List<string> skills; // Skill list
        /// <summary>Equipped cosmetic frame id (catalog). Default <c>frame_default</c>.</summary>
        public string avatarFrameId;
        public List<string> unlockedAvatarFrameIds;
        public List<string> announcedAvatarFrameIds;
        /// <summary>True after first boot seeded achievement frames without announcement spam.</summary>
        public bool avatarFramesSeeded;

        public PlayerEntity()
        {
            playerName = "Player";
            playerID = Guid.NewGuid().ToString();
            selectedTitle = "";
            saveDate = "";
            level = 1;
            commanderExp = 0f;
            commanderExpToNext = 0f;
            combatPower = 0;
            creditPoints = 0;
            creditsBound = 0;
            tier = CharacterTier.None;
            explorationProgress = 0;
            skillCount = 0;
            titles = new List<string>();
            skills = new List<string>();
            InitAvatarFrames();
        }

        public PlayerEntity(string name, int lvl, int power, CharacterTier playerTier, int credits, List<string> playerTitle, int explore, List<string> playerSkills)
        {
            playerName = name;
            playerID = Guid.NewGuid().ToString();
            level = lvl;
            combatPower = power;
            tier = playerTier;
            creditPoints = credits;
            titles = playerTitle;
            explorationProgress = explore;
            skillCount = playerSkills.Count;
            skills = playerSkills;
            InitAvatarFrames();
        }

        private void InitAvatarFrames()
        {
            avatarFrameId = "frame_default";
            unlockedAvatarFrameIds = new List<string> { "frame_default" };
            announcedAvatarFrameIds = new List<string>();
            avatarFramesSeeded = false;
        }
    }
}