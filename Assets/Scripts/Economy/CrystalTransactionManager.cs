using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if UGS_CLOUDCODE_ENABLED
using Unity.Services.CloudCode;
#endif

namespace DreamDungeon
{
    [Serializable]
    public sealed class CrystalProductDefinition
    {
        public string productId;
        public string displayName;
        public int crystalAmount;
        public string displayPrice;
    }

    public struct PurchaseTransactionResult
    {
        public bool Success;
        public AuthErrorCode ErrorCode;
        public string Message;
        public CrystalPurchaseRecord Record;
        public bool ShouldConfirmWithStore;
    }

    public sealed class CrystalTransactionManager : MonoBehaviour
    {
        public static CrystalTransactionManager Instance { get; private set; }

        public static readonly CrystalProductDefinition[] Catalog =
        {
            new CrystalProductDefinition { productId = "crystal_100",  displayName = "Handful of Crystals",  crystalAmount = 100,  displayPrice = "$0.99" },
            new CrystalProductDefinition { productId = "crystal_550",  displayName = "Pouch of Crystals",    crystalAmount = 550,  displayPrice = "$4.99" },
            new CrystalProductDefinition { productId = "crystal_1200", displayName = "Chest of Crystals",    crystalAmount = 1200, displayPrice = "$9.99" },
            new CrystalProductDefinition { productId = "crystal_2500", displayName = "Vault of Crystals",    crystalAmount = 2500, displayPrice = "$19.99" },
            new CrystalProductDefinition { productId = "crystal_5500", displayName = "Dragon Hoard",         crystalAmount = 5500, displayPrice = "$39.99" }
        };

        private readonly List<PendingPurchaseItem> pendingPurchases = new List<PendingPurchaseItem>();
        private bool isProcessingPurchase;

        [Serializable]
        private sealed class PendingPurchaseItem
        {
            public string playerId;
            public string productId;
            public string transactionId;
            public string receipt;
            public string store;
        }

        [Serializable]
        private sealed class PendingPurchaseWrapper
        {
            public List<PendingPurchaseItem> items = new List<PendingPurchaseItem>();
        }

        private const string PendingQueueKey = "DD_Pending_IAP_Queue_Temp";

