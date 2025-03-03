using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;

namespace Assets.Resources.Scripts.Entity
{
    [Serializable]
    public class PlayerEntity
    {
        public string playerName; // Player name
        public string playerID; // Player ID
        public string selectedTitle; // Selected title
        public string saveDate; // Save date
        public int level; // Level
        public int combatPower; // Combat Power
        public int creditPoints; // Number of credit points
        public int explorationProgress; // Exploration degree
        public int skillCount; // Number of skills
        public CharacterTier tier; // Rating (S/A/B/C/D)
        public List<string> titles; // Title
        public List<string> skills; // Skill list

        public PlayerEntity()
        {
            playerName = "Player";
            playerID = Guid.NewGuid().ToString();
            selectedTitle = "";
            saveDate = "";
            level = 0;
            combatPower = 0;
            creditPoints = 0;
            tier = CharacterTier.None;
            explorationProgress = 0;
            skillCount = 0;
            titles = new List<string>();
            skills = new List<string>();
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
        }
    }
}