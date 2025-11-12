using UnityEngine;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;

public class RelayServiceManager : NonPersistentSingleton<RelayServiceManager>
{
    public string RelayJoinCode { get; private set; }
    public string RelayIpV4 { get; private set; }
    public int RelayPort { get; private set; }

    public async Task<string> CreateRelay(int maxPlayers = 4)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            this.RelayJoinCode = relayCode;
            this.RelayIpV4 = allocation.RelayServer.IpV4;
            this.RelayPort = allocation.RelayServer.Port;

            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetHostRelayData(RelayIpV4, (ushort)RelayPort, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

            NetworkManager.Singleton.StartHost();
            return relayCode;
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError($"Relay creation failed: {ex.Message}");
            return null;
        }
    }

    public async Task JoinRelay(string relayCode) // (O JoinRelayByCode, el que uses)
    {
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayCode);

            // --- ¡AÑADE ESTAS 3 LÍNEAS! ---
            this.RelayJoinCode = relayCode; // Ya lo teníamos, pero lo guardamos
            this.RelayIpV4 = allocation.RelayServer.IpV4;
            this.RelayPort = allocation.RelayServer.Port;

            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetClientRelayData(RelayIpV4, (ushort)RelayPort, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.HostConnectionData);

            NetworkManager.Singleton.StartClient();
            Debug.Log("Joined relay successfully.");
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError($"Relay join failed: {ex.Message}");
        }
    }
}