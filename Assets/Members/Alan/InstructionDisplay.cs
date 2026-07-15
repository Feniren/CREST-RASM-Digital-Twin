using UnityEngine;
using TMPro;

public class InstructionDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI infoText; // drag your Text (TMP) UI object here
    // If your text is in world space (not a Canvas), use TextMeshPro instead of TextMeshProUGUI

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
}