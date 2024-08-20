using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace ScaleSokoban{
    public class LevelSelector : Selectable, ISubmitHandler
    {

        public TMP_Text text;

        protected override void OnEnable()
        {
            base.OnEnable();
            Select();
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    GameManager.Instance.SelectLevel(-1);
                    AudioManager.Instance.PlaySfx(AudioManager.Instance.Select);
                    break;
                case MoveDirection.Right:
                    GameManager.Instance.SelectLevel(1);
                    AudioManager.Instance.PlaySfx(AudioManager.Instance.Select);
                    break;
                default:
                    base.OnMove(eventData);
                    break;

            }
        }

        public void UpdateText(int level,bool first,bool last){
            string center=$"level {level+1:00}";
            string left=first?"   ":"<  ";
            string right=last?"   ":"  >";
            text.text=left+center+right;
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            GameManager.Instance.EnterLevel();
        }
    }
}