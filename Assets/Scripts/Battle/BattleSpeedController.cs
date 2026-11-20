using System;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class BattleSpeedController : MonoBehaviour
    {
        private float currentSpeed = 1f;
        private bool isPaused;

        public float Speed => currentSpeed;
        public bool IsPaused => isPaused;

        public event Action<float> SpeedChanged;
        public event Action<bool> PauseChanged;

        private void Awake()
        {
            currentSpeed = 1f;
            isPaused = false;
            Time.timeScale = 1f;
        }

        public void CycleSpeed()
        {
            if (currentSpeed >= 2.9f)
            {
                currentSpeed = 1f;
            }
            else
            {
                currentSpeed += 1f;
            }

            if (!isPaused)
            {
                Time.timeScale = currentSpeed;
            }

            SpeedChanged?.Invoke(currentSpeed);
        }

        public void SetSpeed(float speed)
        {
            currentSpeed = Mathf.Clamp(speed, 1f, 3f);
            if (!isPaused)
            {
                Time.timeScale = currentSpeed;
            }
            SpeedChanged?.Invoke(currentSpeed);
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
            Time.timeScale = isPaused ? 0f : currentSpeed;
            PauseChanged?.Invoke(isPaused);
        }

        public void TogglePause()
        {
            SetPaused(!isPaused);
        }

        public void ResetSpeed()
        {
            currentSpeed = 1f;
            isPaused = false;
            Time.timeScale = 1f;
            SpeedChanged?.Invoke(currentSpeed);
            PauseChanged?.Invoke(false);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
