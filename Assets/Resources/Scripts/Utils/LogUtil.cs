using UnityEngine;
using UnityEngine.UI;

public static class LogUtil
{
    public static bool CheckNull(Object obj, string objName = "")
    {
        if (obj == null)
        {
            LogError(objName + " is null.");
            return true;
        }
        return false;
    }

    public static void Log(string message)
    {
        Debug.Log(message);
    }

    public static void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }

    public static void LogError(string message)
    {
        Debug.LogError(message);
    }
}