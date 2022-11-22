using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public struct BoneCharmNetData : INetworkSerializable
{
    public eCharmType topCharm;
    public eCharmType botCharm;

    public BoneCharmNetData(BoneCharm charm)
    {
        topCharm = charm.topCharmType.Value;
        botCharm = charm.botCharmType.Value;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref topCharm);
        serializer.SerializeValue(ref botCharm);
    }
}


public class BoneCharmManager : NetworkBehaviour
{
    public static BoneCharmManager instance;
    public BoneYard boneYard;
    public EffectTargettingHandler effectTargetting;


    public BoneCharm boneCharmPrefab;
    List<BoneCharm> boneCharmSet;
    public BoneCharm_UI boneCharmUIPrefab;

    public Sprite pinkCharm;
    public Sprite yellowCharm;
    public Sprite greenCharm;
    public Sprite brownCharm;
    public Sprite blueCharm;
    public Sprite purpleCharm;
    public Sprite blankCharm;



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

    private void Start()
    {
        if (IsServer)
        {
            StartCoroutine(WaitForPlayers());
        }
    }

    IEnumerator WaitForPlayers()
    {
        yield return new WaitForSeconds(2.5f);
        CreateBoneCharmSet();
        SendSetToBoneYard();
    }

    public int GetTotalCharmsInSet()
    {
        return 28;
    }

    public BoneCharm_UI GetBoneCharmUI(Transform holder)
    {
        GameObject newObj = Instantiate(boneCharmUIPrefab.gameObject, holder);
        BoneCharm_UI newCharmUI = newObj.GetComponent<BoneCharm_UI>();

        return newCharmUI;
    }

    public void AddNetworkSpawnToNewSet(BoneCharm charm)
    {
        if(boneCharmSet == null) { boneCharmSet = new List<BoneCharm>(); }
        boneCharmSet.Add(charm);
        boneYard.boneYard.Add(charm);
        boneYard.SetBoneYardPositions();
        //UpdateBoneCharmPositions();
    }

    public void CreateBoneCharmSet()
    {
        boneCharmSet = new List<BoneCharm>();
        int numCharmType = (int)eCharmType.eSizeOfCharms;
        Vector3 startPos = transform.position;
        for(int i = 0; i < numCharmType; i++)
        {
            for(int j = i; j < numCharmType; j++)
            {
                eCharmType newTopType = (eCharmType)i;
                eCharmType newBotType = (eCharmType)j;

                //GameObject newBaby = Instantiate(boneCharmPrefab.gameObject, transform);
                GameObject newBaby = NetworkObjectSpawner.SpawnNewNetworkObject(boneCharmPrefab.gameObject);
                BoneCharm newCharm = newBaby.GetComponent<BoneCharm>();
                if(Random.Range(0,100) >= 50)
                {
                    newCharm.InitBoneCharm(newBotType, newTopType);
                }
                else
                {
                    newCharm.InitBoneCharm(newTopType, newBotType);
                }
                newCharm.charmLocation.Value = eLocation.eBoneYard;
                newCharm.transform.position = startPos;
                boneCharmSet.Add(newCharm);
                startPos += Vector3.right;
            }
            startPos += Vector3.up;
        }
    }

    void SendSetToBoneYard()
    {
        if(IsServer)
        {
            boneYard.BeginDraft(boneCharmSet);
        }
        else
        {
            //boneYard.boneYard = boneCharmSet;
            //UpdateBoneCharmPositions();
        }
        //for (int i = 0; i < boneCharmSet.Count; i++)
        //{
        //    boneYard.AddBoneCharmToBoneYard(boneCharmSet[i], false);
        //}
        //boneYard.ShuffleBoneYard();
    }

