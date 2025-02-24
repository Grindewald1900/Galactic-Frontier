using System;
using UnityEngine;

[Serializable]
public class PlayerEntity
{
    public string playerName;  // 玩家名
    public int playerID;  // 玩家ID
    public int level;  // 等级
    public int combatPower;  // 战力
    public CharacterTier tier;  // 评级（S/A/B/C/D）
    public int creditPoints;  // 信用点数量
    public string[] title;  // 称号
    public int explorationProgress;  // 探索度
    public int skillCount;  // 技能数量
    public string[] skills;  // 技能列表

    public PlayerEntity(string name, int id, int lvl, int power, CharacterTier playerTier, int credits, string[] playerTitle, int explore, string[] playerSkills)
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