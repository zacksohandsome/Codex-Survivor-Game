using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Survivor.UI
{
    // A press belongs to the displayed choices. Rebinding while a pointer is held
    // must not turn its later release into a click on the next choices.
    public sealed class UpgradeChoiceButton : Button
    {
        private uint generation,pressedGeneration=uint.MaxValue;
        public void BindGeneration(uint value){generation=value;pressedGeneration=uint.MaxValue;}
        public override void OnPointerDown(PointerEventData data)
        {if(data.button==PointerEventData.InputButton.Left)pressedGeneration=generation;base.OnPointerDown(data);}
        public override void OnPointerClick(PointerEventData data)
        {
            if(pressedGeneration!=generation)return;
            pressedGeneration=uint.MaxValue;base.OnPointerClick(data);
        }
    }
}
