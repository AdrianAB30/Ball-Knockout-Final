using UnityEngine;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using System.Collections.Generic; 
using Unity.Services.CloudSave; 
using System;
using Random = UnityEngine.Random;

[Serializable]
public class UserProfileData
{
    public string description = "--------";
    public string birthday = "00/00/0000";
    public string status = "Disponible";
    public int avatarIndex = 0;
}

public class PlayerAccountManager : PersistentSingleton<PlayerAccountManager> 
{
    public string PlayerName { get; private set; }
    public bool IsGuest { get; private set; }

    public UserProfileData CurrentProfile = new UserProfileData();
    public static event Action<UserProfileData> OnProfileLoaded;

    private string[] randomNamePrefixes = { "China", "Willy", "ChildGrain", "SkinComun", "Mario", "Hawkings" };
    private string[] randomNameSuffixes = { "God", "99", "Noob", "Lord", "Jumper", "Master" };

    public async Task OnLoginSuccess(bool isGuest)
    {
        IsGuest = isGuest;

        if (IsGuest)
        {
            PlayerName = "Guest#" + Random.Range(1000, 9999);
            CurrentProfile = new UserProfileData();
        }
        else
        {
            try
            {
                PlayerName = await AuthenticationService.Instance.GetPlayerNameAsync();

                if (string.IsNullOrEmpty(PlayerName) || !PlayerName.Contains("#"))
                {
                    PlayerName = GenerateRandomName();
                    await AuthenticationService.Instance.UpdatePlayerNameAsync(PlayerName);
                    Debug.Log("Nombre actualizado al nuevo formato: " + PlayerName);
                }

                await LoadProfileData();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error getting Unity Player Name/Profile: {e.Message}");
                PlayerName = "Player#" + Random.Range(1000, 9999);
            }
        }

        PlayerPrefs.SetString("PlayerName", PlayerName);
        Debug.Log($"Login exitoso. Bienvenido, {PlayerName}");
    }

    public async Task SaveProfileData(string desc, string bday, string stat, int avatarId)
    {
        if (IsGuest) return; 

        CurrentProfile.description = desc;
        CurrentProfile.birthday = bday;
        CurrentProfile.status = stat;
        CurrentProfile.avatarIndex = avatarId;

        try
        {
            var data = new Dictionary<string, object> { { "player_profile", CurrentProfile } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log("Perfil guardado en Cloud Save.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al guardar perfil: {e}");
        }
    }

    public async Task LoadProfileData()
    {
        try
        {
            var keys = new HashSet<string> { "player_profile" };
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (data.TryGetValue("player_profile", out var profileItem))
            {
                CurrentProfile = profileItem.Value.GetAs<UserProfileData>();
                Debug.Log("Perfil descargado de Cloud Save.");
            }

            OnProfileLoaded?.Invoke(CurrentProfile);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al cargar perfil: {e}");
        }
    }

    public string GenerateRandomName()
    {
        string prefix = randomNamePrefixes[Random.Range(0, randomNamePrefixes.Length)];
        string suffix = randomNameSuffixes[Random.Range(0, randomNameSuffixes.Length)];
        string number = Random.Range(1000, 9999).ToString();
        return $"{prefix}{suffix}#{number}";
    }

    public async Task<string> ChangePlayerName(string newName)
    {
        if (string.IsNullOrEmpty(newName)) throw new System.ArgumentException("El nombre no puede estar vacío");

        if (IsGuest)
        {
            if (!newName.Contains("#"))
            {
                string number = UnityEngine.Random.Range(1000, 9999).ToString();
                PlayerName = $"{newName}#{number}";
            }
            else
            {
                PlayerName = newName;
            }
        }
        else
        {
            string cleanName = newName;
            if (newName.Contains("#"))
            {
                cleanName = newName.Split('#')[0]; 
            }

            PlayerName = await AuthenticationService.Instance.UpdatePlayerNameAsync(cleanName);
        }

        PlayerPrefs.SetString("PlayerName", PlayerName);

        if (LobbyManager.Instance != null && LobbyManager.Instance.JoinedLobby != null)
        {
            await LobbyManager.Instance.UpdatePlayerNameInLobby(PlayerName);
        }
        return PlayerName;
    }
    public void ForceProfileUpdateEvent()
    {
        OnProfileLoaded?.Invoke(CurrentProfile);
    }
}