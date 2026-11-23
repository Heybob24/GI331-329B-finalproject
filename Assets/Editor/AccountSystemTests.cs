using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace DreamDungeon.Tests
{
    public static class AccountSystemTests
    {
        [MenuItem("Dream Dungeon/Run Account & Player Database Tests")]
        public static async void RunAllTests()
        {
            int passed = 0;
            int total = 0;

            async Task RunAsync(string testName, Func<Task> testAction)
            {
                total++;
                try
                {
                    await testAction();
                    passed++;
                    Debug.Log($"[PASS] {testName}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FAIL] {testName}: {ex.Message}\n{ex.StackTrace}");
                }
            }

            Debug.Log("========== STARTING PLAYER DATABASE & AUTHENTICATION TEST SUITE (14 TESTS) ==========");

            // Clean test environment
            string testCloudDir = Path.Combine(Application.persistentDataPath, "DreamDungeonCloudDB");
            if (Directory.Exists(testCloudDir)) Directory.Delete(testCloudDir, true);
            PlayerPrefs.DeleteKey("DD_UGS_CachedSessionPlayerId");
            PlayerPrefs.DeleteKey("DD_UGS_CachedSessionLoginType");
            PlayerPrefs.DeleteKey("DD_Pending_IAP_Queue_Temp");
            PlayerPrefs.Save();

            var host = new GameObject("TestAccountHost");
            var auth = host.AddComponent<AuthenticationManager>();
            auth.EnsureCompanionManagers();
            var guestMgr = host.GetComponent<GuestLoginManager>();
            var googleMgr = host.GetComponent<GoogleLoginManager>();
            var linkMgr = host.GetComponent<AccountLinkManager>();
            var dbMgr = host.GetComponent<PlayerDatabaseManager>();
            var crystalMgr = host.GetComponent<CrystalManager>();
            var txMgr = host.GetComponent<CrystalTransactionManager>();

            string guestPlayerId = string.Empty;
            string googleTokenA = "TEST_GOOGLE_ID_TOKEN_USER_A_" + Guid.NewGuid().ToString("N");
            string googleTokenB = "TEST_GOOGLE_ID_TOKEN_USER_B_" + Guid.NewGuid().ToString("N");

            // 1. First launch Guest Login succeeds
            await RunAsync("Test_01_FirstLaunch_GuestLoginSucceeds", async () =>
            {
                var res = await guestMgr.ContinueAsGuestAsync();
                Assert(res.Success && auth.IsSignedIn, $"Guest login failed: {res.Message}");
                Assert(auth.ActiveProfile.loginType == AuthLoginType.Guest, "LoginType must be Guest");
                Assert(!string.IsNullOrEmpty(auth.PlayerId), "UGS PlayerId must be assigned");
                guestPlayerId = auth.PlayerId;
            });

            // 2. Reopen game -> Guest session restores same PlayerId (no new Guest created)
            await RunAsync("Test_02_ReopenGame_RestoresSameGuestSession", async () =>
            {
                await auth.LogoutAsync(clearCachedSession: false);
                Assert(!auth.IsSignedIn, "Should be signed out in memory");
                var res = await guestMgr.ContinueAsGuestAsync();
                Assert(res.Success && auth.PlayerId == guestPlayerId,
                    $"Expected restored PlayerId '{guestPlayerId}', got '{auth.PlayerId}'");
            });

            // 3 & 4. Build Dungeon -> Save -> Re-enter -> Dungeon Data persists
            await RunAsync("Test_03_04_SaveAndReload_DungeonDataPersists", async () =>
            {
                dbMgr.CurrentData.dungeonData.dungeonLevel = 4;
                dbMgr.CurrentData.dungeonData.dungeonCoreLevel = 3;
                dbMgr.CurrentData.dungeonData.dungeonLayoutJson = "{\"rooms\":[{\"roomId\":\"entrance\"},{\"roomId\":\"dungeon_core\"}]}";
                var saveRes = await dbMgr.SaveCurrentPlayerDataAsync(captureFromGameSystems: false);
                Assert(saveRes.Success, $"Cloud save failed: {saveRes.Message}");

                await auth.LogoutAsync(clearCachedSession: false);
                var relogin = await guestMgr.ContinueAsGuestAsync();
                Assert(relogin.Success, "Re-login failed");
                Assert(dbMgr.CurrentData.dungeonData.dungeonLevel == 4 && dbMgr.CurrentData.dungeonData.dungeonCoreLevel == 3,
                    "Dungeon Level and Core Level must persist");
                Assert(dbMgr.CurrentData.dungeonData.dungeonLayoutJson.Contains("dungeon_core"), "Dungeon Layout JSON must persist");
            });

            // 10, 11, 12. Purchase Crystal via Google Play IAP, verify Transaction ID, block Duplicate Transaction
            await RunAsync("Test_10_11_12_IAPCrystalPurchase_AndDuplicateTransactionPrevention", async () =>
            {
                string txId = "GPA.3311-9988-7766-10001";
                string receipt = "{\"Store\":\"GooglePlay\",\"TransactionID\":\"" + txId + "\",\"ProductId\":\"crystal_550\"}";

                var buyRes = await txMgr.ProcessGooglePlayPurchaseAsync("crystal_550", txId, receipt, "GooglePlay");
                Assert(buyRes.Success && buyRes.ShouldConfirmWithStore, $"Purchase should succeed: {buyRes.Message}");
                Assert(crystalMgr.CrystalBalance == 550, $"Expected 550 Crystals, got {crystalMgr.CrystalBalance}");
                Assert(txMgr.HasProcessedTransaction(txId), "Transaction ID must be recorded as fulfilled");

                // Duplicate transaction check
                var dupRes = await txMgr.ProcessGooglePlayPurchaseAsync("crystal_550", txId, receipt, "GooglePlay");
                Assert(!dupRes.Success && dupRes.ErrorCode == AuthErrorCode.DuplicateTransaction,
                    "Duplicate transaction must be rejected with DuplicateTransaction error");
                Assert(crystalMgr.CrystalBalance == 550, "Crystal balance must NOT increase on duplicate transaction");
            });

            // 5, 6, 7. Link Guest Account with Google -> PlayerId unchanged, Crystal Balance & Dungeon preserved
            await RunAsync("Test_05_06_07_LinkGuestToGoogle_PreservesPlayerIdCrystalsAndDungeon", async () =>
            {
                var linkRes = await linkMgr.LinkGuestWithGoogleIdTokenAsync(googleTokenA, "keeper.a@gmail.com");
                Assert(linkRes.Success, $"Link Google failed: {linkRes.Message}");
                Assert(auth.PlayerId == guestPlayerId, "Player ID MUST NOT change after linking Google");
                Assert(auth.ActiveProfile.loginType == AuthLoginType.GoogleLinked, "LoginType must become GoogleLinked");
                Assert(crystalMgr.CrystalBalance == 550, "Crystal balance must remain 550 after linking");
                Assert(dbMgr.CurrentData.dungeonData.dungeonLevel == 4, "Dungeon Data must remain intact after linking");
                Assert(dbMgr.CurrentData.purchaseHistory.Count == 1, "Purchase History must remain intact after linking");
            });

            // 8 & 9. Logout -> Sign in with Google on Device B -> Loads exact linked account & Crystals
            await RunAsync("Test_08_09_LogoutAndCrossDeviceGoogleLogin_LoadsExactData", async () =>
            {
                await auth.LogoutAsync(clearCachedSession: true);
                Assert(!auth.IsSignedIn && crystalMgr.CrystalBalance == 0, "Memory state must clear on logout");

                var loginDeviceB = await auth.SignInWithGoogleIdTokenAsync(googleTokenA, "keeper.a@gmail.com");
                Assert(loginDeviceB.Success, $"Device B Google login failed: {loginDeviceB.Message}");
                Assert(auth.PlayerId == guestPlayerId, "Device B must load the exact same UGS Player ID");
                Assert(crystalMgr.CrystalBalance == 550, "Device B must load 550 Crystals");
                Assert(dbMgr.CurrentData.dungeonData.dungeonLevel == 4, "Device B must load Dungeon Level 4");
            });

            // 13. Internet disconnected error handling
            await RunAsync("Test_13_OfflineErrorHandling_BlocksCloudSaveSafely", async () =>
            {
                dbMgr.SimulateNetworkOffline = true;
                var saveOffline = await dbMgr.SaveCurrentPlayerDataAsync(captureFromGameSystems: false);
                Assert(!saveOffline.Success && saveOffline.ErrorCode == AuthErrorCode.NoInternet,
                    "Offline Cloud Save must return AuthErrorCode.NoInternet");
                dbMgr.SimulateNetworkOffline = false;
            });

            // 14. Google Account already linked to another Player ID -> Conflict handled safely without deleting Guest data
            await RunAsync("Test_14_AccountAlreadyLinkedConflict_PreservesGuestDataSafely", async () =>
            {
                // Logout and create a fresh Guest 2
                await auth.LogoutAsync(clearCachedSession: true);
                var guest2Res = await guestMgr.ContinueAsGuestAsync();
                Assert(guest2Res.Success && auth.PlayerId != guestPlayerId, "Fresh Guest 2 should get a new PlayerId");
                string guest2Id = auth.PlayerId;

                // Give Guest 2 some dungeon progress
                dbMgr.CurrentData.dungeonData.dungeonLevel = 2;
                await dbMgr.SaveCurrentPlayerDataAsync(captureFromGameSystems: false);

                // Attempt to link Google Token A (which is already linked to guestPlayerId)
                var conflictRes = await linkMgr.LinkGuestWithGoogleIdTokenAsync(googleTokenA, "keeper.a@gmail.com");
                Assert(!conflictRes.Success && conflictRes.ErrorCode == AuthErrorCode.AccountAlreadyLinked,
                    "Linking an already-linked Google account must return AccountAlreadyLinked");
                Assert(auth.PlayerId == guest2Id && dbMgr.CurrentData.dungeonData.dungeonLevel == 2,
                    "Guest 2 data must NOT be overwritten or deleted on conflict");
                Assert(linkMgr.HasPendingConflict, "AccountLinkManager should hold pending conflict for user choice");

                // User chooses to Cancel & Keep Guest 2
                linkMgr.CancelConflictKeepGuest();
                Assert(!linkMgr.HasPendingConflict && auth.PlayerId == guest2Id, "Cancelling conflict must keep Guest 2 active");

                // Also verify fresh Google Token B creates a brand-new Google profile using UGS PlayerId (never email as ID)
                await auth.LogoutAsync(clearCachedSession: true);
                var googleBRes = await auth.SignInWithGoogleIdTokenAsync(googleTokenB, "newplayer.b@gmail.com");
                Assert(googleBRes.Success && !auth.PlayerId.Contains("@"), "Google PlayerId must be UGS PlayerId, never email");
            });

            UnityEngine.Object.DestroyImmediate(host);
            Debug.Log($"========== ACCOUNT & PLAYER DATABASE TEST RESULTS: {passed} / {total} PASSED ==========");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
