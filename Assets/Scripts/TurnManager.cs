using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public class BC_Player
{
    public ulong clientID;
    public bool isMe;
    public BaseHand playerHand;
    public PlayerUI playerUI;
    public int score = 0;
}


public class TurnManager : NetworkBehaviour
{
    public static TurnManager instance;

    public ResultsDisplay resultsDisplay;
    public PlayerCreationHandler playerCreationHandler;
    public PlayerHand playerHand;
    public List<AIHand> otherHands;
    public List<PlayerUI> playerUIs;
    [SerializeField]
    private List<BC_Player> players = new List<BC_Player>();
    public GameObject turnToken;
    public int turnIdx = 0; //Set this equal to the player who pulled the double
    public bool takeTurns;
    public int totalRounds = 3;
    int currentRounds = 0;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            HideTurnToken();
        }
        else
        {
            Destroy(this);
        }
    }

    PlayerUI GetSpawnedPlayerUI()
    {
        for(int i = 0; i < playerUIs.Count; i++)
        {
            if(playerUIs[i].GetMyHand() == null)
            {
                return playerUIs[i];
            }
        }
        return null;
    }

    BaseHand GetOtherPlayerHand()
    {
        for (int i = 0; i < otherHands.Count; i++)
        {
            if (!otherHands[i].GetIsAssigned())
            {
                return otherHands[i];
            }
        }
        return null;
    }

    public void ServerSceneInit(ulong clientID)
    {
        if(players.Count > 0) { return; }
        Debug.Log("Server Init for: " + clientID);
        List<BCPlayerInLobby> playersInGame = BCPlayersInGame.instance.GetPlayers();
        foreach(BCPlayerInLobby p in playersInGame)
        {
            BC_Player newPlayer = new BC_Player();
            newPlayer.clientID = (ulong)p.playerID.Value;
            newPlayer.isMe = NetworkManager.Singleton.LocalClientId == newPlayer.clientID;
            if (newPlayer.clientID == NetworkManager.Singleton.LocalClientId)
            {
                newPlayer.playerHand = playerHand;
            }
            else
            {
                newPlayer.playerHand = GetOtherPlayerHand();
            }
            newPlayer.playerHand.playerID = newPlayer.clientID;
            newPlayer.playerHand.SetNameText(string.Format("BC_{0}", newPlayer.clientID));
            PlayerUI myUI = GetSpawnedPlayerUI();
            myUI.SetUpPlayerUI(newPlayer.playerHand);
            newPlayer.playerUI = myUI;

            players.Add(newPlayer);
        }
        

        PlayerConnectToGameClientRpc(clientID);
    }

    [ClientRpc]
    public void PlayerConnectToGameClientRpc(ulong clientID)
    {
        if (IsServer) { return; }

        Debug.Log("Client Gameplay Connect RPC");

        List<BCPlayerInLobby> playersInGame = BCPlayersInGame.instance.GetPlayers();
        foreach (BCPlayerInLobby p in playersInGame)
        {
            BC_Player newPlayer = new BC_Player();
            newPlayer.clientID = (ulong)p.playerID.Value;
            newPlayer.isMe = NetworkManager.Singleton.LocalClientId == newPlayer.clientID;
            if (newPlayer.clientID == NetworkManager.Singleton.LocalClientId)
            {
                newPlayer.playerHand = playerHand;
            }
            else
            {
                newPlayer.playerHand = GetOtherPlayerHand();
            }
            newPlayer.playerHand.playerID = newPlayer.clientID;
            newPlayer.playerHand.SetNameText(string.Format("BC_{0}", newPlayer.clientID));
            PlayerUI myUI = GetSpawnedPlayerUI();
            myUI.SetUpPlayerUI(newPlayer.playerHand);
            newPlayer.playerUI = myUI;

            players.Add(newPlayer);
        }
    }



    //void IniatlizePlayers(List<BC_Player> lobbyPlayers)
    //{
    //    players = new List<BC_Player>(lobbyPlayers);
    //    for(int i = 0; i < players.Count; i++)
    //    {
    //        if(players[i].clientID)
    //    }
    //}

    public BaseHand GetPlayerHostHand()
    {
        for(int i = 0; i < players.Count; i++)
        {
            if (players[i].isMe)
            {
                return players[i].playerHand;
            }
        }
        return null;
    }

    public BaseHand GetActivePlayerHand()
    {
        return players[turnIdx].playerHand;
    }

    public List<PlayerUI> GetNonActivePlayerUI()
    {
        List<PlayerUI> retval = new List<PlayerUI>();
        for (int i = 0; i < players.Count; i++)
        {
            if (i != turnIdx)
            {
                retval.Add(players[i].playerUI);
            }
        }
        return retval;
    }

    public List<BaseHand> GetOtherPlayers()
    {
        List<BaseHand> retval = new List<BaseHand>();
        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].isMe)
            {
                retval.Add(players[i].playerHand);
            }
        }
        return retval;
    }

    public List<BaseHand> GetPlayerHandsInGame()
    {
        List<BaseHand> retval = new List<BaseHand>();
        for (int i = 0; i < players.Count; i++)
        {
            retval.Add(players[i].playerHand);
        }
        return retval;
    }

    public BaseHand GetPlayerHandFromID(ulong clientID)
    {
        for(int i = 0; i < players.Count; i++)
        {
            if(players[i].clientID == clientID)
            {
                return players[i].playerHand;
            }
        }
        return null;
    }

    public List<BaseHand> GetNonActivePlayerHands()
    {
        List<BaseHand> retval = new List<BaseHand>();
        for(int i = 0; i < players.Count; i++)
        {
            if(i != turnIdx)
            {
                retval.Add(players[i].playerHand);
            }
        }
        return retval;
    }

    public void TickDraftAction()
    {
        turnIdx++;
        if (turnIdx >= players.Count)
        {
            turnIdx = 0;
        }
        StartDraftActionClientRpc(turnIdx);
        //players[turnIdx].playerHand.DraftAction();
    }

    public void SendStartDraftToClients()
    {
        StartDraftActionClientRpc(turnIdx);
    }

    [ClientRpc]
    public void StartDraftActionClientRpc(int playerIdx)
    {
        //if (IsServer) { return; }
        Debug.Log("Start Draft Player: " + playerIdx);
        BoneCharmManager.instance.boneYard.SetIsInDraft(true);
        if(playerIdx == GetPlayerIdx(NetworkManager.Singleton.LocalClientId))
        {
            Debug.Log("Im starting my draft");
            players[playerIdx].playerHand.DraftAction();
        }
        else
        {
            SetTurnToken(players[playerIdx].playerHand.turnTokenPosition);
        }

    }

    public void SendStartTurnsToClients()
    {
        StartTurnsClientRpc(turnIdx);
    }

    public void StartTurns(BaseHand firstPlayer)
    {
        for(int i = 0; i < players.Count; i++)
        {
            if(players[i].playerHand == firstPlayer) { turnIdx = i; }
            players[i].playerUI.SetUpPlayerUI(players[i].playerHand);
        }



        takeTurns = true;
        BoneCharmManager.instance.boneYard.OnTurnUpdate(players[turnIdx]);
        players[turnIdx].playerHand.StartTurn();
    }

    [ClientRpc]
    public void StartTurnsClientRpc(int playerIdx)
    {
        if (IsServer) { return; }
        Debug.Log("Start Turn Player: " + playerIdx);
        if (playerIdx == GetPlayerIdx(NetworkManager.Singleton.LocalClientId))
        {
            Debug.Log("Im starting my turn");
            StartTurns(players[playerIdx].playerHand);
        }
        else
        {
            SetTurnToken(players[playerIdx].playerHand.turnTokenPosition);
        }
    }

    public void TickTurnIdx(bool ignoreTick = false)
    {
        players[turnIdx].playerHand.EndTUrn();
        int winner = CheckForGameOver();
        if(winner != -1)
        {
            players[winner].score++;
            resultsDisplay.ShowResults(players[winner].isMe);
            //if(SceneLoader.instance)
            //{
            //    SceneLoader.instance.LoadMainMenuScene();
            //}
            return;
        }

        if(!ignoreTick)
            turnIdx++;

        if (turnIdx >= players.Count)
        {
            turnIdx = 0;
        }

        PassTurnClientRpc(turnIdx);
        //BoneCharmManager.instance.boneYard.OnTurnUpdate(players[turnIdx]);
        //players[turnIdx].playerHand.StartTurn();
    }

    [ClientRpc]
    public void PassTurnClientRpc(int playerIdx)
    {
        if (!IsServer) { turnIdx = playerIdx; }
        Debug.Log("Client Rpc Pass Turn To: " + playerIdx);
        if (playerIdx == GetPlayerIdx(NetworkManager.Singleton.LocalClientId))
        {
            BoneCharmManager.instance.boneYard.OnTurnUpdate(players[playerIdx]);
            players[playerIdx].playerHand.StartTurn();
            Debug.Log("Im taking the next turn");
        }
        else
        {
            SetTurnToken(players[playerIdx].playerHand.turnTokenPosition);
        }
    }

    [ClientRpc]
    public void GivePlayerInitialHandClientRpc(ulong clientID, BoneCharmNetData[] charmData)
    {
        if (IsServer) { return; }
        Debug.Log("Give Initial Hand Client Rpc: " + clientID);
        int i = GetPlayerIdx(clientID);
        if(i != -1)
        {
            List<BoneCharm> charms = BoneCharmManager.instance.GetCharmsFromNetData(charmData);
            players[i].playerHand.InitHand(charms);
            BoneCharmManager.instance.boneYard.RemoveBoneCharms(charms);
        }
    }

    public int GetPlayerIdx(ulong clientID)
    {
        for(int i = 0; i < players.Count; i++)
        {
            if (players[i].clientID == clientID)
                return i;
        }

        return -1;
    }

    public bool IsItMyTurn(BaseHand baseHand)
    {
        return GetActivePlayerHand() == baseHand;
    }

    public void SetTurnToken(Transform handTokenPosition)
    {
        turnToken.SetActive(true);
        //turnToken.transform.SetParent(handTokenPosition);
        turnToken.transform.position = handTokenPosition.position;
    }

    public void HideTurnToken()
    {
        turnToken.SetActive(false);
    }

    int CheckForGameOver()
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].playerHand.IsEmpty())
            {
                return i;
            }
        }
        return -1;
    }
}
