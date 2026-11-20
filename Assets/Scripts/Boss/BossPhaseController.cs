using System;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class BossPhaseController : MonoBehaviour
    {
        [Header("Phase Settings")]
        [SerializeField] private int _currentPhase = 1;
        [SerializeField] private float _phase2Threshold = 0.5f; // < 50% HP triggers Phase 2
        [SerializeField] private float _phase2AttackMultiplier = 1.35f;
        [SerializeField] private float _phase2AttackSpeedMultiplier = 1.25f;
        [SerializeField] private float _phase2CooldownMultiplier = 0.8f;

        public int CurrentPhase => _currentPhase;
        public bool IsEnraged => _currentPhase >= 2;
        public float AttackMultiplier => IsEnraged ? _phase2AttackMultiplier : 1.0f;
        public float AttackSpeedMultiplier => IsEnraged ? _phase2AttackSpeedMultiplier : 1.0f;
        public float CooldownMultiplier => IsEnraged ? _phase2CooldownMultiplier : 1.0f;

        public event Action<int> OnPhaseChanged;

        public void Initialize()
        {
            _currentPhase = 1;
        }

        public void CheckHealth(int currentHP, int maxHP)
        {
            if (maxHP <= 0) return;
            float hpPercent = (float)currentHP / maxHP;

            if (_currentPhase == 1 && hpPercent < _phase2Threshold)
            {
                SetPhase(2);
            }
        }

        public void SetPhase(int newPhase)
        {
            if (_currentPhase == newPhase) return;
            _currentPhase = newPhase;
            OnPhaseChanged?.Invoke(_currentPhase);
        }
    }
}
