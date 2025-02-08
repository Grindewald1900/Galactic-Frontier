using UnityEngine;

public static class ColorUtil
{
    //Speed Controller
    public static Color speedColorOne = new Color32(35, 170, 123, 255);
    public static Color speedColorTwo = new Color32(246, 192, 0, 255);
    public static Color speedColorThree = new Color32(240, 93, 87, 255);
    //Damage Text
    public static Color originaDamagelColor = new Color32(255, 100, 100, 255);
    public static Color plainDamagelColor = new Color32(255, 255, 255, 255);

    public static Color criticalDamagelColor = new Color32(193, 41, 46, 255);
    public static Color missDamagelColor = new Color32(75, 200, 200, 255);
    public static Color sunGlowColor = new Color32(255, 209, 102, 255);
    public static Color ChangeAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

}