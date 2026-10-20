using UnityEngine;
using UnityEngine.Video;

// Data for one station type (Carousel, ASRS-36, ASRS-36u, Rack) taught in
// Module 1's intro. Kept as an asset so the wording can be edited in the
// Inspector without touching the scene — SequenceManager reads it for any
// step whose Station Type field is set, and pages it through the existing
// info panel (summary first, details on Next).
[CreateAssetMenu(fileName = "StationType_", menuName = "ASRS/Station Type")]
public class ASRS_Station_Type : ScriptableObject
{
    [Tooltip("Shown in bold as the heading of both pages, e.g. \"ASRS-36u\".")]
    public string displayName;

    [Tooltip("First page: what it is and how it stores parts. Keep it short — the info panel fits roughly 230 characters.")]
    [TextArea(3, 6)]
    public string summary;

    [Tooltip("Second page, revealed by Next: characteristics and its role in the CIM cell. Leave blank for a single-page station.")]
    [TextArea(3, 6)]
    public string details;

    [Header("Optional media")]
    [Tooltip("Optional picture shown on the Station_Type_Card while this station is on screen.")]
    public Sprite image;

    [Tooltip("Optional short clip looped on the Station_Type_Card while this station is on screen. Shown in place of the image when both are set.")]
    public VideoClip video;

    [Tooltip("Play the video's own soundtrack. Off by default so it doesn't talk over narration.")]
    public bool playVideoAudio;

    [Tooltip("Optional prefab spawned at the Station_Type_Card's preview anchor while this station is on screen. Leave empty if no model exists.")]
    public GameObject previewPrefab;

    [Tooltip("Optional narration, played when this station's step begins. Not required.")]
    public AudioClip narration;
}
