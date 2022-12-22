using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public enum ConnectionState : byte
{
    connected,
    disconnected,
    ready
}

[System.Serializable]
public struct PlayerConnectionState
{
    public ConnectionState playerState;
    public Transform displayHolder;
    public PlayerLobbyDisplay lobbyDisplay;
    public BCPlayerInLobby player;
    public string playerName;
    public ulong clientID;
}


public class LobbyHandler : NetworkBehaviour
{
    public static LobbyHandler instance;
    public BCPlayerInLobby playerPrefab;
    public PlayerConnectionState[] playerConnections;
    public Color[] availableColors;
    public GameObject readyButton;
    public GameObject notReadyButton;

    public OnEvent onReadyUp;
    public OnEvent onNotReadyUp;
    

    float startTimerDuration = 1;
    float startTimer;
    bool startTimerActive = false;
    public TextMeshProUGUI timerText;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            UpdateReadyButtons(true);
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    void StartTimer()
    {
        startTimer = startTimerDuration;
        startTimerActive = true;
    }

    public void ServerSceneInit(ulong clientID)
    {
        //Make me a spot in the lobby
        for(int i = 0; i < playerConnections.Length; i++)
        {
            if(playerConnections[i].playerState == ConnectionState.disconnected)
            {
                playerConnections[i].playerState = ConnectionState.connected;
                playerConnections[i].playerName = string.Format("BC_{0}", clientID);
                playerConnections[i].clientID = clientID;

                GameObject newBaby = NetworkObjectSpawner.SpawnNewNetworkObjectChangeOwnershipToClient(playerPrefab.gameObject, Vector3.zero, clientID, false);
                playerConnections[i].player = newBaby.GetComponent<BCPlayerInLobby>();
                playerConnections[i].lobbyDisplay.InitPlayerDisplay(playerConnections[i].playerName, playerConnections[i].player.IsOwner);
                if (playerConnections[i].player.IsOwner)
                {
                    onReadyUp += playerConnections[i].player.ReadyButton;
                    onNotReadyUp += playerConnections[i].player.NotReadyButton;
                }
                playerConnections[i].displayHolder.transform.localScale = Vector3.one;
                playerConnections[i].displayHolder.localPosition = Vector3.zero;

                //GameObject newBaby = Instantiate(lobbyDisplayPrefab.gameObject, playerConnections[i].displayHolder);

                //NetworkObject newNetworkBaby = newBaby.GetComponent<NetworkObject>();
                //newNetworkBaby.Spawn(true);
                //PlayerLobbyDisplay lobbyDisplay = newBaby.GetComponent<PlayerLobbyDisplay>();
                //lobbyDisplay.InitPlayerDisplay(playerConnections[i].playerName);
                //playerConnections[i].lobbyDisplay = lobbyDisplay;
                break;
            }
        }
        //Sync States to Clients
        for(int i = 0; i < playerConnections.Length; i++)
        {
            if(playerConnections[i].player != null)
            {
                PlayerConnectsClientRpc(playerConnections[i].clientID, i, playerConnections[i].playerState, playerConnections[i].player.GetComponent<NetworkObject>());
                Debug.Log("@ Player Connects RPC Sent");
            }
        }
    }


    private void Update()
    {
        if (startTimerActive)
        {
            startTimer -= Time.deltaTime;
            if(startTimer <= 0)
            {
                //GO TO Gameplay
                //TO DO Tell Clients to play Transition
                BCPlayersInGame.instance.IniniateGameSession();
                SceneLoader.instance.LoadSceneNetworked(eScenes.BoneCharmsMultiplayer);
                return;
            }
            timerText.text = Mathf.RoundToInt(startTimer).ToString();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestClientRpc();
        }
    }

    [ClientRpc]
    private void TestClientRpc()
    {
        Debug.Log("Client RPC Fired");
    }


    [ClientRpc]
    void PlayerConnectsClientRpc(ulong clientID, int stateIndex, ConnectionState state, NetworkObjectReference player)
    {
        Debug.Log("@ Player Connects RPC Received");
        if (IsServer) { return; }

        if(state != ConnectionState.disconnected)
        {
            playerConnections[stateIndex].playerState = state;
            playerConnections[stateIndex].clientID = clientID;
            playerConnections[stateIndex].playerName = string.Format("BC_{0}", clientID);
            if (player.TryGet(out NetworkObject playerObject))
            {
                playerConnections[stateIndex].player = playerObject.GetComponent<BCPlayerInLobby>();
                playerConnections[stateIndex].lobbyDisplay.InitPlayerDisplay(playerConnections[stateIndex].playerName, playerConnections[stateIndex].player.IsOwner);
                if (playerConnections[stateIndex].player.IsOwner)
                {
                    onReadyUp += playerConnections[stateIndex].player.ReadyButton;
                    onNotReadyUp += playerConnections[stateIndex].player.NotReadyButton;
                }
            }
            playerConnections[stateIndex].displayHolder.transform.localScale = Vector3.one;
            playerConnections[stateIndex].displayHolder.localPosition = Vector3.zero;
        }
    }

    public void PlayerReady(ulong clientID, int playerID)
    {
        PlayerReadyClientRpc(clientID, playerID);

        //UpdateReadyButtons(false);

        StartGameTimer();
    }

    [ClientRpc]
    void PlayerReadyClientRpc(ulong clientID, int playerID)
    {
        if(playerID < playerConnections.Length && playerID >= 0)
        {
            playerConnections[playerID].playerState = ConnectionState.ready;
            playerConnections[playerID].lobbyDisplay.DisplayReadyUp(true);

            if (clientID == NetworkManager.Singleton.LocalClientId)
            {
                UpdateReadyButtons(false);
            }
            else
            {

            }


        }

    }

    void UpdateReadyButtons(bool val)
    {
        readyButton.SetActive(val);
        notReadyButton.SetActive(!val);
    }

    public void PlayerNotReady(ulong clientID, bool isDisonnected = false)
    {
        int playerID = GetPlayerIdx(clientID);
        //UpdateReadyButtons(true);
        //Stop Timer

        if(isDisonnected)
        {
            //Disconnect RPC
        }
        else
        {
            PlayerNotReadyClientRpc(clientID, playerID);
        }
    }

    [ClientRpc]
    void PlayerNotReadyClientRpc(ulong clientID, int playerID)
    {
        playerConnections[playerID].playerState = ConnectionState.connected;
        playerConnections[playerID].lobbyDisplay.DisplayReadyUp(false);
        if (clientID == NetworkManager.Singleton.LocalClientId)
        {
            UpdateReadyButtons(true);
        }
    }


    public void ReadyUpButton()
    {
        onReadyUp?.Invoke();
    }

    public void NotReadyButton()
    {
        onNotReadyUp?.Invoke();
    }



    void StartGameTimer()
    {
        foreach(PlayerConnectionState state in playerConnections)
        {
            //If a player is connected (not ready)
            if (state.playerState == ConnectionState.connected)
                return;
        }
        //Start the Counting Timer
        StartTimer();
    }

    public int GetPlayerIdx(ulong clientID)
    {
        for (int i = 0; i < playerConnections.Length; i++)
        {
            if (playerConnections[i].clientID == clientID)
                return i;
        }

        return -1;
    }

    public int GetTotalColors()
    {
        return availableColors.Length;
    }

    public Color GetColor(int idx)
    {
        if (idx < availableColors.Length)
            return availableColors[idx];
        else
            return Color.clear;
    }
}
