using BA2LW.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BA2LW.Components
{
    [AddComponentMenu("BA2LW/Components/Talk")]
    public class Talk : MonoBehaviour, IPointerClickHandler
    {
        MainControl control;

        void Awake()
        {
            control = FindObjectOfType<MainControl>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            control.SetTalking();
        }
    }
}
