using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

public class SlimeController : MonoBehaviour
{
    public Transform player;
    public Sprite characterImage;
    private bool isInteractive = true;
    private bool isFirstMeet = true;
    private float distanceThreshold = 5f;

    void Update()
    {
        CheckIsInteractive();
        if (isInteractive)
        {
            HintTextController.Instance.ShowHint("Press E to interact");
            if (Input.GetKeyDown(KeyCode.E))
            {
                ShowDialogue();
                HintTextController.Instance.HideHint();
            }
        }
        else
        {
            HintTextController.Instance.HideHint();
            DialogueController.Instance.HideDialogue();
        }
    }

    private void CheckIsInteractive()
    {
        float distance = Vector2.Distance(player.position, transform.position);
        isInteractive = distance < distanceThreshold;
    }

    private void ShowDialogue()
    {
        if (isFirstMeet)
        {
            DialogueController.Instance.StartDialogue(DialogueContent.slimeGreetings, characterImage, "Slime");
            isFirstMeet = false;
        }
        else
        {
            DialogueController.Instance.StartDialogue(DialogueContent.slime1, characterImage, "Slime");
        }
    }
}
