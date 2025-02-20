[System.Serializable]
public class BadgeEntity
{
    public string badgeName;
    public string badgeDescription;

    public BadgeEntity() { }
    public BadgeEntity(string badgeName, string badgeDescription)
    {
        this.badgeName = badgeName;
        this.badgeDescription = badgeDescription;
    }
}