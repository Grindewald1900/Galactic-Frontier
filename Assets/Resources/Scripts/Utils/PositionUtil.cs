using UnityEngine;

public static class PositionUtil
{
    /// <summary>
    /// 计算一个新的位置，使其在卡牌与摄像机连线上，但仅调整X和Y轴坐标，保持Z轴不变。
    /// </summary>
    /// <param name="cameraPos">摄像机的世界坐标</param>
    /// <param name="cardPos">卡牌的世界坐标</param>
    /// <param name="effectCurrentPos">效果对象当前的位置（用于获取Z坐标）</param>
    /// <param name="t">位置插值因子，0表示卡牌位置，1表示摄像机位置</param>
    /// <returns>返回新的位置</returns>
    public static Vector3 GetEffectPosition(Vector3 cameraPos, Vector3 cardPos, Vector3 effectCurrentPos, float t)
    {
        float newX = Mathf.Lerp(cardPos.x, cameraPos.x, t);
        float newY = Mathf.Lerp(cardPos.y, cameraPos.y, t);
        return new Vector3(newX, newY, effectCurrentPos.z);
    }

    //TODO: WIP
    public static Vector3 GetCenterBottomPosition(GameObject gameObject)
    {
        Vector3 cardBottomCenter = gameObject.transform.position;
        RectTransform rt = gameObject.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.Log("GetCenterBottomPosition: RectTransform not found");
            return cardBottomCenter;
        }
        return cardBottomCenter + new Vector3(0, -rt.rect.height * 0.5f, 0);
    }
}
