using UnityEngine;
using UnityEngine.InputSystem;

namespace BA2LW.Core
{
    [AddComponentMenu("BA2LW/Core/Input Manager")]
    [RequireComponent(typeof(PlayerInput))]
    public class InputManager : MonoBehaviour
    {
        public Vector2 PointerPosition { get; private set; }

        private InputSettings inputSettings;

        private void OnEnable()
        {
            inputSettings.Enable();
        }

        private void OnDisable()
        {
            inputSettings.Disable();
        }

        private void Awake()
        {
            inputSettings = new InputSettings();
        }

        private void Update()
        {
            PointerPosition = inputSettings.UI.PointerPosition.ReadValue<Vector2>();
        }
    }
}
