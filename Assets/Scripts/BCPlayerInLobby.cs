using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BCPlayerInLobby : NetworkBehaviour
{
    //public NetworkVariable<bool> playerReady = new NetworkVariable<bool>(false);
    public NetworkVariable<int> playerID = new NetworkVariable<int>(-1);


    private void Start()
    {
        if(IsServer)
        {
            playerID.Value = LobbyHandler.instance.GetPlayerIdx(OwnerClientId);
        }
        else if(!IsOwner){

        }
        BCPlayersInGame.instance.AddPlayer(this);
    }

    public void ReadyButton()
    {
        if (!IsOwner) { return; }
        ReadyServerRpc();
    }

    public void NotReadyButton()
    {
        if (!IsOwner) { return; }
        NotReadyServerRpc();
    }

    [ServerRpc]
    public void ReadyServerRpc()
    {
        //Ready Up W/ Server
        LobbyHandler.instance.PlayerReady(OwnerClientId, playerID.Value);
    }

    [ServerRpc]
    public void NotReadyServerRpc()
    {
        //Not Ready Up W/ Server
        LobbyHandler.instance.PlayerNotReady(OwnerClientId, false);
    }

    //public void ReadyUp(bool val)
    //{
    //    playerReady.Value = val;
    //}
}
