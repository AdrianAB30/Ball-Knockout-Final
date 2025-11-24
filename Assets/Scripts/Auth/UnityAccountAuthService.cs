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

            if (AuthenticationService.Instance.IsSignedIn && PlayerAccountService.Instance.IsSignedIn)
            {
                Debug.Log($"[{ServiceType}] Ya estabas logueado con Unity. Éxito inmediato.");
                HandleSignedIn();
                return;
            }
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"[{ServiceType}] Sesión previa detectada (posiblemente Guest). Cerrando para iniciar con Unity...");
                AuthenticationService.Instance.SignOut();
            }

            if (PlayerAccountService.Instance != null)
            {
                PlayerAccountService.Instance.SignedIn -= HandleUnityAccountSignedIn;
                PlayerAccountService.Instance.SignedIn += HandleUnityAccountSignedIn;
            }

            isActiveAuthSource = true;
            OnUnityAccountSignInStarted?.Invoke();

            await PlayerAccountService.Instance.StartSignInAsync();
        }
        catch (RequestFailedException ex)
        {
            if (ex.Message.Contains("already signing in") || ex.ErrorCode == 8001)
            {
                Debug.LogWarning($"[{ServiceType}] Login en progreso detectado. Esperando...");
                return;
            }

            Debug.LogError($"[{ServiceType}] Unity Account sign in failed: {ex.Message}");
            isActiveAuthSource = false;
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{ServiceType}] Error general: {ex.Message}");
            isActiveAuthSource = false;
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