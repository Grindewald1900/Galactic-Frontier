using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages tab button interactions and switches associated panels.
/// </summary>
public class TabSwitcher : MonoBehaviour
{
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private GameObject[] panels;

    private int currentTab = -1;

    private void Awake()
    {
        if (tabButtons == null || panels == null)
        {
            Debug.LogError("TabSwitcher: Tab buttons or panels array is null.");
            enabled = false;
            return;
        }
        if (tabButtons.Length != panels.Length)
        {
            Debug.LogError("TabSwitcher: tabButtons and panels count mismatch.");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        for (int i = 0; i < tabButtons.Length; ++i)
        {
            int idx = i;
            if (tabButtons[i] != null)
                tabButtons[i].onClick.AddListener(() => SwitchTab(idx));
        }
        if (currentTab < 0) SwitchTab(0);
        else RefreshTabs();
    }

    private void OnDisable()
    {
        for (int i = 0; i < tabButtons.Length; ++i)
        {
            if (tabButtons[i] != null)
                tabButtons[i].onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// Switches to the tab at the given index.
    /// </summary>
    /// <param name="index">Tab index to activate.</param>
    public void SwitchTab(int index)
    {
        if (index < 0 || index >= panels.Length || index == currentTab) return;
        currentTab = index;
        RefreshTabs();
    }

    /// <summary>
    /// Gets currently active tab index.
    /// </summary>
    public int CurrentTab => currentTab;

    private void RefreshTabs()
    {
        for (int i = 0; i < panels.Length; ++i)
        {
            if (panels[i] != null) panels[i].SetActive(i == currentTab);
            if (tabButtons[i] != null) tabButtons[i].interactable = i != currentTab;
        }
    }
}