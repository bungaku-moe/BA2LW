using UnityEngine;
using UnityEngine.InputSystem;

namespace BA2LW.Core
{
    [AddComponentMenu("BA2LW/Core/Input Manager")]
    [RequireComponent(typeof(PlayerInput))]
    public class InputManager : MonoBehaviour
    {
        public Vector2 PointerPosition { get; private set; }

        InputSettings inputSettings;

        void OnEnable()
        {
            inputSettings.Enable();
        }

        void OnDisable()
        {
            inputSettings.Disable();
        }

        void Awake()
        {
            inputSettings = new InputSettings();
        }

        void Update()
        {
            PointerPosition = inputSettings.UI.PointerPosition.ReadValue<Vector2>();
        }
    }
}
