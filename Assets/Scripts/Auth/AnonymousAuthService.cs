using UnityEngine;
using UnityEngine.Events;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication.PlayerAccounts;

public class AnonymousAuthService : BaseAuthService
{
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

            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"[{ServiceType}] Player is already signed in");
                HandleSignedIn();
                isActiveAuthSource = false; 
                return;
            }

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{ServiceType}] Anonymous sign in failed: {ex.Message}");
            isActiveAuthSource = false;
            throw;
        }
    }
}
