using Steamworks;
using UnityEngine;

public class SteamMatchmaker : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(SteamManager.Initialized) {
            string playerName = SteamFriends.GetPersonaName();
            Debug.Log(playerName);
            var lobbyPromise = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, 8);
        }
        else
        {
            Debug.Log("SteamManager not initialized");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
