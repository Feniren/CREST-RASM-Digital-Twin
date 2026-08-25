using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A VR-clickable numeric keypad (0-9, Backspace, Clear, Enter) that types
// directly into an assigned TMP_InputField — lets a trainee enter a table ID
// (or any other numeric field) by pointing and clicking instead of needing a
// physical keyboard's number pad, which isn't usable in VR.
public class Numeric_Keypad : MonoBehaviour
{
    [Tooltip("The input field this keypad types into — e.g. the SCORBASE panel's Table ID field.")]
    [SerializeField] private TMP_InputField targetField;

    [Header("Buttons")]
    [Tooltip("Ten buttons, index 0-9, one per digit.")]
    [SerializeField] private Button[] digitButtons;
    [SerializeField] private Button backspaceButton;
    [SerializeField] private Button clearButton;
    [Tooltip("The keypad's own Enter button — pressing it invokes Submit Button's click, if one is assigned.")]
    [SerializeField] private Button enterButton;
    [Tooltip("Optional — invoked when Enter is pressed, e.g. wire to the SCORBASE panel's Go button so Enter behaves the same as pressing Go.")]
    [SerializeField] private Button submitButton;

    private void Awake()
    {
        for (int i = 0; i < digitButtons.Length; i++)
        {
            int digit = i; // capture for the closure
            if (digitButtons[i] != null)
                digitButtons[i].onClick.AddListener(() => PressDigit(digit));
        }

        if (backspaceButton != null) backspaceButton.onClick.AddListener(PressBackspace);
        if (clearButton != null) clearButton.onClick.AddListener(PressClear);
        if (enterButton != null) enterButton.onClick.AddListener(PressEnter);
    }

    public void PressDigit(int digit)
    {
        if (targetField == null)
            return;

        targetField.text += digit.ToString();
    }

    public void PressBackspace()
    {
        if (targetField == null || string.IsNullOrEmpty(targetField.text))
            return;

        targetField.text = targetField.text.Substring(0, targetField.text.Length - 1);
    }

    public void PressClear()
    {
        if (targetField != null)
            targetField.text = string.Empty;
    }

    public void PressEnter()
    {
        if (submitButton != null)
            submitButton.onClick.Invoke();
    }
}
