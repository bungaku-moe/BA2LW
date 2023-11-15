using BA2LW.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BA2LW.Components
{
    [AddComponentMenu("BA2LW/Components/Look")]
    public class Look : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        MainControl control;

        void Awake()
        {
            control = FindObjectOfType<MainControl>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            control.SetLooking(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            control.SetLooking(false);
        }
    }
}
