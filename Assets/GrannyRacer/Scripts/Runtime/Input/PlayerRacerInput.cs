using UnityEngine;
using UnityEngine.InputSystem;

namespace GrannyRacer.Input
{
    public sealed class PlayerRacerInput : MonoBehaviour, IRacerInputSource
    {
        private InputActionMap actions;
        private InputAction throttle;
        private InputAction brake;
        private InputAction steer;
        private InputAction boost;
        private InputAction jump;
        private InputAction reset;
        private InputAction pause;
        private InputAction leftAttack;
        private InputAction rightAttack;

        public RacerInputState Sample()
        {
            return new RacerInputState(
                throttle.ReadValue<float>(),
                brake.ReadValue<float>(),
                steer.ReadValue<float>(),
                boost.IsPressed(),
                boost.WasPressedThisFrame(),
                jump.WasPressedThisFrame(),
                jump.IsPressed(),
                reset.WasPressedThisFrame(),
                pause.WasPressedThisFrame(),
                leftAttack.WasPressedThisFrame(),
                rightAttack.WasPressedThisFrame());
        }

        private void Awake()
        {
            actions = new InputActionMap("Racer");
            throttle = actions.AddAction("Throttle", InputActionType.Value);
            throttle.AddBinding("<Keyboard>/w");
            throttle.AddBinding("<Keyboard>/upArrow");
            throttle.AddBinding("<Gamepad>/rightTrigger");

            brake = actions.AddAction("Brake", InputActionType.Value);
            brake.AddBinding("<Keyboard>/s");
            brake.AddBinding("<Keyboard>/downArrow");
            brake.AddBinding("<Gamepad>/leftTrigger");

            steer = actions.AddAction("Steer", InputActionType.Value);
            steer.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            steer.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            steer.AddBinding("<Gamepad>/leftStick/x");

            boost = CreateButton("Boost", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            jump = CreateButton("Jump", "<Keyboard>/leftShift", "<Gamepad>/rightShoulder");
            reset = CreateButton("Reset", "<Keyboard>/r", "<Gamepad>/buttonNorth");
            pause = CreateButton("Pause", "<Keyboard>/escape", "<Gamepad>/start");
            leftAttack = CreateButton("Left Attack (Reserved)", "<Keyboard>/q", "<Gamepad>/buttonWest");
            rightAttack = CreateButton("Right Attack (Reserved)", "<Keyboard>/e", "<Gamepad>/buttonEast");
        }

        private void OnEnable()
        {
            actions?.Enable();
        }

        private void OnDisable()
        {
            actions?.Disable();
        }

        private void OnDestroy()
        {
            actions?.Dispose();
        }

        private InputAction CreateButton(string name, string keyboardBinding, string gamepadBinding)
        {
            var action = actions.AddAction(name, InputActionType.Button);
            action.AddBinding(keyboardBinding);
            action.AddBinding(gamepadBinding);
            return action;
        }
    }
}
