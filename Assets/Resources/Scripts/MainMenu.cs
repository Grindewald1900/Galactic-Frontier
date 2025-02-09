using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button buttonLoad;
    public Button buttonPlay;
    public Button buttonSetting;

    void Start()
    {
        buttonPlay.onClick.AddListener(OnPlayButtonClick);
    }


    void OnPlayButtonClick()
    {
        SceneManager.LoadScene(Constants.sceneHome);
    }
}
