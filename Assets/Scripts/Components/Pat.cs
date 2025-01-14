using BA2LW.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BA2LW.Components
{
    [AddComponentMenu("BA2LW/Components/Pat")]
    public class Pat : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        MainControl control;

        private void Awake()
        {
            control = FindFirstObjectByType<MainControl>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            control.Patting(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            control.Patting(false);
        }
    }
}
