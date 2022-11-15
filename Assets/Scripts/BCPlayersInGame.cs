using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BCPlayersInGame : NetworkBehaviour
{
    public static BCPlayersInGame instance;
    public List<BCPlayerInLobby> playersInGame;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    public void AddPlayer(BCPlayerInLobby playerInLobby)
    {
        DontDestroyOnLoad(playerInLobby.gameObject);
        playersInGame.Add(playerInLobby);
    }

    //Possibly Sort this first so theyre in ID order
    public List<BCPlayerInLobby> GetPlayers()
    {
        return playersInGame;
    }
}
