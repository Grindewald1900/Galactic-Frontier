
[System.Serializable]
public class PortraitEntity
{
    public string portraitName = "Asra";
    public string portraitFrame = "TierE_Default";
    public string tier = "TierE";
    bool isShowFrame = true;

    public PortraitEntity(string name, string frame, CharacterTier tier)
    {
        portraitName = name;
        portraitFrame = frame + "_Default";
        this.tier = tier.ToString();
    }

    public PortraitEntity SetShowFrame(bool show)
    {
        isShowFrame = show;
        return this;
    }

    public bool IsShowFrame()
    {
        return isShowFrame;
    }

    public PortraitEntity() { }
}