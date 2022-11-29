using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BoneYard : NetworkBehaviour
{
    //public float radius;
    public BoneCharm prefab;
    public BoneYardUI boneYardUI;
    public float xSpacing;
    public float ySpacing;
    public int rowsToDisplay;
    //public GameObject boneYardDrawHintDisplay;
    public Transform topHalf;
    public Transform botHalf;

    public List<Transform> cornerSpots;

    public List<BoneCharm> boneYard;
    List<BoneCharm> initialDraft = new List<BoneCharm>();


    private void Start()
    {
        for(int i = 0; i < cornerSpots.Count; i++)
        {
            float angle = Random.Range(2, 8);
            int sign = i % 2 == 0 ? -1 : 1;
            cornerSpots[i].Rotate(0, 0, angle * sign);
        }
    }

    bool isInDraft = false;
    public void BeginDraft(List<BoneCharm> freshSet)
    {
        SetIsInDraft(true);
        boneYard = new List<BoneCharm>();
        initialDraft = new List<BoneCharm>();
        for(int i = 0; i < freshSet.Count; i++)
        {
            AddBoneCharmToBoneYard(freshSet[i], false);
        }
        TurnManager.instance.SendStartDraftToClients();

        ShuffleBoneYard();
    }

    public void AddNetworkSpawnToBoneyard(BoneCharm charm)
    {
        if(boneYard == null) { boneYard = new List<BoneCharm>(); }
        boneYard.Add(charm);
    }

    public void AddBoneCharmToBoneYard(BoneCharm boneCharm, bool duringGame = true)
    {
        //boneCharm.transform.SetParent(boneYard.Count >= Mathf.RoundToInt(28 / 2.0f) ? botHalf : topHalf);
        boneCharm.transform.localScale = Vector3.one;
        boneCharm.SetRevealedState(false);
        boneCharm.SetOrientation(BoardCenter.eDirection.eNorth);
        boneCharm.UpdateLocation(eLocation.eBoneYard);
        boneYard.Add(boneCharm);
        if (duringGame)
        {
            boneCharm.SetPreviousCharm(null);
            boneCharm.SetNextCharm(null);
            boneCharm.SetOwnerHand(null);
            //boneCharm.UpdateBoneCharmSelectedEvent(TurnManager.instance.GetPlayerHostHand().AddBoneToHand_ServerRequest);
            boneCharm.UpdateBoneCharmSelectedEvent(AddBoneToPlayerHandRequest);
            MoveBoneYardToCorners();
            ShuffleBoneYard();
        }
        else
        {
            //boneCharm.UpdateBoneCharmSelectedEvent(BoneCharmSelected_BoneYard);
            SetBoneYardPositions();
        }
    }

    public void SetIsInDraft(bool val)
    {
        isInDraft = val;
    }

    public bool IsEmpty()
    {
        return boneYard.Count == 0;
    }

    public void ClearBoneYardSelectedEvents()
    {
        foreach(BoneCharm charm in boneYard)
        {
            charm.ClearBoneCharmSelectedEvent();
        }
    }

    public void SetBoneYardDraftActionEvent()
    {
        foreach (BoneCharm charm in boneYard)
        {
            charm.UpdateBoneCharmSelectedEvent(BoneCharmSelected_BoneYard);
        }
    }

    //May need a ServerRpc for the clients to use
    public void BoneCharmSelected_BoneYard(BoneCharm charm)
    {
        if(charm == null) { return; }
        if (IsServer)
        {
            Debug.Log("Server Doing Draft Action");
            if (isInDraft)
            {
                charm.SetRevealedState(true);
                initialDraft.Add(charm);
                if (charm.IsDouble())
                {
                    EndDraft(charm);
                }
                else
                {
                    ClearBoneYardSelectedEvents();
                    TurnManager.instance.TickDraftAction();
                }
            }
        }
        else
        {
            Debug.Log("Telling Server my Draft Choice");
            ClearBoneYardSelectedEvents();
            
            RevealBoneCharmServerRpc(charm.topCharmType.Value, charm.botCharmType.Value);
        }
    }

    [ServerRpc(RequireOwnership =false)]
    void RevealBoneCharmServerRpc(eCharmType topCharmType, eCharmType botCharmType)
    {
        if (IsServer)
        {
            Debug.Log("Receiving User Draft Choice");
            BoneCharmSelected_BoneYard(BoneCharmManager.GetCharmInCollection(boneYard, topCharmType, botCharmType));
        }
        Debug.Log("SERVER ONLY");
    }

    void EndDraft(BoneCharm chosenDouble)
    {
        SetIsInDraft(false);
        ClearBoneYardSelectedEvents();
        EndDraftClientRpc();
        //End Draft
        List<BoneCharm> firstPlayerHand = GetInitialHand(4);
        //Add Double to Player Hand
        firstPlayerHand.Add(chosenDouble);

        boneYard.Remove(chosenDouble);

        BaseHand firstPlayer = TurnManager.instance.GetActivePlayerHand();
        firstPlayer.InitHand(firstPlayerHand);
        TurnManager.instance.GivePlayerInitialHandClientRpc(firstPlayer.playerID, BoneCharmManager.GetCharmNetDataList(firstPlayerHand));

        List<BaseHand> otherPlayers = TurnManager.instance.GetNonActivePlayerHands();
        foreach(BaseHand hand in otherPlayers)
        {
            List<BoneCharm> aiHand = GetInitialHand(5);
            hand.InitHand(aiHand);
            TurnManager.instance.GivePlayerInitialHandClientRpc(hand.playerID, BoneCharmManager.GetCharmNetDataList(aiHand));
        }

        //TurnManager.instance.StartTurns(firstPlayer);
        



        // DisplayBoneYard(false);
        boneYardUI.UpdateBoneYardUI(this);
        MoveBoneYardToCorners();

        //UpdateCharmsInYardToDrawOnSelect();
        Debug.Log("End Draft");
        TurnManager.instance.SendStartTurnsToClients();
    }

    [ClientRpc]
    void EndDraftClientRpc()
    {
        SetIsInDraft(false);
        MoveBoneYardToCorners();
        //ClearBoneYardSelectedEvents();
        Debug.Log("End Draft Client Rpc");
    }

    public void AddBoneToPlayerHandRequest(BoneCharm boneCharm)
    {
        AddBoneToHandServerRpc(boneCharm.GetCharmNetData(), NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddBoneToHandServerRpc(BoneCharmNetData charmData, ulong handClientID)
    {
        AddBoneToHandClientRpc(charmData, handClientID);
    }

    [ClientRpc]
    public void AddBoneToHandClientRpc(BoneCharmNetData charmData, ulong handClientID)
    {
        BoneCharm charm = BoneCharmManager.instance.GetCharmFromNetData(charmData);
        BaseHand playerHand = TurnManager.instance.GetPlayerHandFromID(handClientID);
        RemoveBoneCharm(charm);
        if (charm && playerHand)
        {
            playerHand.AddBoneToHand_FromBoneyard(charm);
        }
    }


    public void OverrideCharmsInYardSelectEvent(OnEventBoneCharm eventBoneCharm)
    {
        foreach (BoneCharm charm in boneYard)
        {
            charm.UpdateBoneCharmSelectedEvent(eventBoneCharm);
        }
    }

    //This should only be assigned at the start of the turn
    public void UpdateCharmsInYardToDrawOnSelect()
    {
        foreach (BoneCharm charm in boneYard)
        {
            charm.UpdateBoneCharmSelectedEvent(AddBoneToPlayerHandRequest);
            //charm.UpdateBoneCharmSelectedEvent(TurnManager.instance.GetPlayerHostHand().AddBoneToHand_ServerRequest);
        }
    }

    void MoveBoneYardToCorners()
    {
        for (int i = 0; i < boneYard.Count; i++)
        {
            if(i < cornerSpots.Count)
            {
                //boneYard[i].transform.SetParent(cornerSpots[i]);
                boneYard[i].transform.position = cornerSpots[i].position;
                boneYard[i].transform.localRotation = Quaternion.identity;
            }
        }

    }

    public void UpdateBoneCharmPosition_BoneYard(BoneCharm charm, int placementIdx)
    {
        if(placementIdx < cornerSpots.Count)
            charm.transform.position = cornerSpots[placementIdx].position;
    }

   
    public void SetBoneYardPositions()
    {
        for(int i = 0; i < boneYard.Count; i++)
        {
            boneYard[i].transform.position = GetDraftPositionInBoneYard(i);
        }
    }

    //Redo this to pre-built positions and replace them like the hand
    Vector3 GetDraftPositionInBoneYard(int idx)
    {
        //return transform.position + ((Vector3)(Random.insideUnitCircle) * radius);
        int index = idx;

        int xOffset = index % rowsToDisplay;
        int yOffset = (int)(index / (float)rowsToDisplay);

        Vector3 retval = topHalf.position;
        if(index >= (28 / 2.0f))
        {
            index -= Mathf.RoundToInt(28 / 2.0f);
            retval = botHalf.position;
            yOffset = (int)(index / (float)rowsToDisplay);
        }
        
        retval += (Vector3.right * (xSpacing * prefab.GetWidth())) * xOffset;
        retval += (Vector3.down * (ySpacing * prefab.GetHeight())) * yOffset;

        return retval;
    }

    private void OnDrawGizmosSelected()
    {
        
    }

    public void ShuffleBoneYard()
    {
        int shuffles = Random.Range(2, 5);
        for(int x = 0; x < shuffles; x++)
        {
            for (int i = 0; i < boneYard.Count; i++)
            {
                int r = Random.Range(0, boneYard.Count);
                Vector3 posA = boneYard[i].transform.position;
                Vector3 posB = boneYard[r].transform.position;
                //Maybe Swap Parents Too?

                Transform parent = boneYard[i].transform.parent;
                //boneYard[i].transform.SetParent(boneYard[r].transform.parent);
                //boneYard[r].transform.SetParent(parent);

                boneYard[i].transform.position = posB;
                boneYard[r].transform.position = posA;
            }
        }
        
    }

    public void OnTurnUpdate(BC_Player bc_Player)
    {
        if (bc_Player.isMe)
        {
            UpdateCharmsInYardToDrawOnSelect();
        }
        else
        {
            ClearBoneYardSelectedEvents();
        }
        //DisplayBoneYardDrawHint(false);
        //DisplayBoneYard(false);
        //ClearBoneYardSelectedEvents();
        //if(!bc_Player.playerHand.HasValidPlay())
        //{
        //    DisplayBoneYardDrawHint(true);
        //    //foreach(BoneCharm charm in boneYard)
        //    //{
        //    //    charm.UpdateBoneCharmSelectedEvent(bc_Player.playerHand.AddBoneToHand_FromBoneyard);
        //    //}
        //}
    }

    public List<BoneCharm> RevealBoneCharmInBoneYard(int amount)
    {
        //int count = 0;
        //int safety = 0;
        //while(count < amount && boneYard.Count > 0)
        //{
        //    int r = Random.Range(0, boneYard.Count);
        //    if(!boneYard[r].IsRevealed())
        //    {
        //        boneYard[r].SetFlipState(true);
        //        count++;
        //    }
        //    safety++;
        //    if(safety > 500)
        //    {
        //        break;
        //    }
        //}
        List<BoneCharm> HiddenCharms = GetRandomNonRevealedCharms(amount, false);
        for(int i = 0; i < HiddenCharms.Count; i++)
        {
            if(HiddenCharms[i] != null)
            {
                HiddenCharms[i].SetRevealedState(true);
            }
        }
        boneYardUI.UpdateBoneYardUI(this);
        return HiddenCharms;
    }

    public BoneCharm GetRandomBoneCharm()
    {
        if(boneYard.Count == 0) { return null; }
        int r = Random.Range(0, boneYard.Count);

        BoneCharm retval = boneYard[r];
        boneYard.RemoveAt(r);
        boneYardUI.UpdateBoneYardUI(this);
        return retval;
    }

    public BoneCharm GetRandomHiddenCharm(bool withRemoval = true)
    {
        List<BoneCharm> charms = GetHiddenCharms();
        if(charms.Count == 0) { return null; }
        int r = Random.Range(0, charms.Count);
        if (withRemoval)
        {
            RemoveBoneCharm(charms[r]);
        }
        return charms[r];
    }

    public BoneCharm GetRandomCharm_AI()
    {
        if(boneYard.Count == 0) { return null; }
        BoneCharm best = GetRandomBoneCharm();
        if(best != null) { return best; }
        int r = Random.Range(0, boneYard.Count);
        boneYard.RemoveAt(r);
        return boneYard[r];
    }

    public void RemoveBoneCharm(BoneCharm charm)
    {
        boneYard.Remove(charm);
        boneYardUI.UpdateBoneYardUI(this);
        MoveBoneYardToCorners();
    }

    public void RemoveBoneCharms(List<BoneCharm> charms)
    {
        foreach(BoneCharm c in charms)
        {
            boneYard.Remove(c);
        }
        boneYardUI.UpdateBoneYardUI(this);
        MoveBoneYardToCorners();
    }

    List<BoneCharm> GetInitialHand(int count)
    {
        List<BoneCharm> hiddens = GetRandomNonRevealedCharms(count, true);
        while(boneYard.Count > 0 && hiddens.Count < count)
        {
            int r = Random.Range(0, boneYard.Count);
            hiddens.Add(boneYard[r]);
            boneYard.RemoveAt(r);
        }
        return hiddens;
    }

    List<BoneCharm> GetRandomNonRevealedCharms(int count, bool remove = true)
    {
        List<BoneCharm> retval = new List<BoneCharm>();
        List<BoneCharm> options = GetHiddenCharms();
        int safety = 0;
        while (retval.Count < count && options.Count > 0)
        {
            int r = Random.Range(0, options.Count);
            retval.Add(options[r]);
            options.RemoveAt(r);
            //if (!boneYard[r].IsRevealed())
            //{

            //}
            safety++;
            if (safety > 500)
            {
                break;
            }
        }

        if (remove)
        {
            foreach (BoneCharm charm in retval)
            {
                RemoveBoneCharm(charm);
            }
        }

        boneYardUI.UpdateBoneYardUI(this);
        return retval;
    }

    public int GetNonRevealedCharms()
    {
        int retval = 0;

        for (int i = 0; i < boneYard.Count; i++)
        {
            if (!boneYard[i].IsRevealed())
            {
                retval++;
            }
        }

        return retval;
    }

    public List<BoneCharm> GetHiddenCharms()
    {
        List<BoneCharm> retval = new List<BoneCharm>();

        for (int i = 0; i < boneYard.Count; i++)
        {
            if (!boneYard[i].IsRevealed())
            {
                retval.Add(boneYard[i]);
            }
        }

        return retval;
    }

    public List<BoneCharm> GetRevealedCharms()
    {
        List<BoneCharm> retval = new List<BoneCharm>();

        for (int i = 0; i < boneYard.Count; i++)
        {
            if (boneYard[i].IsRevealed())
            {
                retval.Add(boneYard[i]);
            }
        }

        return retval;
    }

    //public void DisplayBoneYardDrawHint(bool val)
    //{
    //    boneYardDrawHintDisplay.SetActive(val);
    //}

    public void OnDrawGizmos()
    {
        if (prefab != null)
        {
            int total = 28;
            for (int i = 0; i < total / 2; i++)
            {
                int xOffset = i % rowsToDisplay;
                int yoffset = (int)(i / (float)rowsToDisplay);

                Vector3 pos = topHalf.position;
                pos += (Vector3.right * (xSpacing * prefab.GetWidth())) * xOffset;
                pos += (Vector3.down * (ySpacing * prefab.GetHeight())) * yoffset;

                Gizmos.DrawWireCube(pos, new Vector3(prefab.GetWidth(), prefab.GetHeight(), 1));
            }
            for (int i = (total / 2); i < total; i++)
            {
                int xOffset = i % rowsToDisplay;
                int yoffset = (int)((i - (total / 2)) / (float)rowsToDisplay);

                Vector3 pos = botHalf.position;
                pos += (Vector3.right * (xSpacing * prefab.GetWidth())) * xOffset;
                pos += (Vector3.down * (ySpacing * prefab.GetHeight())) * yoffset;
                Gizmos.DrawWireCube(pos, new Vector3(prefab.GetWidth(), prefab.GetHeight(), 1));
            }
            for(int i = 0; i < cornerSpots.Count; i++)
            {
                Gizmos.DrawWireCube(cornerSpots[i].position, new Vector3(prefab.GetWidth(), prefab.GetHeight(), 1));
            }
            //Gizmos.DrawWireCube(botHalf.position, new Vector3(prefab.GetWidth(), prefab.GetHeight(), 1));
        }
    }
}
