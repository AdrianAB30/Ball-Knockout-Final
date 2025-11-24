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
        await EnsureInitialized();

        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }

            isActiveAuthSource = true;
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
