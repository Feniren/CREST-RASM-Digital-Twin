using System.Collections.Generic;
using UnityEngine;

public class Marker_Registry : MonoBehaviour{
    [SerializeField] private Transform MarkersRoot;
    [SerializeField] private Lesson_Sequencer Sequencer;

    private readonly Dictionary<string, Component_Marker> markers = new Dictionary<string, Component_Marker>();

    public IReadOnlyCollection<Component_Marker> All => markers.Values;

    private void Awake(){
        Component_Marker[] found = MarkersRoot.GetComponentsInChildren<Component_Marker>(true);
        Debug.Log($"Marker_Registry: found {found.Length} Component_Marker(s) under '{MarkersRoot.name}'.", this);

        foreach (Component_Marker marker in found){
            markers[marker.Marker_Id] = marker;
            marker.Selected += Sequencer.Notify_Marker_Selected;
            Debug.Log($"Marker_Registry: wired '{marker.Marker_Id}' ({marker.name}).", marker);
        }
    }

    public Component_Marker Resolve(string id){
        return id != null && markers.TryGetValue(id, out Component_Marker marker) ? marker : null;
    }

    public void Set_All_Labels_Visible(bool visible){
        foreach (Component_Marker marker in markers.Values)
            marker.Set_Label_Visible(visible);
    }
}
