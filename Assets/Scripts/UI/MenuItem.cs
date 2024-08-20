using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuItem : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public TMP_Text Text;
    public bool Default;
    string label;
    private void Awake()
    {
        label=Text.text;
    }
    void OnEnable()
    {
        if(Default){
            GetComponent<Selectable>().Select();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        Text.text=">  "+label+"  <";
    }
    public void OnDeselect(BaseEventData eventData)
    {
        Text.text=label;
    }
}
