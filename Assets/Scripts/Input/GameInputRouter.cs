using UnityEngine;
using UnityEngine.InputSystem;

namespace Survivor.Input
{
    public sealed class GameInputRouter : System.IDisposable
    {
        private readonly InputActionAsset actions;
        private readonly InputAction move;
        private readonly InputAction pause;
        private readonly MovementGate gate = new MovementGate();
        public GameInputRouter(InputActionAsset template)
        {
            actions = Object.Instantiate(template);
            move = actions.FindAction("Gameplay/Move", true);
            pause = actions.FindAction("Global/Pause", true);
            actions.Enable();
        }
        public bool PausePressed => pause.WasPressedThisFrame();
        public int UpgradePressed
        {
            get
            {
                var keyboard = Keyboard.current;
                if (keyboard == null) return -1;
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) return 0;
                if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) return 1;
                if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) return 2;
                return -1;
            }
        }
        public void BlockMovement() => gate.Block();
        public Vector2 ReadMovement(bool battle)
        {
            var keyboard = Keyboard.current;
            bool held = keyboard != null && (keyboard.wKey.isPressed || keyboard.aKey.isPressed || keyboard.sKey.isPressed || keyboard.dKey.isPressed);
            return gate.Read(battle, held, move.ReadValue<Vector2>());
        }
        public void Dispose() { actions.Disable(); Object.Destroy(actions); }
    }
}
