using System;
using System.Threading.Tasks;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class CrystalManager : MonoBehaviour
    {
        public static CrystalManager Instance { get; private set; }

        private int crystalBalance;
        private int totalCrystalPurchased;
        private int totalCrystalSpent;

        public int CrystalBalance => crystalBalance;
        public int TotalCrystalPurchased => totalCrystalPurchased;
        public int TotalCrystalSpent => totalCrystalSpent;

        public event Action<int> OnCrystalBalanceChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (transform.parent == null && gameObject.GetComponents<Component>().Length <= 2)
                    Destroy(gameObject);
                else
                    Destroy(this);
                return;
            }
            Instance = this;
            if (transform.parent == null && gameObject.GetComponents<Component>().Length <= 2)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SyncFromPlayerDatabase(int balance, int totalPurchased, int totalSpent)
        {
            crystalBalance = Mathf.Max(0, balance);
            totalCrystalPurchased = Mathf.Max(0, totalPurchased);
            totalCrystalSpent = Mathf.Max(0, totalSpent);

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.SetCrystal(crystalBalance);
            }

            OnCrystalBalanceChanged?.Invoke(crystalBalance);
        }

        public void ResetInMemory()
        {
            crystalBalance = 0;
            totalCrystalPurchased = 0;
            totalCrystalSpent = 0;
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.SetCrystal(0);
            }
            OnCrystalBalanceChanged?.Invoke(0);
        }

        internal void CreditVerifiedPurchaseCrystals(int amount)
        {
            if (amount <= 0) return;
            crystalBalance = (int)Math.Min(int.MaxValue, (long)crystalBalance + amount);
            totalCrystalPurchased = (int)Math.Min(int.MaxValue, (long)totalCrystalPurchased + amount);

            if (PlayerDatabaseManager.Instance?.CurrentData?.currencyData != null)
            {
                var cur = PlayerDatabaseManager.Instance.CurrentData.currencyData;
                cur.crystalBalance = crystalBalance;
                cur.totalCrystalPurchased = totalCrystalPurchased;
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.SetCrystal(crystalBalance);
            }

            OnCrystalBalanceChanged?.Invoke(crystalBalance);
        }

        public async Task<bool> TrySpendCrystalAsync(int amount, string reason = "")
        {
            if (amount <= 0 || crystalBalance < amount) return false;

            crystalBalance -= amount;
            totalCrystalSpent = (int)Math.Min(int.MaxValue, (long)totalCrystalSpent + amount);

            if (PlayerDatabaseManager.Instance?.CurrentData?.currencyData != null)
            {
                var cur = PlayerDatabaseManager.Instance.CurrentData.currencyData;
                cur.crystalBalance = crystalBalance;
                cur.totalCrystalSpent = totalCrystalSpent;
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.SetCrystal(crystalBalance);
            }

            OnCrystalBalanceChanged?.Invoke(crystalBalance);

            if (PlayerDatabaseManager.Instance != null && PlayerDatabaseManager.Instance.CurrentData != null)
            {
                await PlayerDatabaseManager.Instance.SaveCurrentPlayerDataAsync(captureFromGameSystems: false);
            }
            return true;
        }
    }
}
