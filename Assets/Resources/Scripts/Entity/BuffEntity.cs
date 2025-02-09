using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class BuffEntity
{
    public Status.BuffType type;
    public int roundsRemaining;

    public Sprite icon;

    public BuffEntity(Status.BuffType type, int roundsRemaining, Sprite icon)
    {
        this.type = type;
        this.roundsRemaining = roundsRemaining;
        this.icon = icon;
    }

    public void Purify()
    {
        roundsRemaining = 0;
    }
    public void ChangeRounds(int rounds)
    {
        roundsRemaining += rounds;
    }
}