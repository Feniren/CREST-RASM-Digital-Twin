using System.Collections.Generic;
using UnityEngine;

// Single source of truth for which training modules exist. The editor tooling
// reads this to generate the Bootstrap menu (Training_Builder_Core), the build
// settings scene list, and the play-mode redirect (Training_Play_Redirect).
// Module builders register themselves here during their Build() — list order
// is menu order and build-settings order.
[CreateAssetMenu(fileName = "Training_Modules", menuName = "Training/Module Registry")]
public class Training_Module_Registry : ScriptableObject{
    [System.Serializable]
    public class Entry{
        public Lesson_Definition Lesson;
        public string Scene_Path;        // "Assets/Members/<name>/Scenes/<scene>.unity"
        public string Placeholder_Label; // used when Lesson is null: disabled "coming soon" menu button
    }

    public string Bootstrap_Scene_Path = "Assets/Members/Colin/Scenes/Bootstrap.unity";
    public List<Entry> Modules = new List<Entry>();
}
