using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum InteractionType { Interact, AlternateInteract }

// The green "click me" box over a part during part identification.
//
// Visibility rules (why it used to look inconsistent):
//  - The pulse used to subtract PulseAmount straight from the material's own
//    alpha. Green_Glass is only 0.34 opaque, so every ~3 s the marker dipped
//    to ~0.04 and practically vanished ("phasing"). Now the marker pulses
//    between Visible Alpha and Visible Alpha × (1 − Pulse Amount), so it
//    never drops below ~70% of its peak.
//  - A coroutine dies when its GameObject is deactivated, and Start() never
//    runs twice, so a marker that was hidden and shown again froze at
//    whatever alpha it had at that moment — sometimes almost invisible. The
//    pulse now restarts in OnEnable.
//  - Two transparent objects (the marker and the rack's glass) swap draw
//    order as you move, so the marker flickered when seen through glass.
//    Sorting Order makes it always draw after ordinary transparents.
[RequireComponent(typeof(Interactable_Select))]
[RequireComponent(typeof(MeshRenderer))]
public class Marker_Interactable : MonoBehaviour
{
    bool Activated;
    MeshRenderer MeshRendererReference;

    public string Marker_Id;

    [SerializeField] private float FadeDuration = 0.25f;
    [SerializeField] private float PulseSpeed = 2f;
    [Tooltip("How far the pulse dips, as a fraction of Visible Alpha (0.3 = dips to 70%).")]
    [SerializeField] private float PulseAmount = 0.3f;

    [Tooltip("Opacity at the top of the pulse. Overrides the material's own alpha (Green_Glass is 0.34, too faint to spot across the room).")]
    [SerializeField, Range(0.05f, 1f)] private float VisibleAlpha = 0.3f;

    [Tooltip("Draw order among transparent objects — higher draws later, so the marker stays steady behind the rack's glass instead of flickering.")]
    [SerializeField] private int SortingOrder = 10;

    public event Action<Marker_Interactable, InteractionType> Selected;
    public UnityEvent OnSelected; // wire per-marker actions (e.g. play an animation) here in the Inspector

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock block;
    private Coroutine fadeRoutine;
    private Coroutine idleRoutine;
    private bool initialised;

    // True after this marker was clicked (and hid itself) until something
    // shows it again — so re-activating a parent doesn't bring a finished
    // marker back.
    private bool hiddenByClick;

    // Lazy, because SequenceManager/Quiz_Manager may call SetVisible on a
    // marker the same frame they first activate it — before its Start().
    private void Init()
    {
        if (initialised)
            return;
        initialised = true;

        MeshRendererReference = GetComponent<MeshRenderer>();
        MeshRendererReference.sortingOrder = SortingOrder;
        block = new MaterialPropertyBlock();

        Interactable_Select select = GetComponent<Interactable_Select>();
        select.OnInteractBegin.AddListener(() => ToggleVisibility(InteractionType.Interact));
    }

    private void Awake()
    {
        Init();
    }

    public void Start()
    {
        Init();
    }

    private void OnEnable()
    {
        Init();
        MeshRendererReference.enabled = !hiddenByClick;
        if (!hiddenByClick)
            RestartPulse();
    }

    private void OnDisable()
    {
        // Unity stops the coroutines itself; forget the handles so they're
        // not stopped twice or mistaken for running ones.
        fadeRoutine = null;
        idleRoutine = null;
    }

    // Restores/hides visibility directly, bypassing the click-driven toggle —
    // used by quiz mode to un-hide a marker that auto-hid itself after a
    // wrong-order click, and by SequenceManager when a marker's step starts.
    public void SetVisible(bool visible)
    {
        Init();
        StopRoutines();

        hiddenByClick = !visible;
        MeshRendererReference.enabled = visible;

        if (visible)
            RestartPulse();
    }

    public void ToggleVisibility(InteractionType interactionType)
    {
        Init();
        Activated = !Activated;

        StopRoutines();

        hiddenByClick = true;
        MeshRendererReference.enabled = false;

        Selected?.Invoke(this, interactionType);
        OnSelected?.Invoke();
    }

    private void StopRoutines()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        if (idleRoutine != null)
            StopCoroutine(idleRoutine);
        fadeRoutine = null;
        idleRoutine = null;
    }

    private void RestartPulse()
    {
        StopRoutines();
        ApplyAlpha(VisibleAlpha); // visible at full strength right away, not from a stale value

        // A coroutine can't start on an inactive object — OnEnable restarts
        // it when the marker is activated.
        if (isActiveAndEnabled)
            idleRoutine = StartCoroutine(IdlePulse());
    }

    private IEnumerator FadeOut()
    {
        yield return FadeTo(0f);
        fadeRoutine = null;
    }

    private IEnumerator FadeInThenIdle()
    {
        yield return FadeTo(VisibleAlpha);
        fadeRoutine = null;
        idleRoutine = StartCoroutine(IdlePulse());
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        MeshRendererReference.enabled = true;

        float startAlpha = CurrentAlpha();
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

    // Gentle breathing pulse between Visible Alpha and a floor that's still
    // clearly visible, so the marker keeps drawing the eye without vanishing.
    private IEnumerator IdlePulse()
    {
        float low = VisibleAlpha * (1f - Mathf.Clamp01(PulseAmount));

        while (true)
        {
            float wave = (Mathf.Sin(Time.time * PulseSpeed) + 1f) * 0.5f;
            ApplyAlpha(Mathf.Lerp(VisibleAlpha, low, wave));
            yield return null;
        }
    }

    private float CurrentAlpha()
    {
        MeshRendererReference.GetPropertyBlock(block);
        Color color = block.GetColor(BaseColorId);
        return color == default ? MeshRendererReference.sharedMaterial.color.a : color.a;
    }

    private void ApplyAlpha(float alpha)
    {
        Color color = MeshRendererReference.sharedMaterial.color;
        color.a = alpha;
        block.SetColor(BaseColorId, color);
        MeshRendererReference.SetPropertyBlock(block);
    }
}
