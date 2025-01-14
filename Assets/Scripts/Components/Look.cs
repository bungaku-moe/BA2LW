using BA2LW.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BA2LW.Components
{
    [AddComponentMenu("BA2LW/Components/Look")]
    public class Look : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        MainControl control;

        private void Awake()
        {
            control = FindFirstObjectByType<MainControl>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            control.Looking(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            control.Looking(false);
        }
    }
}
