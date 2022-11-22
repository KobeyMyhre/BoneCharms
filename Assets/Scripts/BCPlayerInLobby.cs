using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BCPlayerInLobby : NetworkBehaviour
{
    //public NetworkVariable<bool> playerReady = new NetworkVariable<bool>(false);
    public NetworkVariable<int> playerID = new NetworkVariable<int>(-1);
    public NetworkVariable<int> roundPoints = new NetworkVariable<int>();

    private void Start()
    {
        if(IsServer)
        {
            playerID.Value = LobbyHandler.instance.GetPlayerIdx(OwnerClientId);
            roundPoints.Value = 0;
        }
        else if(!IsOwner){

        }

        DontDestroyOnLoad(gameObject);
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

    public void AddRoundPoints(BaseHand myHand)
    {
        roundPoints.Value += myHand.myHand.Count;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Debug.Log("Dont do This");
    }

    //public void ReadyUp(bool val)
    //{
    //    playerReady.Value = val;
    //}
}
