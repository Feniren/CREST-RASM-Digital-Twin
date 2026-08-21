using UnityEngine;
using UnityEngine.EventSystems;

public class JogHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private JogControlManager controlManager;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("pressed down");
        controlManager.SetJogStatus(this.gameObject.name, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("no longer pressing down");
        controlManager.SetJogStatus("no direction", false);
        // controlManager.SetJogStatus(0, false);
    }
}
