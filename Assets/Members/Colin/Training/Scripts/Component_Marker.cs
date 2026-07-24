using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Component_Marker : MonoBehaviour{
    public string Marker_Id;
    public string Display_Name;

    [SerializeField] private Renderer GlowShell;
    [SerializeField] private GameObject LabelRoot;

    public event Action<Component_Marker> Selected;
    public event Action<Component_Marker, bool> Hover_Changed;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private bool persistentOn;
    private Color persistentColor;
    private bool hoverOn;
    private Color hoverColor;
    private MaterialPropertyBlock block;
    private XRSimpleInteractable interactable;

    private void Awake(){
        block = new MaterialPropertyBlock();
        interactable = GetComponent<XRSimpleInteractable>();

        if (interactable == null)
            Debug.LogError($"Component_Marker: no XRSimpleInteractable on '{name}' — this marker can never be clicked.", this);

        Refresh();
    }

    private void OnEnable(){
        if (interactable == null)
            interactable = GetComponent<XRSimpleInteractable>();

        if (interactable != null){
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.hoverEntered.AddListener(OnHoverEntered);
            interactable.hoverExited.AddListener(OnHoverExited);
        }
    }

    private void OnDisable(){
        if (interactable != null){
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
        }
    }

    public void Set_Persistent_Glow(bool on, Color color){
        persistentOn = on;
        persistentColor = color;
        Refresh();
    }

    public void Set_Hover_Tint(Color color){
        hoverOn = true;
        hoverColor = color;
        Refresh();
    }

    public void Clear_Hover_Tint(){
        hoverOn = false;
        Refresh();
    }

    public void Set_Label_Visible(bool visible){
        if (LabelRoot != null)
            LabelRoot.SetActive(visible);
    }

    // Fully disables the marker — not just "ignored by the sequencer" but the
    // whole GameObject deactivated, so a wrong-marker click can't hover, select,
    // or even register a hit while it isn't the active step's target.
    public void Set_Interactable(bool value){
        gameObject.SetActive(value);
    }

    private void OnSelectEntered(SelectEnterEventArgs args){
        Debug.Log($"Component_Marker: selectEntered on '{Marker_Id}' ({name}) by '{args.interactorObject}'.", this);
        Selected?.Invoke(this);
    }

    private void OnHoverEntered(HoverEnterEventArgs args){
        Debug.Log($"Component_Marker: hoverEntered on '{Marker_Id}' ({name}) by '{args.interactorObject}'.", this);
        Hover_Changed?.Invoke(this, true);
    }

    private void OnHoverExited(HoverExitEventArgs args){
        Hover_Changed?.Invoke(this, false);
    }

    private void Refresh(){
        if (GlowShell == null)
            return;

        bool visible = persistentOn || hoverOn;
        GlowShell.enabled = visible;

        if (!visible)
            return;

        GlowShell.GetPropertyBlock(block);
        block.SetColor(BaseColorId, hoverOn ? hoverColor : persistentColor);
        GlowShell.SetPropertyBlock(block);
    }
}
