using UnityEngine;

namespace Survivor.Input
{
    public sealed class MovementGate
    {
        private bool waitingForRelease = true;
        public void Block() { waitingForRelease = true; }
        public Vector2 Read(bool battle, bool anyMovementHeld, Vector2 direction)
        {
            if (!battle) { Block(); return Vector2.zero; }
            if (waitingForRelease)
            {
                if (!anyMovementHeld) waitingForRelease = false;
                return Vector2.zero;
            }
            return Vector2.ClampMagnitude(direction, 1);
        }
    }
}
