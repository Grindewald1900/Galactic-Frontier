using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueController : MonoBehaviour
{
    public static DialogueController Instance;
    public Sprite defaultSprite;
    public Image characterImage;
    public TextMeshProUGUI dialogText;
    public TextMeshProUGUI continueText;
    public Button nextButton;
    public Button prevButton;
    public Button skipButton;
    private string characterNameText;


    private string[] currentDialogue;
    private int currentIndex = 0;
    private bool isDialogueActive = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        HideDialogue();
        nextButton.onClick.AddListener(NextDialogue);
        prevButton.onClick.AddListener(PreviousDialogue);
        skipButton.onClick.AddListener(SkipDialogue);
    }

    void Update()
    {
        CheckInput();
    }

    private void CheckInput()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (isDialogueActive)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                NextDialogue();
            if (canvas != null && Input.GetMouseButtonDown(0))
                if (MouseUtil.IsPointerOverUIElement(dialogText.gameObject, canvas))
                    NextDialogue();
        }
    }
    public void StartDialogue(string[] dialogue, Sprite characterSprite, string characterName = "Character")
    {
        if (dialogue == null || dialogue.Length == 0) return;
        currentDialogue = dialogue;
        currentIndex = 0;
        isDialogueActive = true;
        if (characterSprite != null)
            characterImage.sprite = characterSprite;
        else
            characterImage.sprite = defaultSprite;
        characterNameText = characterName;
        ShowDialogue();
        DisplayDialogue(currentIndex);
    }

    public void ShowDialogue()
    {
        gameObject.SetActive(true);
    }

    void HideDialogue()
    {
        gameObject.SetActive(false);
        isDialogueActive = false;
    }

    void DisplayDialogue(int index)
    {
        if (index >= 0 && index < currentDialogue.Length)
        {
            dialogText.text = currentDialogue[index];
            prevButton.interactable = index > 0;
            nextButton.interactable = index < currentDialogue.Length - 1;
        }
    }

    public void NextDialogue()
    {
        if (isDialogueActive && currentIndex < currentDialogue.Length - 1)
        {
            currentIndex++;
            DisplayDialogue(currentIndex);
        }
        else
        {
            EndDialogue();
        }
    }

    public void PreviousDialogue()
    {
        if (isDialogueActive && currentIndex > 0)
        {
            currentIndex--;
            DisplayDialogue(currentIndex);
        }
    }

    public void SkipDialogue()
    {
        EndDialogue();
    }

    void EndDialogue()
    {
        HideDialogue();
        currentDialogue = null;
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}
