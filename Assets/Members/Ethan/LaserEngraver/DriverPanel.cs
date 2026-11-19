using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DriverPanel : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject zSetPanel;
    public GameObject imagePanel;
    public JobLaserEngrave managedJob;
    public List<Texture2D> images;
    [Tooltip("Printed image width in inches.")]
    public float jobWidthInches = 1f;
    public TextMeshProUGUI operationModeText;

    [SerializeField] Transform imageContent;
    [SerializeField] GameObject imageButtonPrefab; // Button + RawImage
    [SerializeField] LaserEngraver engraver;

    [Tooltip("Sends the selected image to the laser.")]
    [SerializeField] Button downloadButton;
    [SerializeField] Color selectedTint = new Color(0.55f, 1f, 0.6f, 1f);

    bool built;
    int selectedIndex = -1;

    readonly List<Sprite> runtimeSprites = new();
    readonly List<(int index, Image image)> imageButtons = new();

    public int SelectedIndex => selectedIndex;

    public void OnEnable()
    {
        managedJob = GetComponentInChildren<JobLaserEngrave>();
        Refresh_Download_Button();
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
        imageButtons.Clear();

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

            imageButtons.Add((index, img));
            go.GetComponent<Button>().onClick.AddListener(() => OnImageClicked(index));
        }
        built = true;
        Refresh_Selection_Tint();
    }

    void OnImageClicked(int index)
    {
        selectedIndex = index;
        Refresh_Selection_Tint();
        Refresh_Download_Button();
    }

    public void Clear_Selection()
    {
        selectedIndex = -1;
        Refresh_Selection_Tint();
        Refresh_Download_Button();
    }

    // Builds the engrave mask from the selected image and downloads it to the
    // laser. Returns false if it fails.
    public bool Download_Selected()
    {
        if (selectedIndex < 0 || selectedIndex >= images.Count || images[selectedIndex] == null)
        {
            Debug.LogWarning("[DriverPanel] Download_Selected: no image selected.", this);
            return false;
        }

        if (managedJob == null)
            managedJob = GetComponentInChildren<JobLaserEngrave>();

        if (managedJob == null)
        {
            Debug.LogWarning("[DriverPanel] Download_Selected: no JobLaserEngrave found under the panel.", this);
            return false;
        }

        // TODO: Add height/width settings for the print job
        managedJob.Image2Engrave = EngraveMask.FromImage(images[selectedIndex], jobWidthInches, 100);
        managedJob.DownloadJob();
        // Stays on the image panel
        return true;
    }

    public void Set_Operation_Mode(string mode)
    {
        if (operationModeText != null)
            operationModeText.text = mode;
    }

    void Refresh_Selection_Tint()
    {
        foreach (var (index, image) in imageButtons)
            if (image != null)
                image.color = index == selectedIndex ? selectedTint : Color.white;
    }

    void Refresh_Download_Button()
    {
        if (downloadButton != null)
            downloadButton.interactable = selectedIndex >= 0;
    }
}
