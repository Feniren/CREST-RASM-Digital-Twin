using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// A 3D-clickable startup action: the trainee selects a real machine part (the
// mill's "kaig" main-power switch) instead of a UI button. Mirrors Component_Marker
// but feeds the Module 2 action pipeline (Action_Button_Registry -> Notify_Action).
// The always-visible bounding box is the clickable affordance; it brightens on
// hover and turns to the highlight color while it is the current guided step.
[RequireComponent(typeof(XRSimpleInteractable))]
public class Action_Interactable : MonoBehaviour{
    public string Action_Id;

    [SerializeField] private Renderer BoundingBox;
    [SerializeField] private Color IdleColor = new Color(0.3f, 0.8f, 1f, 0.32f);
    [SerializeField] private Color HoverColor = new Color(0.3f, 0.85f, 1f, 0.6f);
    [SerializeField] private Color HighlightColor = new Color(1f, 0.85f, 0.2f, 0.75f);

    public event Action<string> Clicked;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock block;
    private XRSimpleInteractable interactable;
    private bool highlighted;
    private bool hovered;

    private void Awake(){
        block = new MaterialPropertyBlock();
        interactable = GetComponent<XRSimpleInteractable>();
        Refresh();
    }

    private void OnEnable(){
        if (interactable == null)
            interactable = GetComponent<XRSimpleInteractable>();

        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
    }

    private void OnDisable(){
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
    }

    public void Set_Highlight(bool on){
        highlighted = on;
        Refresh();
    }

    private void OnSelectEntered(SelectEnterEventArgs args){
        Clicked?.Invoke(Action_Id);
    }

    private void OnHoverEntered(HoverEnterEventArgs args){
        hovered = true;
        Refresh();
    }

    private void OnHoverExited(HoverExitEventArgs args){
        hovered = false;
        Refresh();
    }

    private void Refresh(){
        if (BoundingBox == null)
            return;

        BoundingBox.GetPropertyBlock(block);
        block.SetColor(BaseColorId, highlighted ? HighlightColor : hovered ? HoverColor : IdleColor);
        BoundingBox.SetPropertyBlock(block);
    }
}
