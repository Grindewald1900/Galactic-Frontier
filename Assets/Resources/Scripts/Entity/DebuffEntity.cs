using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DebuffEntity
{
    public Status.DebuffType type;
    public int roundsRemaining;
    public Sprite icon;

    public DebuffEntity(Status.DebuffType type, int roundsRemaining, Sprite icon)
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
