using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ClientConnection : NetworkBehaviour
{
    public static ClientConnection instance;

    public int maxConnections = 4;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public bool IsExtraClient(ulong clientID)
    {
        return CanConnect(clientID);
    }

    public bool CanClientConnection(ulong clientID)
    {
        if (!IsServer)
            return false;

        bool canConnect = CanConnect(clientID);
        if(!canConnect)
        {
            RemoveClient(clientID);
        }

        return canConnect;
    }

    public bool CanConnect(ulong clientID)
    {
        if(SceneLoader.instance.currScene == eScenes.Lobby)
        {
            int playersConnected = NetworkManager.Singleton.ConnectedClientsList.Count;

            if(playersConnected > maxConnections)
            {
                Debug.Log("Lobby Is Full");
                return false;
            }

            Debug.Log("You Can Enter the Lobby " + clientID);
            return true;
        }
        else
        {
            if(ItHasACharacterSelect(clientID))
            {
                Debug.Log("You Can Enter the Lobby " + clientID);
                return true;
            }
            else
            {
                Debug.Log("Lobby Is Full");
                return false;
            }
        }
    }

    public void RemoveClient(ulong clientID)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientID}
            }
        };

        ShutDownClientRpc(clientRpcParams);
    }

    public bool ItHasACharacterSelect(ulong clientID)
    {
        return true;
    }

    [ClientRpc]
    private void ShutDownClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Shutdown();
    }

    private void Shutdown()
    {
        NetworkManager.Singleton.Shutdown();
        SceneLoader.instance.LoadMainMenuScene();
    }
}
