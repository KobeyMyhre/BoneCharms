using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BCPlayersInGame : NetworkBehaviour
{
    public static BCPlayersInGame instance;
    public List<BCPlayerInLobby> playersInGame;
    public NetworkVariable<int> currentRound = new NetworkVariable<int>();
    [Range(1,3)]
    public int totalRounds = 1;

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
        if(currentRound.Value >= totalRounds)
        {
            TurnManager.instance.EndGameSession();
            //End Session
            
        }
        else
        {
            SceneLoader.instance.LoadSceneNetworked(eScenes.BoneCharmsMultiplayer);
            //Load Another Round
        }
    }

    

    public bool IsMatchOver()
    {
        return currentRound.Value <= totalRounds;
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

    public string GetPlayerName(int clientID)
    {
        foreach(BCPlayerInLobby p in playersInGame)
        {
            if(p.playerID.Value == clientID)
            {
                return p.GetPlayerName();
            }
        }
        return "Name Not Found.";
    }

    //Possibly Sort this first so theyre in ID order
    public List<BCPlayerInLobby> GetPlayers()
    {
        return playersInGame;
    }

    public PlayerRoundScore[] GetRoundScoreDate()
    {
        List<PlayerRoundScore> retval = new List<PlayerRoundScore>();

        foreach(BCPlayerInLobby p in playersInGame)
        {
            retval.Add(new PlayerRoundScore(p));
        }

        return retval.ToArray();
    }

    public void EndGameSession()
    {
        Destroy(instance.gameObject);
        instance = null;
    }
}

public struct PlayerRoundScore : INetworkSerializable
{
    public int playerID;
    public int currentScore;

    public PlayerRoundScore(BCPlayerInLobby player)
    {
        playerID = player.playerID.Value;
        currentScore = player.roundPoints.Value;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref playerID);
        serializer.SerializeValue(ref currentScore);
    }
}