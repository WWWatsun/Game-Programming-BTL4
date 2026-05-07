using UnityEngine;
using Unity.Netcode;

public class ColorPickerUIController : MonoBehaviour
{
    // This function will be linked to the OnClick() events of your 4 color buttons.
    // Red = 0, Blue = 1, Green = 2, Yellow = 3
    public void OnColorButtonClicked(int colorIndex)
    {
        // 1. Ensure the network is actually running and we are a connected client
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            // 2. Find the NetworkObject that belongs to THIS specific screen
            NetworkObject localPlayerObject = NetworkManager.Singleton.LocalClient.PlayerObject;

            if (localPlayerObject != null)
            {
                // 3. Grab the PlayerController component and send the choice
                PlayerController localPlayer = localPlayerObject.GetComponent<PlayerController>();

                if (localPlayer != null)
                {
                    localPlayer.SubmitColorChoice(colorIndex);

                    // Hide the UI locally right away so the player knows they clicked it
                    gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogError("Could not find PlayerController on the Local Player Object!");
                }
            }
        }
    }
}