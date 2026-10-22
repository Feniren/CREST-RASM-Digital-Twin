using UnityEngine;
using UnityEngine.UI;

// Splits one software panel (SCORBASE / CNCBase) into tabs so only a small group
// of action buttons shows at a time, keeping the menu near the monitor's size.
// Every Startup_Action_Button stays in the hierarchy (hidden tabs are just
// SetActive(false)), so Action_Button_Registry still wires them and out-of-order
// clicks on a *visible* tab still register. In guided mode the registry calls
// Reveal() so the highlighted next step's tab is shown automatically.
public class Panel_Tab_Group : MonoBehaviour{
    [SerializeField] private Button[] Tabs;
    [SerializeField] private GameObject[] Contents;
    [SerializeField] private Color ActiveTab = new Color(0.22f, 0.45f, 0.75f, 1f);
    [SerializeField] private Color InactiveTab = new Color(0.13f, 0.16f, 0.22f, 1f);

    private void Awake(){
        for (int i = 0; i < Tabs.Length; i++){
            int index = i;
            Tabs[i].onClick.AddListener(() => Show(index));
        }

        Show(0);
    }

    public void Show(int index){
        for (int i = 0; i < Contents.Length; i++){
            if (Contents[i] != null)
                Contents[i].SetActive(i == index);

            if (i < Tabs.Length && Tabs[i] != null){
                Image image = Tabs[i].GetComponent<Image>();
                if (image != null)
                    image.color = i == index ? ActiveTab : InactiveTab;
            }
        }
    }

    // Shows the tab whose content holds the button with this action id. Used by the
    // guided highlight so the next step is never hidden behind another tab.
    public bool Reveal(string actionId){
        for (int i = 0; i < Contents.Length; i++){
            if (Contents[i] == null)
                continue;

            foreach (Startup_Action_Button button in Contents[i].GetComponentsInChildren<Startup_Action_Button>(true)){
                if (button.Action_Id == actionId){
                    Show(i);
                    return true;
                }
            }
        }

        return false;
    }

    public void Reset_To_First(){
        Show(0);
    }
}
