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
    public PlayerUI playerUI;
    public List<AIHand> otherHands;
    public List<PlayerUI> playerUIs;
    [SerializeField]
    private List<BC_Player> players = new List<BC_Player>();
    public TokenMover turnToken;
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
            for(int i =0; i < playerUIs.Count; i++)
            {
                playerUIs[i].gameObject.SetActive(false);
            }
        }
        else
        {
            Destroy(this);
        }
    }

    PlayerUI GetOtherPlayersUI()
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
                otherHands[i].SetIsAssigned(true);
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
                newPlayer.playerUI = playerUI;
                newPlayer.playerUI.SetUpPlayerUI(playerHand);
            }
            else
            {
                newPlayer.playerHand = GetOtherPlayerHand();
                newPlayer.playerUI = GetOtherPlayersUI();
                newPlayer.playerUI.SetUpPlayerUI(newPlayer.playerHand);
            }
            newPlayer.playerHand.playerID = newPlayer.clientID;
            newPlayer.playerHand.SetNameText(p.GetPlayerName());
            newPlayer.playerHand.SetScoreText(p.roundPoints.Value);
            //PlayerUI myUI = GetOtherPlayersUI();
            //myUI.SetUpPlayerUI(newPlayer.playerHand);
            //newPlayer.playerUI = myUI;

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
                newPlayer.playerUI = playerUI;
                newPlayer.playerUI.SetUpPlayerUI(playerHand);
            }
            else
            {
                newPlayer.playerHand = GetOtherPlayerHand();
                newPlayer.playerUI = GetOtherPlayersUI();
                newPlayer.playerUI.SetUpPlayerUI(newPlayer.playerHand);
            }
            newPlayer.playerHand.playerID = newPlayer.clientID;
            newPlayer.playerHand.SetNameText(p.GetPlayerName());
            newPlayer.playerHand.SetScoreText(p.roundPoints.Value);
            //PlayerUI myUI = GetOtherPlayersUI();
            //myUI.SetUpPlayerUI(newPlayer.playerHand);
            //newPlayer.playerUI = myUI;

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

    public List<BaseHand> GetAllHands()
    {
        List<BaseHand> retval = new List<BaseHand>(otherHands);
        retval.Add(playerHand);
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

    public BC_Player GetPlayerFromID(ulong clientID)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].clientID == clientID)
            {
                return players[i];
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
        List<BaseHand> otherHands = GetNonActivePlayerHands();
        foreach(BaseHand hand in otherHands)
        {
            hand.PlaceHandPositions();
        }
    }

    [ClientRpc]
    public void DisplayMidRoundResultsClientRpc(PlayerRoundScore[] bcPlayers, int currentRound)
    {
        resultsDisplay.ShowMidRoundResults(bcPlayers, currentRound);
    }

    [ClientRpc]
    public void DisplayWinnerClientRpc(ulong winnerID)
    {
        BC_Player winner = GetPlayerFromID(winnerID);
        if(winner != null)
        {
            winner.score++;
            resultsDisplay.ShowResults(winner.isMe);
        }
    }

    //[ServerRpc(RequireOwnership = false)]
    //public void ClientTickTurnIdxServerRpc(bool ignoreTick = false)
    //{
    //    TickTurnIdx(ignoreTick);
    //}

    public bool AttemptToFinishGame()
    {
        int winner = CheckForGameOver();
        if (winner != -1)
        {
            //Add up all the Points
            foreach (BC_Player p in players)
            {
                BCPlayerInLobby playerInGame = BCPlayersInGame.instance.GetPlayerInGame(p.clientID);
                playerInGame.AddRoundPoints(p.playerHand);
            }
            //players[winner].score++;
            //resultsDisplay.ShowResults(players[winner].isMe);
            if (BCPlayersInGame.instance.IsMatchOver())
            {
                DisplayWinnerClientRpc(players[winner].clientID);
            }
            else
            {
                DisplayMidRoundResultsClientRpc(BCPlayersInGame.instance.GetRoundScoreDate(), BCPlayersInGame.instance.currentRound.Value);
            }

            return true;
        }
        return false;
    }

    public void TickTurnIdx(bool ignoreTick = false)
    {
        if (!IsServer) { return; }

        players[turnIdx].playerHand.EndTurn();
        if (AttemptToFinishGame())
        {
            return;
        }
        //int winner = CheckForGameOver();
        //if(winner != -1)
        //{
        //    //Add up all the Points
        //    foreach(BC_Player p in players)
        //    {
        //        BCPlayerInLobby playerInGame = BCPlayersInGame.instance.GetPlayerInGame(p.clientID);
        //        playerInGame.AddRoundPoints(p.playerHand);
        //    }
        //    //players[winner].score++;
        //    //resultsDisplay.ShowResults(players[winner].isMe);
        //    if (BCPlayersInGame.instance.IsMatchOver())
        //    {
        //        DisplayWinnerClientRpc(players[winner].clientID);
        //    }
        //    else
        //    {
        //        DisplayMidRoundResultsClientRpc(BCPlayersInGame.instance.GetRoundScoreDate(), BCPlayersInGame.instance.currentRound.Value);
        //    }

        //    return;
        //}

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
        List<BaseHand> hands = GetAllHands();
        foreach(BaseHand hand in hands)
        {
            hand.EndTurn();
        }

        if (!IsServer) { turnIdx = playerIdx; }
        Debug.Log("Client Rpc Pass Turn To: " + playerIdx);
        BoneCharmManager.instance.boneYard.OnTurnUpdate(players[playerIdx]);
        if (playerIdx == GetPlayerIdx(NetworkManager.Singleton.LocalClientId))
        {
            //BoneCharmManager.instance.boneYard.OnTurnUpdate(players[playerIdx]);
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

    public GameObject GetPassTurnToken()
    {
        return turnToken.gameObject;
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
        turnToken.gameObject.SetActive(true);
        //turnToken.transform.SetParent(handTokenPosition);
        //turnToken.transform.position = handTokenPosition.position;
        turnToken.MoveToNewTarget(handTokenPosition.position);
    }

    public void HideTurnToken()
    {
        turnToken.gameObject.SetActive(false);
    }

    public void EndGameSession()
    {
        if(!NetworkManager.Singleton.IsServer) { return; }
        Debug.Log("Final Round Over");
        //TellClientsToGoHomeClientRpc();
        SceneLoader.instance.LoadSceneNetworked(eScenes.GameplayRoundCleanUp);
    }

    [ClientRpc]
    public void TellClientsToGoHomeClientRpc()
    {
        //if (NetworkManager.Singleton.IsServer) { return; }
        Debug.Log("Exit Game on Client");
        if (SceneLoader.instance)
        {
            SceneLoader.instance.LoadSceneNetworked(eScenes.GameplayRoundCleanUp);
            //NetworkManager.Singleton.Shutdown();
        }
    }

    public bool IsCharmInOtherPlayerHand(BoneCharm charm)
    {
        List<BaseHand> otherHands = GetNonActivePlayerHands();
        foreach(BaseHand hand in otherHands)
        {
            if (hand.myHand.Contains(charm))
            {
                return true;
            }
        }
        return false;
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
