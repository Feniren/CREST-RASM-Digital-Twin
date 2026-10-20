using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// The one shared visual for every station-type step — SequenceManager calls
// Show() with whichever ASRS_Station_Type the current step references, and
// Hide() on any other step. The text itself still goes through the existing
// info panel; this only handles the optional extras (video or picture,
// preview prefab). With a Media Root assigned (the Lesson Panel's media
// box), station steps always show the box — the video if there is one,
// else the image, else the "coming soon" placeholder.
public class Station_Type_Card : MonoBehaviour
{
    [Tooltip("Optional: the whole media box. Shown on station steps (even with no media yet, so the placeholder appears) and hidden on every other step — on the Lesson Panel this lets the body text widen to fill the space.")]
    [SerializeField] private GameObject mediaRoot;

    [Tooltip("Optional: shown inside the media box when the station has neither an image nor a video yet.")]
    [SerializeField] private GameObject placeholder;

    [Tooltip("Optional: the Image that displays ASRS_Station_Type.image. Hidden when the station has no image, or when its video is playing instead.")]
    [SerializeField] private Image image;

    [Tooltip("Optional: keeps Image at the sprite's own aspect ratio, so rounded corners sit on the picture's edge rather than on empty letterbox space.")]
    [SerializeField] private AspectRatioFitter imageAspect;

    [Tooltip("Optional: the RawImage the station's video is drawn onto. Hidden when the station has no video.")]
    [SerializeField] private RawImage videoSurface;

    [Tooltip("Optional: plays ASRS_Station_Type.video into a RenderTexture shown on Video Surface.")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Tooltip("Optional: keeps Video Surface at the clip's own aspect ratio inside the card.")]
    [SerializeField] private AspectRatioFitter videoAspect;

    [Tooltip("Optional: where ASRS_Station_Type.previewPrefab is spawned. Nothing is spawned when this or the prefab is empty.")]
    [SerializeField] private Transform previewAnchor;

    private GameObject spawnedPreview;
    private RenderTexture videoTexture;

    private void Awake()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        }

        Hide();
    }

    private void OnDestroy()
    {
        ReleaseVideoTexture();
    }

    public void Show(ASRS_Station_Type stationType)
    {
        Hide();

        if (stationType == null)
            return;

        if (mediaRoot != null)
            mediaRoot.SetActive(true);

        bool showingMedia = PlayVideo(stationType);

        if (!showingMedia && image != null && stationType.image != null)
        {
            Sprite sprite = stationType.image;
            image.sprite = sprite;
            image.preserveAspect = true;

            if (imageAspect != null && sprite.rect.height > 0f)
                imageAspect.aspectRatio = sprite.rect.width / sprite.rect.height;

            image.gameObject.SetActive(true);
            showingMedia = true;
        }

        if (placeholder != null)
            placeholder.SetActive(!showingMedia);

        if (previewAnchor != null && stationType.previewPrefab != null)
            spawnedPreview = Instantiate(stationType.previewPrefab, previewAnchor, false);
    }

    // The media box with just its placeholder — for steps that keep the box
    // on screen but have no station media of their own.
    public void ShowEmpty()
    {
        Hide();

        if (mediaRoot != null)
            mediaRoot.SetActive(true);

        if (placeholder != null)
            placeholder.SetActive(true);
    }

    public void Hide()
    {
        if (image != null)
            image.gameObject.SetActive(false);

        if (videoPlayer != null)
            videoPlayer.Stop();

        if (videoSurface != null)
            videoSurface.gameObject.SetActive(false);

        if (placeholder != null)
            placeholder.SetActive(false);

        if (mediaRoot != null)
            mediaRoot.SetActive(false);

        if (spawnedPreview != null)
        {
            Destroy(spawnedPreview);
            spawnedPreview = null;
        }
    }

    private bool PlayVideo(ASRS_Station_Type stationType)
    {
        VideoClip clip = stationType.video;
        if (clip == null || videoPlayer == null || videoSurface == null)
            return false;

        // One texture, resized only when a clip with different dimensions
        // comes along.
        int width = Mathf.Max(1, (int)clip.width);
        int height = Mathf.Max(1, (int)clip.height);

        if (videoTexture == null || videoTexture.width != width || videoTexture.height != height)
        {
            ReleaseVideoTexture();
            videoTexture = new RenderTexture(width, height, 0);
            videoTexture.Create();
        }

        if (videoAspect != null)
            videoAspect.aspectRatio = (float)width / height;

        videoPlayer.clip = clip;
        videoPlayer.targetTexture = videoTexture;
        videoPlayer.audioOutputMode = stationType.playVideoAudio ? VideoAudioOutputMode.Direct : VideoAudioOutputMode.None;

        videoSurface.texture = videoTexture;
        videoSurface.gameObject.SetActive(true);
        videoPlayer.Play();
        return true;
    }

    private void ReleaseVideoTexture()
    {
        if (videoTexture == null)
            return;

        if (videoPlayer != null && videoPlayer.targetTexture == videoTexture)
            videoPlayer.targetTexture = null;

        videoTexture.Release();
        Destroy(videoTexture);
        videoTexture = null;
    }
}
