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
