using UnityEngine;
using TMPro;

public class InstructionDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI infoText; // drag your Text (TMP) UI object here
    // If your text is in world space (not a Canvas), use TextMeshPro instead of TextMeshProUGUI

    [Header("Optional (Lesson Panel)")]
    [Tooltip("Optional heading. When set, SequenceManager puts each step's title here instead of inside the body text.")]
    [SerializeField] private TextMeshProUGUI titleText;
    [Tooltip("Optional small label for lesson progress, e.g. \"Step 3 of 13\".")]
    [SerializeField] private TextMeshProUGUI progressText;
    [Tooltip("Heading used by steps that don't set their own title.")]
    [SerializeField] private string defaultTitle = "ASRS Overview";

    public bool HasTitle => titleText != null;

    public void UpdateText(string newText)
    {
        if (infoText != null)
            infoText.text = newText;
    }

    public void ClearText()
    {
        if (infoText != null)
            infoText.text = "";
    }

    // Empty or null falls back to defaultTitle.
    public void UpdateTitle(string newTitle)
    {
        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(newTitle) ? defaultTitle : newTitle;
    }

    public void UpdateProgress(string newProgress)
    {
        if (progressText != null)
            progressText.text = newProgress;
    }
}
