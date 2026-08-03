using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum InteractionType { Interact, AlternateInteract }

[RequireComponent(typeof(Interactable_Select))]
[RequireComponent(typeof(MeshRenderer))]
public class Marker_Interactable : MonoBehaviour
{
    bool Activated;
    MeshRenderer MeshRendererReference;

    public string Marker_Id;

    [SerializeField] private float FadeDuration = 0.25f;
    [SerializeField] private float PulseSpeed = 2f;
    [SerializeField] private float PulseAmount = 0.3f;

    public event Action<Marker_Interactable, InteractionType> Selected;
    public UnityEvent OnSelected; // wire per-marker actions (e.g. play an animation) here in the Inspector

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock block;
    private Coroutine fadeRoutine;
    private Coroutine idleRoutine;

    public void Start()
    {
        MeshRendererReference = GetComponent<MeshRenderer>();
        block = new MaterialPropertyBlock();

        Interactable_Select select = GetComponent<Interactable_Select>();
        select.OnInteractBegin.AddListener(() => ToggleVisibility(InteractionType.Interact));

        idleRoutine = StartCoroutine(IdlePulse());
    }

    public void ToggleVisibility(InteractionType interactionType)
    {
        Activated = !Activated;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        if (idleRoutine != null)
            StopCoroutine(idleRoutine);

        MeshRendererReference.enabled = false;

        Selected?.Invoke(this, interactionType);
        OnSelected?.Invoke();
    }

    private IEnumerator FadeOut()
    {
        yield return FadeTo(0f);
        fadeRoutine = null;
    }

    private IEnumerator FadeInThenIdle()
    {
        yield return FadeTo(1f);
        fadeRoutine = null;
        idleRoutine = StartCoroutine(IdlePulse());
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        MeshRendererReference.enabled = true;

        float startAlpha = MeshRendererReference.sharedMaterial.color.a;
        float elapsed = 0f;

        while (elapsed < FadeDuration)
        {
            elapsed += Time.deltaTime;
            ApplyAlpha(Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / FadeDuration)));
            yield return null;
        }

        ApplyAlpha(targetAlpha);
        MeshRendererReference.enabled = targetAlpha > 0f;
    }

    // Gentle breathing pulse while fully visible, so the marker keeps drawing the eye.
    private IEnumerator IdlePulse()
    {
        float baseAlpha = MeshRendererReference.sharedMaterial.color.a;

        while (true)
        {
            float wave = (Mathf.Sin(Time.time * PulseSpeed) + 1f) * 0.5f;
            ApplyAlpha(baseAlpha - PulseAmount * wave);
            yield return null;
        }
    }

    private void ApplyAlpha(float alpha)
    {
        Color color = MeshRendererReference.sharedMaterial.color;
        color.a = alpha;
        block.SetColor(BaseColorId, color);
        MeshRendererReference.SetPropertyBlock(block);
    }
}