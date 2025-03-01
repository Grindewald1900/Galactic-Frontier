using System;
using Assets.Resources.Scripts.Cards;

namespace Assets.Resources.Scripts.Entity
{
    [Serializable]
    public class PlayerEntity
    {
        public string playerName; // Player name
        public string playerID; // Player ID
        public int level; // Level
        public int combatPower; // Combat Power
        public CharacterTier tier; // Rating (S/A/B/C/D)
        public int creditPoints; // Number of credit points
        public string[] title; // Title
        public int explorationProgress; // Exploration degree
        public int skillCount; // Number of skills
        public string[] skills; // Skill list

        public PlayerEntity(string name, string id, int lvl, int power, CharacterTier playerTier, int credits, string[] playerTitle, int explore, string[] playerSkills)
        {
            playerName = name;
            playerID = id;
            level = lvl;
            combatPower = power;
            tier = playerTier;
            creditPoints = credits;
            title = playerTitle;
            explorationProgress = explore;
            skillCount = playerSkills.Length;
            skills = playerSkills;
        }
    }
}