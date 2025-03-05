using UnityEngine;
using UnityEngine.SceneManagement;

public class DefaultSceneLoader
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void LoadDefaultScene()
    {
        string defaultScene = "MainMenuScene";
        if (SceneManager.GetActiveScene().name != defaultScene)
        {
            Debug.Log($"Loading default scene: {defaultScene}");
            SceneManager.LoadScene(defaultScene);
        }
    }
}