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

    [Tooltip("Body text size on steps marked Large Text (auto-size range when the body auto-sizes).")]
    [SerializeField] private float largeFontSizeMax = 5.2f;
    [SerializeField] private float largeFontSizeMin = 3.4f;

    public bool HasTitle => titleText != null;

    private bool normalSizeCaptured;
    private float normalFontSize, normalFontSizeMin, normalFontSizeMax;

    // Bigger body text for steps that ask for it; false restores the size
    // the panel was built with.
    public void SetLargeText(bool large)
    {
        if (infoText == null)
            return;

        if (!normalSizeCaptured)
        {
            normalFontSize = infoText.fontSize;
            normalFontSizeMin = infoText.fontSizeMin;
            normalFontSizeMax = infoText.fontSizeMax;
            normalSizeCaptured = true;
        }

        if (infoText.enableAutoSizing)
        {
            infoText.fontSizeMin = large ? largeFontSizeMin : normalFontSizeMin;
            infoText.fontSizeMax = large ? largeFontSizeMax : normalFontSizeMax;
        }
        else
        {
            infoText.fontSize = large ? largeFontSizeMax : normalFontSize;
        }
    }

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