    public void UpdateBoneCharmPositions()
    {
        int boneYardCount = 0;
        for(int i = 0; i < boneCharmSet.Count; i++)
        {
            switch (boneCharmSet[i].charmLocation.Value)
            {
                case eLocation.eBoneYard:
                    boneYard.UpdateBoneCharmPosition_BoneYard(boneCharmSet[i], boneYardCount);
                    boneYardCount++;
                    break;
                case eLocation.ePlayerHand:
                    BaseHand ownerHand = TurnManager.instance.GetPlayerHandFromID((ulong)boneCharmSet[i].playerID.Value);
                    if(ownerHand != null)
                    {
                        //Place Here
                    }
                    break;
                case eLocation.eBoard:

                    break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResolveBoneCharmEffectServerRpc(BoneCharmNetData netDataA, BoneCharmNetData netDataB, eCharmType charmType, bool north, ulong targetOpponentID)
    {
        ResolveBoneCharmEffectClientRpc(netDataA, netDataB, charmType, north, targetOpponentID);
    }

    [ServerRpc(RequireOwnership = false)]
    public void GreenCharmServerRpc(ulong clientID)
    {
        BaseHand targetHand = TurnManager.instance.GetPlayerHandFromID(clientID);
        if(targetHand != null)
        {
            BoneCharm revealed = targetHand.RevealRandomCharm();
            if (revealed != null) { revealed.PlayTargettedAoE(eCharmType.eGreenCharm); }
        }
        GameplayTransitions.instance.PassTurn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void YellowCharmServerRpc(ulong clientID, BoneCharmNetData dataA, BoneCharmNetData dataB)
    {
        YellowCharmClientRpc(clientID, dataA, dataB);
    }

    [ClientRpc]
    public void YellowCharmClientRpc(ulong clientID, BoneCharmNetData dataA, BoneCharmNetData dataB)
    {
        BoneCharm swapA = GetCharmFromNetData(dataA);
        BoneCharm swapB = GetCharmFromNetData(dataB);

        BaseHand handA = swapA.GetOwnerHand();
        BaseHand handB = swapB.GetOwnerHand();
        if(handA == null)
        {
            if(handB != null)
            {
                handB.RemoveCharmFromHand(swapB);
                boneYard.RemoveBoneCharm(swapA);

                handB.AddBoneToHand(swapA);
                boneYard.AddBoneCharmToBoneYard(swapB);
            }
        }
        else if(handB == null)
        {
            if(handA != null)
            {
                handA.RemoveCharmFromHand(swapA);
                boneYard.RemoveBoneCharm(swapB);

                handA.AddBoneToHand(swapB);
                boneYard.AddBoneCharmToBoneYard(swapA);
            }
        }
        else
        {
            if(handA != null && handB != null)
            {
                handA.RemoveCharmFromHand(swapA);
                handB.RemoveCharmFromHand(swapB);
                swapA.SetRevealedState();
                swapB.SetRevealedState();

                handA.AddBoneToHand(swapB);
                handB.AddBoneToHand(swapA);
            }
        }
        GameplayTransitions.instance.PassTurn();
    }

    [ClientRpc]
    public void WhiteCharmClientRpc(bool north)
    {
        if (north)
        {
            BoneCharm toYard = BoardCenter.instance.RemoveSouthTrack();
            toYard.PlayTargettedAoE(eCharmType.eWhiteCharm);
            if (toYard == BoardCenter.instance.southCharm)
            {
                Debug.LogError("White Charm Broke");
            }
            boneYard.AddBoneCharmToBoneYard(toYard);
        }
        else
        {
            BoneCharm toYard = BoardCenter.instance.RemoveNorthTrack();
            toYard.PlayTargettedAoE(eCharmType.eWhiteCharm);
            if (toYard == BoardCenter.instance.northCharm)
            {
                Debug.LogError("White Charm Broke");
            }
            boneYard.AddBoneCharmToBoneYard(toYard);
        }
        GameplayTransitions.instance.PassTurn(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PurpleCharmServerRpc(ulong clientID)
    {
        PurpleCharmClientRpc(clientID);
        GameplayTransitions.instance.PassTurn();
    }

    [ClientRpc]
    public void PurpleCharmClientRpc(ulong clientID)
    {
        BaseHand targetHand = TurnManager.instance.GetPlayerHandFromID(clientID);
        if(targetHand != null)
        {
            BoneCharm newCharm = boneYard.GetRandomBoneCharm();
            if (newCharm != null)
            {
                newCharm.PlayTargettedAoE(eCharmType.ePurpleCharm);
                targetHand.AddBoneToHand(newCharm);
            }
        }

    }


    [ServerRpc]
    public void RequestBoneCharmResolutionServerRpc(eCharmType charmType, bool north, ulong clientID)
    {
        switch (charmType)
        {
            case eCharmType.ePinkCharm:
                List<BoneCharm> revealedCharms = boneYard.RevealBoneCharmInBoneYard(2);
                for (int i = 0; i < revealedCharms.Count; i++)
                {
                    if (revealedCharms[i] != null)
                    {
                        revealedCharms[i].PlayTargettedAoE(charmType);
                    }
                }
                GameplayTransitions.instance.PassTurn(false);
                break;
            case eCharmType.eYellowCharm:
                TellClientToSelectCharmSwapClientRpc(clientID, charmType);
                //GameplayTransitions.instance.PassTurn(false);
                break;
            case eCharmType.eGreenCharm:
                TellClientToSelectPlayerClientRpc(clientID, charmType);
                //effectTargetting.InitiateSelectPlayerUI(charmType);
                break;
            case eCharmType.eWhiteCharm:
                WhiteCharmClientRpc(north);
                break;
            case eCharmType.eBlueCharm:
                GameplayTransitions.instance.PassTurn(true);
                break;
            case eCharmType.ePurpleCharm:
                TellClientToSelectPlayerClientRpc(clientID, charmType);
                //effectTargetting.InitiateSelectPlayerUI(charmType);
                break;
            default:
                GameplayTransitions.instance.PassTurn(false);
                break;
        }
    }

    [ClientRpc]
    public void TellClientToSelectPlayerClientRpc(ulong clientID, eCharmType charmType)
    {
        if(clientID == NetworkManager.Singleton.LocalClientId)
        {
            effectTargetting.InitiateSelectPlayerUI(charmType, clientID);
        }
    }

    [ClientRpc]
    public void TellClientToSelectCharmSwapClientRpc(ulong clientID, eCharmType charmType)
    {
        if(clientID == NetworkManager.Singleton.LocalClientId)
        {
            effectTargetting.InitiateSelectBoneCharmUI(charmType);
        }
    }

    public void ResolveBoneCharmEffect(eCharmType charmType, bool north, ulong clientID)
    {
        RequestBoneCharmResolutionServerRpc(charmType, north, clientID);

        //if(TurnManager.instance.GetActivePlayerHand() is PlayerHand)
        //{
        //    if (charmType == eCharmType.eGreenCharm || charmType == eCharmType.ePurpleCharm)
        //    {
        //        effectTargetting.InitiateSelectPlayerUI(null, null, charmType, north);
        //        return;
        //    }
        //    if (charmType == eCharmType.eYellowCharm)
        //    {
        //        effectTargetting.InitiateSelectBoneCharmUI(null, null, charmType, north);
        //        return;
        //    }
        //}

        //ResolveBoneCharmEffectServerRpc(new BoneCharmNetData(), new BoneCharmNetData(), charmType, north, 999);
    }

    [ClientRpc]
    public void ResolveBoneCharmEffectClientRpc(BoneCharmNetData netDataA, BoneCharmNetData netDataB, eCharmType charmType, bool north, ulong targetOpponentID)
    {
        BoneCharm charmA = GetCharmFromNetData(netDataA);
        BoneCharm charmB = GetCharmFromNetData(netDataB);
        BaseHand targetOpponent = TurnManager.instance.GetPlayerHandFromID(targetOpponentID);
        ResolveBoneCharmEffect(charmA, charmB, charmType, north, targetOpponent);
    }

    public void ResolveBoneCharmEffect(BoneCharm charmA, BoneCharm charmB, eCharmType charmType, bool north, BaseHand targetOpponent)
    {
        //if(charmA != null)
        //{
        //    charmA.PlayResolveEffect(charmType);
        //}
        //if(charmB != null)
        //{
        //    charmB.PlayResolveEffect(charmType);
        //}
        bool blueCharm = false;
        //List<BaseHand> opponents;
        int r;
        switch (charmType)
        {
            case eCharmType.eBlankCharm:
                break;
            case eCharmType.ePinkCharm:
                List<BoneCharm> revealedCharms = boneYard.RevealBoneCharmInBoneYard(2);
                for(int i = 0; i < revealedCharms.Count; i++)
                {
                    if(revealedCharms[i] != null)
                    {
                        revealedCharms[i].PlayTargettedAoE(charmType);
                    }
                }
                break;
            case eCharmType.eYellowCharm:
                break;
                if(targetOpponent == null) //Player Version
                {
                    BaseHand swapCharmHandA = charmA.GetOwnerHand();
                    BaseHand swapCharmHandB = charmB.GetOwnerHand();
                    if (swapCharmHandA == null)
                    {
                        charmA.PlayTargettedAoE(charmType);
                        charmB.PlayTargettedAoE(charmType);

                        swapCharmHandB.RemoveCharmFromHand(charmB);
                        boneYard.RemoveBoneCharm(charmA);

                        swapCharmHandB.AddBoneToHand(charmA);
                        boneYard.AddBoneCharmToBoneYard(charmB);
                        //Swap HandB with BoneYard
                    }
                    else if (swapCharmHandB == null)
                    {
                        charmA.PlayTargettedAoE(charmType);
                        charmB.PlayTargettedAoE(charmType);

                        swapCharmHandA.RemoveCharmFromHand(charmA);
                        boneYard.RemoveBoneCharm(charmB);

                        swapCharmHandA.AddBoneToHand(charmB);
                        boneYard.AddBoneCharmToBoneYard(charmA);
                        //Swap HandA with BoneYard
                    }
                    else
                    {
                        charmA.PlayTargettedAoE(charmType);
                        charmB.PlayTargettedAoE(charmType);

                        swapCharmHandA.RemoveCharmFromHand(charmA);
                        swapCharmHandB.RemoveCharmFromHand(charmB);
                        charmA.SetRevealedState();
                        charmB.SetRevealedState();


                        swapCharmHandA.AddBoneToHand(charmB);
                        swapCharmHandB.AddBoneToHand(charmA);
                        //Swap HandA with HandB
                    }
                }
                else //AI Version
                {
                    BoneCharm swapCharmBoneYard = boneYard.GetRandomBoneCharm();
                    if (swapCharmBoneYard != null)
                    {
                        BoneCharm swapCharmHand = targetOpponent.GetRandomCharm();
                        if (swapCharmHand != null)
                        {
                            swapCharmHand.PlayTargettedAoE(charmType);
                            swapCharmBoneYard.PlayTargettedAoE(charmType);

                            targetOpponent.AddBoneToHand(swapCharmBoneYard);
                            boneYard.AddBoneCharmToBoneYard(swapCharmHand);
                        }
                    }
                }
                break;
            case eCharmType.eGreenCharm:
                break;
                BoneCharm revealed = targetOpponent.RevealRandomCharm();
                if(revealed != null) { revealed.PlayTargettedAoE(charmType); }
                break;
            case eCharmType.eWhiteCharm:
                break;
                if(north)
                {
                    BoneCharm toYard = BoardCenter.instance.RemoveSouthTrack();
                    toYard.PlayTargettedAoE(charmType);
                    if(toYard == BoardCenter.instance.southCharm)
                    {
                        Debug.LogError("White Charm Broke");
                    }
                    boneYard.AddBoneCharmToBoneYard(toYard);
                }
                else
                {
                    BoneCharm toYard = BoardCenter.instance.RemoveNorthTrack();
                    toYard.PlayTargettedAoE(charmType);
                    if (toYard == BoardCenter.instance.northCharm)
                    {
                        Debug.LogError("White Charm Broke");
                    }
                    boneYard.AddBoneCharmToBoneYard(toYard);
                }
                break;
            case eCharmType.eBlueCharm:
                blueCharm = true;
                break;
            case eCharmType.ePurpleCharm:
                break;
                BoneCharm newCharm = boneYard.GetRandomBoneCharm();
                if(newCharm != null)
                {
                    newCharm.PlayTargettedAoE(charmType);
                    targetOpponent.AddBoneToHand(newCharm);
                }
                break;
        }
        //TurnManager.instance.TickTurnIdx(blueCharm);
        GameplayTransitions.instance.PassTurn(blueCharm);
    }

    public Color GetBoneCharmColor(eCharmType charmType)
    {
        switch (charmType)
        {
            case eCharmType.ePinkCharm:
                return Color.red;
            case eCharmType.eYellowCharm:
                return Color.yellow;
            case eCharmType.eGreenCharm:
                return Color.green;
            case eCharmType.eWhiteCharm:
                return Color.white;
            case eCharmType.eBlueCharm:
                return Color.cyan;
            case eCharmType.ePurpleCharm:
                return Color.magenta;
            case eCharmType.eBlankCharm:
                return Color.white;
        }
        return Color.clear;
    }

    public Sprite GetBoneCharmSprite(eCharmType charmType)
    {
        switch(charmType)
        {
            case eCharmType.ePinkCharm:
                return pinkCharm;
            case eCharmType.eYellowCharm:
                return yellowCharm;
            case eCharmType.eGreenCharm:
                return greenCharm;
            case eCharmType.eWhiteCharm:
                return brownCharm;
            case eCharmType.eBlueCharm:
                return blueCharm;
            case eCharmType.ePurpleCharm:
                return purpleCharm;
            case eCharmType.eBlankCharm:
                return blankCharm;
        }
        return null;
    }

    public int GetBoneCharmPlayabilityScore(BaseHand myHand, BoneCharm charm, eCharmType matchType)
    {
        int totalValue = 0;
        eCharmType[] charmTypes = charm.GetTypes();
        switch (matchType)
        {
            case eCharmType.ePurpleCharm:
                if(DoesCharmTypesMatch(eCharmType.ePurpleCharm, charmTypes))
                {
                    totalValue += 50;
                }
                break;
            case eCharmType.eBlueCharm:
                if(DoesCharmTypesMatch(eCharmType.eBlueCharm, charmTypes))
                {
                    if(myHand.ContainsCharmType(eCharmType.eBlueCharm))
                    {
                        totalValue += 50;
                    }
                }
                break;
            case eCharmType.eWhiteCharm:
                //If we have a matching type with a previous bonecharm of either end piece?
                break;
            case eCharmType.eGreenCharm:
                //If Opponent has more than 0 hidden charms
                break;
            case eCharmType.eYellowCharm:
                //If Opponent has a revealed matching pieces
                break;
            case eCharmType.ePinkCharm:
                //Other stuff is bad/filler?
                totalValue += 5;
                break;
        }
        return totalValue;
    }

    public BoneCharm GetCharmFromNetData(BoneCharmNetData charmData)
    {
        BoneCharm retval = GetCharmInCollection(boneCharmSet, charmData.topCharm, charmData.botCharm);
        
        return retval;
    }

    public List<BoneCharm> GetCharmsFromNetData(BoneCharmNetData[] charmData)
    {
        List<BoneCharm> retval = new List<BoneCharm>();

        foreach(BoneCharmNetData data in charmData)
        {
            BoneCharm charm = GetCharmInCollection(boneCharmSet, data.topCharm, data.botCharm);
            if(charm != null) { retval.Add(charm); }
        }

        return retval;
    }

    public static bool DoesCharmTypesMatch(eCharmType charmType, eCharmType[] types)
    {
        for(int i =0; i < types.Length; i++)
        {
            if(charmType == types[i])
            {
                return true;
            }
        }
        return false;
    }

    public static BoneCharmNetData GetNetDataFromCharm(BoneCharm charm)
    {
        return new BoneCharmNetData(charm);
    }

    public static BoneCharmNetData[] GetCharmNetDataList(List<BoneCharm> charms)
    {
        List<BoneCharmNetData> retval = new List<BoneCharmNetData>();
        foreach(BoneCharm charm in charms)
        {
            retval.Add(new BoneCharmNetData(charm));
        }
        return retval.ToArray();
    }

    public static BoneCharm GetCharmInCollection(List<BoneCharm> charms, eCharmType topType, eCharmType botType)
    {
        bool needsDouble = topType == botType;
        foreach(BoneCharm charm in charms)
        {
            if(needsDouble && !charm.IsDouble()) { continue; }
            eCharmType[] types = charm.GetTypes();
            if(DoesCharmTypesMatch(topType, types) && DoesCharmTypesMatch(botType, types))
            {
                return charm;
            }
        }
        Debug.LogError("Couldnt Find Charm In Collection");
        return null;
    }
}
