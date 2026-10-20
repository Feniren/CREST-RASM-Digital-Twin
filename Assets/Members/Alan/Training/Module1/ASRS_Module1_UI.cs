using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Button wiring shared by the Module 1 panels. Their buttons are the widget
// ListItemButton plus an Interactable_Select + collider (like the Lesson
// Panel's Next), so a press can arrive through the UI ray AND trigger
// interact. Bind() listens to both; the Debouncer drops the second arrival
// so one physical press never counts twice (which would skip a question or
// toggle pause straight back).
public static class ASRS_Module1_UI
{
    public const float PressDebounce = 0.3f;

    public sealed class Debouncer
    {
        private float lastPress = float.NegativeInfinity;

        public bool Accept()
        {
            if (Time.unscaledTime - lastPress < PressDebounce)
                return false;

            lastPress = Time.unscaledTime;
            return true;
        }
    }

    public static void Bind(Button button, Debouncer debouncer, UnityAction action)
    {
        if (button == null || action == null)
            return;

        UnityAction guarded = () =>
        {
            if (debouncer == null || debouncer.Accept())
                action();
        };

        button.onClick.AddListener(guarded);

        Interactable_Select select = button.GetComponent<Interactable_Select>();
        if (select != null)
            select.OnInteractBegin.AddListener(guarded);
    }

    public static void SetVisible(Component component, bool visible)
    {
        if (component != null)
            component.gameObject.SetActive(visible);
    }

    public static void SetText(TMPro.TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
