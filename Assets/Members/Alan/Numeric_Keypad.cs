using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A VR-clickable numeric keypad (0-9, Backspace, Clear, Enter) that types
// into a TMP_InputField — lets a trainee enter a slot number by pointing and
// clicking instead of needing a physical keyboard, which isn't usable in VR.
//
// With a SCORBASE panel in the scene it hooks itself up: it types into the
// panel's Source Index by default, switches to whichever of the panel's
// number boxes (Source Index, Target Index, Speed) the trainee last clicked,
// and Enter presses the panel's OK. Target Field / Submit Button only need
// setting by hand to use the keypad with something else.
public class Numeric_Keypad : MonoBehaviour
{
    [Tooltip("The input field this keypad types into. Left empty, it uses the SCORBASE panel's Source Index.")]
    [SerializeField] private TMP_InputField targetField;

    [Header("Buttons")]
    [Tooltip("Ten buttons, index 0-9, one per digit.")]
    [SerializeField] private Button[] digitButtons;
    [SerializeField] private Button backspaceButton;
    [SerializeField] private Button clearButton;
    [Tooltip("The keypad's own Enter button — pressing it invokes Submit Button's click, if one is assigned.")]
    [SerializeField] private Button enterButton;
    [Tooltip("Invoked when Enter is pressed. Left empty, it uses the SCORBASE panel's OK button.")]
    [SerializeField] private Button submitButton;

    [Tooltip("Optional — found in the scene automatically when left empty.")]
    [SerializeField] private ASRS_Scorbase_Panel scorbasePanel;

    public TMP_InputField TargetField => targetField;

    private void Awake()
    {
        if (scorbasePanel == null)
            scorbasePanel = FindFirstObjectByType<ASRS_Scorbase_Panel>(FindObjectsInactive.Include);

        if (scorbasePanel != null)
        {
            if (targetField == null)
                targetField = scorbasePanel.SourceIndexField;
            if (submitButton == null)
                submitButton = scorbasePanel.GoButton;

            // Clicking one of the panel's number boxes makes it the one the
            // keypad types into.
            foreach (TMP_InputField field in new List<TMP_InputField> { scorbasePanel.SourceIndexField, scorbasePanel.TargetIndexField, scorbasePanel.SpeedField })
                if (field != null)
                {
                    TMP_InputField captured = field;
                    captured.onSelect.AddListener(_ => targetField = captured);
                }
        }

        if (targetField == null)
            Debug.LogWarning("[Numeric_Keypad] No input field to type into — assign Target Field or add a SCORBASE panel to the scene.", this);

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

        if (targetField.characterLimit > 0 && targetField.text.Length >= targetField.characterLimit)
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

    // Enter on the Speed box just applies the speed; on any other box it
    // presses OK (Submit Button).
    public void PressEnter()
    {
        if (targetField != null)
            targetField.onEndEdit.Invoke(targetField.text);

        bool isSpeed = scorbasePanel != null && targetField != null && targetField == scorbasePanel.SpeedField;
        if (!isSpeed && submitButton != null)
            submitButton.onClick.Invoke();
    }
}
