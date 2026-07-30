using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DriverPanel : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject zSetPanel;
    public GameObject imagePanel;
    public List<Texture2D> images;

    [SerializeField] Transform imageContent;
    [SerializeField] GameObject imageButtonPrefab; // Button + RawImage
    [SerializeField] LaserEngraver engraver;

    bool built;

    readonly List<Sprite> runtimeSprites = new();

    public void Start()
    {
        // demo test
        ShowZSetPanel();
    }

    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        zSetPanel.SetActive(false);
        imagePanel.SetActive(false);
    }

    public void ShowZSetPanel()
    {
        mainPanel.SetActive(false);
        zSetPanel.SetActive(true);
        imagePanel.SetActive(false);
    }

    public void ShowImagePanel()
    {
        mainPanel.SetActive(false);
        zSetPanel.SetActive(false);
        imagePanel.SetActive(true);
        if (!built) BuildImageList();
    }

    void BuildImageList()
    {
        foreach (Transform c in imageContent) Destroy(c.gameObject);
        foreach (var s in runtimeSprites) Destroy(s);
        runtimeSprites.Clear();

        for (int i = 0; i < images.Count; i++)
        {
            int index = i;
            var tex = images[index];
            if (tex == null) continue;

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));
            runtimeSprites.Add(sprite);

            var go = Instantiate(imageButtonPrefab, imageContent);
            var img = go.GetComponentInChildren<Image>(true);
            img.sprite = sprite;
            img.preserveAspect = true;

            var rt = go.GetComponent<RectTransform>();
            Canvas.ForceUpdateCanvases();
            Debug.Log($"[{index}] img={img != null} sprite={img.sprite != null} " +
                      $"color={img.color} enabled={img.enabled} " +
                      $"rect={rt.rect.size} scale={rt.lossyScale} " +
                      $"active={go.activeInHierarchy} pos={rt.position}");

            go.GetComponent<Button>().onClick.AddListener(() => OnImageClicked(index));
        }
        built = true;
    }

    void OnImageClicked(int index)
    {
        // TODO: Add height/width settings for the print job
        engraver.DownloadJob(PrintJob.FromImage(images[index], 100, 100));
    }
}