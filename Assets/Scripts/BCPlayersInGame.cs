using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BCPlayersInGame : NetworkBehaviour
{
    public static BCPlayersInGame instance;
    public List<BCPlayerInLobby> playersInGame;
    public NetworkVariable<int> currentRound = new NetworkVariable<int>();
    public int totalRounds = 3;

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

    public void IniniateGameSession()
    {
        if(!NetworkManager.Singleton.IsServer) { return; }
        currentRound.Value = 0;
    }

    public void ProgressToNextRound()
    {
        if (!NetworkManager.Singleton.IsServer) { return; }
        currentRound.Value++;
        if(currentRound.Value == totalRounds)
        {
            //End Session
            if (SceneLoader.instance)
            {
                SceneLoader.instance.LoadMainMenuScene();
            }
        }
        else
        {
            SceneLoader.instance.LoadSceneNetworked(eScenes.BoneCharmsMultiplayer);
            //Load Another Round
        }
    }

    public void AddPlayer(BCPlayerInLobby playerInLobby)
    {
        playersInGame.Add(playerInLobby);
    }

    public BCPlayerInLobby GetPlayerInGame(ulong clientID)
    {
        int ID = (int)clientID;
        foreach(BCPlayerInLobby p in playersInGame)
        {
            if(p.playerID.Value == ID)
            {
                return p;
            }
        }
        return null;
    }

    //Possibly Sort this first so theyre in ID order
    public List<BCPlayerInLobby> GetPlayers()
    {
        return playersInGame;
    }
}
