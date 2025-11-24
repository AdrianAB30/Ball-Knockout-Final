using UnityEngine;
using UnityEngine.Events;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication.PlayerAccounts;

public class UnityAccountAuthService : BaseAuthService
{
    [Header("Unity Account Events")]
    public UnityEvent OnUnityAccountSignInStarted;

    public override async Task SignInAsync()
    {
        try
        {
            await EnsureInitialized();

            // 1. Chequeo Rápido
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"[{ServiceType}] Ya estabas logueado. Éxito inmediato.");
                HandleSignedIn();
                return;
            }

            // 2. Configurar eventos
            if (PlayerAccountService.Instance != null)
            {
                PlayerAccountService.Instance.SignedIn -= HandleUnityAccountSignedIn;
                PlayerAccountService.Instance.SignedIn += HandleUnityAccountSignedIn;
            }

            isActiveAuthSource = true; // Activamos la bandera
            OnUnityAccountSignInStarted?.Invoke();

            // 3. Intentar login
            await PlayerAccountService.Instance.StartSignInAsync();
        }
        catch (Exception ex)
        {
            // --- LA CORRECCIÓN MÁGICA ---
            // Si el error es "Already signing in" (Conflicto de editor), NO cancelamos.
            if (ex.Message.Contains("already signing in"))
            {
                Debug.LogWarning($"[{ServiceType}] Conflicto detectado: Unity ya se está logueando. Esperando éxito...");
                // IMPORTANTE: NO ponemos isActiveAuthSource = false. 
                // La dejamos en true y esperamos a que el evento de éxito llegue solo.
                return;
            }
            // -----------------------------

            Debug.LogError($"[{ServiceType}] Unity Account sign in failed: {ex.Message}");
            isActiveAuthSource = false; // Solo cancelamos si es un error real
            throw;
        }
    }

    private async void HandleUnityAccountSignedIn()
    {
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"[{ServiceType}] UGS is already signed in. Triggering success event.");
                HandleSignedIn();
                return;
            }
            string accessToken = PlayerAccountService.Instance.AccessToken;
            await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{ServiceType}] Sign in with Unity failed: {ex.Message}");
            isActiveAuthSource = false;
            OnSignInFailed?.Invoke(ex);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (PlayerAccountService.Instance != null)
        {
            PlayerAccountService.Instance.SignedIn -= HandleUnityAccountSignedIn;
        }
    }
}