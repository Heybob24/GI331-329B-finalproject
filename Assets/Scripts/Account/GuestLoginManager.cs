using System.Threading.Tasks;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class GuestLoginManager : MonoBehaviour
    {
        public static GuestLoginManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public async Task<AuthOperationResult> ContinueAsGuestAsync()
        {
            AuthenticationManager auth = AuthenticationManager.Instance ?? FindAnyObjectByType<AuthenticationManager>();
            if (auth == null)
            {
                auth = gameObject.GetComponent<AuthenticationManager>() ?? gameObject.AddComponent<AuthenticationManager>();
            }

            return await auth.SignInOrRestoreGuestAsync();
        }
    }
}
