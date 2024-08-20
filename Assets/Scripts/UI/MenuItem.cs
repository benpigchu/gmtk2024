using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuItem : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public TMP_Text Text;
    public bool Default;
    private Selectable selectable;
    string label;
    private void Awake()
    {
        label=Text.text;
        selectable=GetComponent<Selectable>();
    }
    void OnEnable()
    {
        if(Default){
            selectable.Select();
        }
    }
    void OnDisable(){
        Text.text=label;
    }

    public void OnSelect(BaseEventData eventData)
    {
        Text.text=">  "+label+"  <";
    }
    public void OnDeselect(BaseEventData eventData)
    {
        Text.text=label;
        if (!selectable.IsActive()){
            return;
        }
        AudioManager.Instance.PlaySfx(AudioManager.Instance.Select);
    }
}