        public event Action<PurchaseTransactionResult> OnPurchaseCompleted;

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
            LoadPendingQueue();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static CrystalProductDefinition FindProduct(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId)) return null;
            string clean = productId.Trim();
            for (int i = 0; i < Catalog.Length; i++)
            {
                if (string.Equals(Catalog[i].productId, clean, StringComparison.OrdinalIgnoreCase))
                    return Catalog[i];
            }
            return null;
        }

        public bool HasProcessedTransaction(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId)) return false;
            var history = PlayerDatabaseManager.Instance?.CurrentData?.purchaseHistory;
            if (history == null) return false;

            string clean = transactionId.Trim();
            for (int i = 0; i < history.Count; i++)
            {
                if (history[i] != null &&
                    string.Equals(history[i].transactionId, clean, StringComparison.OrdinalIgnoreCase) &&
                    history[i].purchaseStatus == PurchaseVerificationStatus.Fulfilled)
                {
                    return true;
                }
            }
            return false;
        }

        public void EnqueuePendingPurchase(string productId, string transactionId, string receipt, string store = "GooglePlay")
        {
            string activePlayerId = AuthenticationManager.Instance != null ? AuthenticationManager.Instance.PlayerId : string.Empty;
            if (string.IsNullOrWhiteSpace(transactionId)) return;

            for (int i = 0; i < pendingPurchases.Count; i++)
            {
                if (string.Equals(pendingPurchases[i].transactionId, transactionId.Trim(), StringComparison.OrdinalIgnoreCase))
                    return;
            }

            pendingPurchases.Add(new PendingPurchaseItem
            {
                playerId = activePlayerId,
                productId = productId,
                transactionId = transactionId.Trim(),
                receipt = receipt ?? string.Empty,
                store = store
            });
            SavePendingQueue();
        }

        public async Task<int> RecoverPendingPurchasesAsync()
        {
            if (pendingPurchases.Count == 0) return 0;
            if (AuthenticationManager.Instance == null || !AuthenticationManager.Instance.IsSignedIn) return 0;

            int recovered = 0;
            var snapshot = new List<PendingPurchaseItem>(pendingPurchases);
            foreach (var item in snapshot)
            {
                if (!string.IsNullOrEmpty(item.playerId) &&
                    !string.Equals(item.playerId, AuthenticationManager.Instance.PlayerId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var result = await ProcessGooglePlayPurchaseAsync(item.productId, item.transactionId, item.receipt, item.store);
                if (result.Success || result.ErrorCode == AuthErrorCode.DuplicateTransaction)
                {
                    RemovePending(item.transactionId);
                    if (result.Success) recovered++;
                }
            }
            return recovered;
        }

        public async Task<PurchaseTransactionResult> ProcessGooglePlayPurchaseAsync(
            string productId,
            string transactionId,
            string googlePlayReceipt,
            string store = "GooglePlay")
        {
            if (isProcessingPurchase)
            {
                return Fail(AuthErrorCode.PurchasePending, "Another purchase is currently being verified.");
            }

            if (AuthenticationManager.Instance == null || !AuthenticationManager.Instance.IsSignedIn ||
                PlayerDatabaseManager.Instance == null || PlayerDatabaseManager.Instance.CurrentData == null)
            {
                return Fail(AuthErrorCode.AuthenticationFailed, "You must be signed in before purchasing Crystals.");
            }

            // 1. Validate Product ID
            CrystalProductDefinition product = FindProduct(productId);
            if (product == null)
            {
                return Fail(AuthErrorCode.InvalidProduct, $"Invalid Crystal product ID: '{productId}'.");
            }

            // 2. Validate Transaction ID & Receipt presence
            if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(googlePlayReceipt))
            {
                return Fail(AuthErrorCode.PurchaseFailed, "Purchase receipt or Transaction ID is missing.");
            }

            string cleanTxId = transactionId.Trim();

            // 3. Check Duplicate Transaction BEFORE crediting
            if (HasProcessedTransaction(cleanTxId))
            {
                return new PurchaseTransactionResult
                {
                    Success = false,
                    ErrorCode = AuthErrorCode.DuplicateTransaction,
                    Message = $"Transaction '{cleanTxId}' has already been fulfilled. Duplicate credit blocked.",
                    ShouldConfirmWithStore = true
                };
            }

            if (!PlayerDatabaseManager.Instance.HasNetworkConnection())
            {
                EnqueuePendingPurchase(product.productId, cleanTxId, googlePlayReceipt, store);
                return Fail(AuthErrorCode.NoInternet, "No internet connection. Purchase queued and will be recovered when online.");
            }

            isProcessingPurchase = true;
            try
            {
                // 4. Verify Receipt (Server-side Cloud Code when deployed, or validated receipt check in Sandbox/Editor)
                bool receiptValid = await VerifyReceiptWithServerAsync(product.productId, cleanTxId, googlePlayReceipt);
                if (!receiptValid)
                {
                    return Fail(AuthErrorCode.PurchaseFailed, "Google Play receipt verification failed.");
                }

                // 5. Credit Crystal to active Player ID
                CrystalManager crystalMgr = CrystalManager.Instance ?? FindAnyObjectByType<CrystalManager>() ?? gameObject.AddComponent<CrystalManager>();
                crystalMgr.CreditVerifiedPurchaseCrystals(product.crystalAmount);

                // 6. Record Purchase History in Player Database
                var record = new CrystalPurchaseRecord
                {
                    productId = product.productId,
                    transactionId = cleanTxId,
                    store = store,
                    purchaseDateUtc = DateTime.UtcNow.ToString("o"),
                    crystalAmount = product.crystalAmount,
                    purchaseStatus = PurchaseVerificationStatus.Fulfilled,
                    receiptHash = ComputeSha256(googlePlayReceipt)
                };

                var data = PlayerDatabaseManager.Instance.CurrentData;
                if (data.purchaseHistory == null) data.purchaseHistory = new List<CrystalPurchaseRecord>();
                data.purchaseHistory.Add(record);

                // 7. Persist updated Crystal Balance & Purchase History to Player Database
                var saveRes = await PlayerDatabaseManager.Instance.SaveCurrentPlayerDataAsync(captureFromGameSystems: false);
                if (!saveRes.Success)
                {
                    // Queue for recovery if cloud save transiently failed
                    EnqueuePendingPurchase(product.productId, cleanTxId, googlePlayReceipt, store);
                    return Fail(saveRes.ErrorCode, $"Purchase verified but Cloud Save failed: {saveRes.Message}");
                }

                RemovePending(cleanTxId);

                // 8. Confirm Purchase with Store (caller calls IStoreController.ConfirmPendingPurchase when ShouldConfirmWithStore == true)
                var successRes = new PurchaseTransactionResult
                {
                    Success = true,
                    ErrorCode = AuthErrorCode.None,
                    Message = $"Purchased +{product.crystalAmount:N0} Crystals ({product.productId})!",
                    Record = record,
                    ShouldConfirmWithStore = true
                };

                OnPurchaseCompleted?.Invoke(successRes);
                return successRes;
            }
            finally
            {
                isProcessingPurchase = false;
            }
        }

        private async Task<bool> VerifyReceiptWithServerAsync(string productId, string transactionId, string receipt)
        {
#if UGS_CLOUDCODE_ENABLED
            var args = new Dictionary<string, object>
            {
                { "productId", productId },
                { "transactionId", transactionId },
                { "receipt", receipt },
                { "store", "GooglePlay" }
            };
            var response = await CloudCodeService.Instance.CallEndpointAsync<Dictionary<string, object>>("VerifyGooglePlayReceipt", args);
            return response != null && response.TryGetValue("valid", out object v) && Convert.ToBoolean(v);
#else
            await Task.Yield();
            // Reject obviously forged/empty receipts; real production verification runs in UGS Cloud Code
            return !string.IsNullOrWhiteSpace(receipt) && receipt.Length >= 8;
#endif
        }

        private PurchaseTransactionResult Fail(AuthErrorCode code, string message)
        {
            var res = new PurchaseTransactionResult
            {
                Success = false,
                ErrorCode = code,
                Message = message,
                ShouldConfirmWithStore = false
            };
            OnPurchaseCompleted?.Invoke(res);
            return res;
        }

        private void RemovePending(string transactionId)
        {
            pendingPurchases.RemoveAll(p => string.Equals(p.transactionId, transactionId, StringComparison.OrdinalIgnoreCase));
            SavePendingQueue();
        }

        private void SavePendingQueue()
        {
            PlayerPrefs.SetString(PendingQueueKey, JsonUtility.ToJson(new PendingPurchaseWrapper { items = pendingPurchases }, false));
            PlayerPrefs.Save();
        }

        private void LoadPendingQueue()
        {
            pendingPurchases.Clear();
            string json = PlayerPrefs.GetString(PendingQueueKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                var wrap = JsonUtility.FromJson<PendingPurchaseWrapper>(json);
                if (wrap?.items != null) pendingPurchases.AddRange(wrap.items);
            }
        }

        private static string ComputeSha256(string raw)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw ?? string.Empty));
                var sb = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++) sb.Append(bytes[i].ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
