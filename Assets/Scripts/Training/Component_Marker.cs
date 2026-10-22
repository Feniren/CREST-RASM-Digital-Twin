using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Component_Marker : MonoBehaviour{
    public string Marker_Id;
    public string Display_Name;

    [SerializeField] private Renderer GlowShell;

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

    // Selection entry point shared by XRI select and Desktop_Click_Select —
    // the lesson flow must not depend on an XRI interactor being present.
    public void Notify_Selected(){
        Selected?.Invoke(this);
    }

    // Hover entry point, shared the same way: XRI ray hover and the desktop crosshair.
    public void Notify_Hover(bool entered){
        Hover_Changed?.Invoke(this, entered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args){
        Notify_Selected();
    }

    private void OnHoverEntered(HoverEnterEventArgs args){
        Notify_Hover(true);
    }

    private void OnHoverExited(HoverExitEventArgs args){
        Notify_Hover(false);
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
