using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Survivor.UI
{
    public sealed class AbilityTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action Enter, Exit;
        public void OnPointerEnter(PointerEventData data) => Enter?.Invoke();
        public void OnPointerExit(PointerEventData data) => Exit?.Invoke();
        private void OnDisable() => Exit?.Invoke();
    }
}
