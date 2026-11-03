using UnityEngine;

namespace Survivor.Player
{
    public sealed class ArenaCamera : MonoBehaviour
    {
        public static Vector3 Follow(Vector2 position, Vector2 arenaHalf, float halfHeight, float aspect)
        {
            float x = Mathf.Max(0, arenaHalf.x - halfHeight * aspect), y = Mathf.Max(0, arenaHalf.y - halfHeight);
            return new Vector3(Mathf.Clamp(position.x, -x, x), Mathf.Clamp(position.y, -y, y), -10);
        }
    }
}
