using UnityEngine;
using UnityEngine.Events;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication.PlayerAccounts;

public abstract class BaseAuthService : MonoBehaviour
{
    [Header("Authentication Events")]
    public UnityEvent<PlayerInfo> OnSignedIn;
    public UnityEvent<Exception> OnSignInFailed;
    public UnityEvent OnSignedOut;
    public UnityEvent OnSessionExpired;

    protected bool IsInitialized { get; private set; } = false;
    protected string ServiceType => GetType().Name;
    protected bool isActiveAuthSource = false;


    protected virtual async Task EnsureInitialized()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            try
            {
                await UnityServices.InitializeAsync();
                Debug.Log($"[{ServiceType}] Unity Services initialized (Lazy Init)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{ServiceType}] Failed to initialize Unity Services: {ex.Message}");
                throw;
            }
        }

        if (!IsInitialized)
        {
            SetupAuthenticationEvents();
            IsInitialized = true;
        }
    }

    protected virtual void SetupAuthenticationEvents()
    {
        AuthenticationService.Instance.SignedIn -= HandleSignedIn;
        AuthenticationService.Instance.SignInFailed -= HandleSignInFailed;
        AuthenticationService.Instance.SignedOut -= HandleSignedOut;
        AuthenticationService.Instance.Expired -= HandleSessionExpired;

        AuthenticationService.Instance.SignedIn += HandleSignedIn;
        AuthenticationService.Instance.SignInFailed += HandleSignInFailed;
        AuthenticationService.Instance.SignedOut += HandleSignedOut;
        AuthenticationService.Instance.Expired += HandleSessionExpired;
    }

    protected virtual void HandleSignedIn()
    {
        if (!isActiveAuthSource) return;
        Debug.Log($"[{ServiceType}] Player signed in - ID: {AuthenticationService.Instance.PlayerId}");
        OnSignedIn?.Invoke(AuthenticationService.Instance.PlayerInfo);
        isActiveAuthSource = false;
    }

    protected virtual void HandleSignInFailed(RequestFailedException exception)
    {
        if (!isActiveAuthSource) return;
        Debug.LogError($"[{ServiceType}] Sign in failed: {exception.Message}");
        OnSignInFailed?.Invoke(exception);
        isActiveAuthSource = false;
    }

    protected virtual void HandleSignedOut()
    {
        Debug.Log($"[{ServiceType}] Player signed out");
        OnSignedOut?.Invoke();
    }

    protected virtual void HandleSessionExpired()
    {
        Debug.Log($"[{ServiceType}] Player session expired");
        OnSessionExpired?.Invoke();
    }

    public abstract Task SignInAsync();

    protected virtual void OnDestroy()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance != null)
        {
            AuthenticationService.Instance.SignedIn -= HandleSignedIn;
            AuthenticationService.Instance.SignInFailed -= HandleSignInFailed;
            AuthenticationService.Instance.SignedOut -= HandleSignedOut;
            AuthenticationService.Instance.Expired -= HandleSessionExpired;
        }
    }
}