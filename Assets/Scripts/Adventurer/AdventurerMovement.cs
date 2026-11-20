using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class AdventurerMovement : MonoBehaviour
    {
        private List<Vector2Int> path;
        private Func<Vector2Int, Vector3> cellToWorldFunc;
        private int currentPathIndex;
        private float baseSpeed = 1f;
        private Vector3 laneOffset;
        private float slowUntil;
        private float slowMultiplier = 1f;

        public int CurrentPathIndex => currentPathIndex;
        public bool HasReachedEnd => path == null || currentPathIndex >= path.Count;
        public float EffectiveSpeed => baseSpeed * (slowUntil > Time.time ? Mathf.Max(0.25f, slowMultiplier) : 1f);

        public event Action ReachedCore;

        public void Configure(List<Vector2Int> route, Func<Vector2Int, Vector3> worldConverter, float moveSpeed, Vector3 offset)
        {
            path = route != null ? new List<Vector2Int>(route) : new List<Vector2Int>();
            cellToWorldFunc = worldConverter;
            baseSpeed = Mathf.Max(0.1f, moveSpeed);
            laneOffset = offset;
            currentPathIndex = 0;
            slowUntil = 0f;
            slowMultiplier = 1f;
        }

        public void ApplySlow(float slowPercent, float duration)
        {
            slowMultiplier = Mathf.Min(slowMultiplier, Mathf.Clamp01(1f - slowPercent));
            slowUntil = Mathf.Max(slowUntil, Time.time + Mathf.Max(0.1f, duration));
        }

        public void TickMovement(float deltaTime)
        {
            if (path == null || path.Count == 0 || cellToWorldFunc == null) return;
            if (currentPathIndex >= path.Count)
            {
                ReachedCore?.Invoke();
                return;
            }

            Vector3 targetCellWorld = cellToWorldFunc(path[currentPathIndex]);
            Vector3 destination = targetCellWorld + laneOffset;

            transform.position = Vector3.MoveTowards(transform.position, destination, EffectiveSpeed * deltaTime);

            if (Vector2.Distance(transform.position, destination) < 0.02f)
            {
                currentPathIndex++;
                if (currentPathIndex >= path.Count)
                {
                    ReachedCore?.Invoke();
                }
            }
        }

        public void ResetPath(int startIndex = 0)
        {
            currentPathIndex = startIndex;
        }
    }
}
