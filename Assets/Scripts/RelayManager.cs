using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using TMPro; // For UI text

public class RelayManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField joinCodeInputField;
    public TextMeshProUGUI joinCodeDisplay;

    private async void Start()
    {
        // 1. Initialize Unity Services
        await UnityServices.InitializeAsync();

        // 2. Sign the player in anonymously (required for Relay)
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Player signed in with ID: {AuthenticationService.Instance.PlayerId}");
        }
    }

    // Call this from your "Host Game" UI Button
    public async void CreateRelayHost()
    {
        try
        {
            // Relay allocation takes the number of *clients* excluding the host.
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            // Get the short Join Code and display it on the UI
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            joinCodeDisplay.text = "Room Code: " + joinCode;
            Debug.Log("Hosting with Code: " + joinCode);

            // Configure the Transport with the Relay data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Start the Host
            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Failed to host: " + e.Message);
        }
    }

    // Call this from your "Join Game" UI Button
    public async void JoinRelayClient()
    {
        try
        {
            string joinCode = joinCodeInputField.text;
            Debug.Log("Attempting to join with code: " + joinCode);

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // Configure the Transport with the joined Relay data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // Start the Client
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Failed to join: " + e.Message);
        }
    }
}