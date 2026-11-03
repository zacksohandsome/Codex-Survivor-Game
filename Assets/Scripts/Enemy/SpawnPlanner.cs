using UnityEngine;

namespace Survivor.Enemy
{
    public static class SpawnPlanner
    {
        public static bool Choose(Vector2 half, float radius, Vector2 player, Rect view, float safeDistance,
            int attempts, System.Random random, out Vector2 chosen)
        {
            chosen = Vector2.zero; float best = -1; bool foundOutside = false;
            for (int i = 0; i < attempts; i++)
            {
                float t = (float)random.NextDouble() * 2 - 1;
                Vector2 point;
                switch (i % 4)
                {
                    case 0: point = new Vector2(t * (half.x - radius), half.y - radius); break;
                    case 1: point = new Vector2(t * (half.x - radius), -half.y + radius); break;
                    case 2: point = new Vector2(half.x - radius, t * (half.y - radius)); break;
                    default: point = new Vector2(-half.x + radius, t * (half.y - radius)); break;
                }
                float distance = (point - player).sqrMagnitude;
                if (distance < safeDistance * safeDistance) continue;
                bool outside = !new Rect(view.xMin - radius, view.yMin - radius, view.width + radius * 2, view.height + radius * 2).Contains(point);
                if (foundOutside && !outside) continue;
                if ((outside && !foundOutside) || distance > best)
                { chosen = point; best = distance; foundOutside = outside; }
            }
            return best >= 0;
        }
    }
}
