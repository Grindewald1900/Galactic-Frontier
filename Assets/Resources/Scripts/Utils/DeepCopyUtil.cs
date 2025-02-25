using UnityEngine;

public static class DeepCopyUtil
{
    public static T DeepCopy<T>(T obj)
    {
        string json = JsonUtility.ToJson(obj);
        return JsonUtility.FromJson<T>(json);
    }
}