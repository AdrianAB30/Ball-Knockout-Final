using UnityEngine;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class PlayerAccountManager : NonPersistentSingleton<PlayerAccountManager>
{
    public string PlayerName { get; private set; }
    public bool IsGuest { get; private set; }

    private string[] randomNamePrefixes = { "China", "Willy", "ChildGrain", "SkinComun", "Mario", "Hawkings" };
    private string[] randomNameSuffixes = { "God", "99", "Noob", "Lord", "Jumper", "Master" };

    public async Task OnLoginSuccess(bool isGuest)
    {
        IsGuest = isGuest;

        if (IsGuest)
        {
            PlayerName = "Guest_" + Random.Range(1000, 9999);
        }
        else
        {
            try
            {
                PlayerName = await AuthenticationService.Instance.GetPlayerNameAsync();
                if (string.IsNullOrEmpty(PlayerName))
                {
                    PlayerName = "Player" + Random.Range(1000, 9999);
                    await AuthenticationService.Instance.UpdatePlayerNameAsync(PlayerName);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error getting/setting Unity Player Name: {e.Message}");
                PlayerName = "Player" + Random.Range(1000, 9999);
            }
        }

        PlayerPrefs.SetString("PlayerName", PlayerName);
        Debug.Log($"Login exitoso. Bienvenido, {PlayerName}");
    }
    public string GenerateRandomName()
    {
        string prefix = randomNamePrefixes[Random.Range(0, randomNamePrefixes.Length)];
        string suffix = randomNameSuffixes[Random.Range(0, randomNameSuffixes.Length)];
        string number = Random.Range(10, 99).ToString();
        return $"{prefix}{suffix}{number}";
    }

    public async Task<string> ChangePlayerName(string newName)
    {
        if (string.IsNullOrEmpty(newName))
        {
            throw new System.ArgumentException("El nombre no puede estar vacío");
        }

        if (IsGuest)
        {
            PlayerName = newName;
        }
        else
        {
            PlayerName = await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
        }

        PlayerPrefs.SetString("PlayerName", PlayerName);

        if (LobbyManager.Instance != null && LobbyManager.Instance.JoinedLobby != null)
        {
            await LobbyManager.Instance.UpdatePlayerNameInLobby(PlayerName);
        }
        return PlayerName;
    }
}