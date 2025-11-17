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

    protected override async void Start()
    {
        try
        {
            await InitializeAuthentication();
            if (PlayerAccountService.Instance != null)
            {
                PlayerAccountService.Instance.SignedIn += HandleUnityAccountSignedIn;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{ServiceType}] Failed to initialize Unity Account Auth Service: {ex.Message}");
            OnSignInFailed?.Invoke(ex);
        }
    }

    public override async Task SignInAsync()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning($"[{ServiceType}] Authentication service not initialized");
            throw new InvalidOperationException("Authentication service not initialized");
        }
        try
        {
            isActiveAuthSource = true;
            OnUnityAccountSignInStarted?.Invoke();
            await PlayerAccountService.Instance.StartSignInAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{ServiceType}] Unity Account sign in failed: {ex.Message}");
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
