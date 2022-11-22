using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public class BoardContainer
{
    public List<BoneCharm> board = new List<BoneCharm>();
    BoneCharm centerPiece = null;
    public void AddToBoard(BoneCharm charm, bool north)
    {
        if (board.Contains(charm)) { return; }
        if(board.Count == 0) { centerPiece = charm; board.Add(charm); return; }

        if(north)
        {
            board.Insert(0, charm);
        }
        else
        {
            board.Add(charm);
        }
    }

    public BoneCharm GetNorthEnd(bool withRemoval)
    {
        BoneCharm retval = board[0];
        if (withRemoval)
        {
            board.RemoveAt(0);
            if(retval == centerPiece)
            {
                if (board.Count > 0)
                    centerPiece = board[0];
            }
        }
        return retval;
    }

    public BoneCharm GetSouthEnd(bool withRemoval)
    {
        BoneCharm retval = board[board.Count - 1];
        if(withRemoval)
        {
            board.RemoveAt(board.Count - 1);
            if(retval == centerPiece)
            {
                if (board.Count > 0)
                    centerPiece = board[board.Count - 1];
            }
        }
        return retval;
    }

    public int GetNorthCount()
    {
        int retval = 1;
        for(int i = 0; i < board.Count; i++)
        {
            if(board[i] == centerPiece)
            {
                return retval;
            }
            retval++;
            //else
            //{
            //    retval++;
            //}
        }
        return retval;
    }

    public int GetSouthCount()
    {
        int retval = 1;
        for(int i = board.Count - 1; i >= 0; i--)
        {
            if (board[i] == centerPiece)
            {
                return retval;
            }
            retval++;
            //else
            //{
            //    retval++;
            //}
        }
        return retval;
    }

    public bool IsBoardEmpty()
    {
        return board.Count == 0;
    }

    public List<BoneCharm> GetBoard()
    {
        return board;
    }
}



public class BoardCenter : NetworkBehaviour
{
    public static BoardCenter instance;
    
    public BoneCharm prefab;

    public enum eDirection
    {
        eNorth,
        eEast,
        eSouth,
        eWest
    }

    public Transform boardScalar;
    public Vector3 boardExtents;
    


    public SpriteRenderer northEndIcon;
    public BoneCharm northCharm;

    public NetworkVariable<eCharmType> northType = new NetworkVariable<eCharmType>(eCharmType.eSizeOfCharms);
    public eDirection northTrackDirection;
    eDirection prevNorthDir;
    bool northVerticalSwitch;
    public int northTrackCount;

    //Break this down to a linked list at some point
    //public List<BoneCharm> charmsInChain;
    public BoardContainer boardChains;
    //public List<BoneCharm> northChain = new List<BoneCharm>();
    //public List<BoneCharm> southChain = new List<BoneCharm>();
    public Bounds charmsBounds;
    public Transform boardExtentTopLeft;
    public Transform boardExtentBotRight;

    public SpriteRenderer southEndIcon;
    public BoneCharm southCharm;
    public NetworkVariable<eCharmType> southType = new NetworkVariable<eCharmType>(eCharmType.eSizeOfCharms);
    public eDirection southTrackDirection;
    eDirection prevSouthDir;
    bool southVerticalSwitch;
    public int southTrackCount;
    bool awkwardPlacement = false;

    public int maxVerticalCount;
    public int maxHorizontalCount;
    public float minBoardScale;

    public float maximumPlacementDistance;

    public override void OnNetworkSpawn()
    {
        northType.Value = eCharmType.eSizeOfCharms;
        southType.Value = eCharmType.eSizeOfCharms;
    }

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            SetNorthDirection(eDirection.eNorth);
            northVerticalSwitch = false;
            northTrackCount = 0;
            southTrackCount = 0;
            SetSouthDirection(eDirection.eSouth);
            southVerticalSwitch = false;
            charmsBounds = new Bounds(transform.position,Vector3.zero);
            if (!IsServer)
            {
                northType.OnValueChanged += UpdateNorthIcon;
                southType.OnValueChanged += UpdateSouthIcon;
            }
        }
        else
        {
            Destroy(this);
        }
    }

    void SetNorthDirection(eDirection dir)
    {
        prevNorthDir = northTrackDirection;
        northTrackDirection = dir;
    }

    void SetSouthDirection(eDirection dir)
    {
        prevSouthDir = southTrackDirection;
        southTrackDirection = dir;
    }

    public bool IsCharmValidOnBoard(BoneCharm newCharm)
    {
        if (boardChains.IsBoardEmpty()) { return true; }
        //if(northChain.Count == 0 && southChain.Count == 0) { return true; }
        eCharmType[] types = newCharm.GetTypes();
        for(int i = 0; i < types.Length; i++)
        {
            if(types[i] == GetNorthType())
            {
                return true;
            }
            if(types[i] == GetSouthType())
            {
                return true;
            }
        }
        return false;
    }

    //public bool PlayBoneCharm(BoneCharm newCharm, BaseHand originHand, bool northTrack, int negativeOne)
    //{
    //    eCharmType[] types = newCharm.GetTypes();
    //    for (int i = 0; i < types.Length; i++)
    //    {
    //        if(northTrack)
    //        {
    //            if (types[i] == GetNorthType())
    //            {
    //                //newCharm.ClearBoneCharmSelectedEvent();
    //                originHand.RemoveCharmFromHand(newCharm);
    //                PlaceNewNorthTile(newCharm);
    //                return true;
    //            }
    //        }
    //        else
    //        {
    //            if (types[i] == GetSouthType())
    //            {
    //                //newCharm.ClearBoneCharmSelectedEvent();
    //                originHand.RemoveCharmFromHand(newCharm);
    //                PlaceNewSouthTile(newCharm);
    //                return true;
    //            }
    //        }
    //    }
    //    return false;
    //}

    public bool IsBoardEmpty()
    {
        return boardChains.IsBoardEmpty();
    }

    [ClientRpc]
    public void PlayBoneCharmClientRpc(BoneCharmNetData playedCharm, ulong clientID, bool north)
    {

        if (IsServer) { return; }
        BoneCharm newCharm = BoneCharmManager.instance.GetCharmFromNetData(playedCharm);
        //Remove Bonecharm from owner hand
        BaseHand ownerHand = TurnManager.instance.GetPlayerHandFromID(clientID);
        ownerHand.RemoveCharmFromHand(newCharm);

        Debug.Log("Client Rpc Play BoneCharm");
        if (boardChains.IsBoardEmpty())
        {
            PlayBoneCharm(newCharm, ownerHand, true, clientID);

            
        }
        else
        {
            if (north)
            {
                PlaceNewNorthTile(newCharm, clientID);
            }
            else
            {
                PlaceNewSouthTile(newCharm, clientID);
            }
        }
        // Move Position Onto Board
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayBoneCharmServerRpc(BoneCharmNetData netData, ulong clientID, bool north)
    {
        BoneCharm playedCharm = BoneCharmManager.instance.GetCharmFromNetData(netData);
        if(playedCharm != null)
        {
            BaseHand ownerHand = TurnManager.instance.GetPlayerHandFromID((ulong)clientID);
            if(ownerHand != null)
            {
                Debug.Log("Server Rpc Play BoneCharm");
                ownerHand.RemoveCharmFromHand(playedCharm);
                if (boardChains.IsBoardEmpty())
                {
                    PlayBoneCharm(playedCharm, ownerHand, true, clientID);
                    PlayBoneCharmClientRpc(BoneCharmManager.GetNetDataFromCharm(playedCharm), ownerHand.playerID, true);
                    return;
                }
                if (north)
                {
                    PlaceNewNorthTile(playedCharm, clientID);
                    PlayBoneCharmClientRpc(BoneCharmManager.GetNetDataFromCharm(playedCharm), ownerHand.playerID, true);
                }
                else
                {
                    PlaceNewSouthTile(playedCharm, clientID);
                    PlayBoneCharmClientRpc(BoneCharmManager.GetNetDataFromCharm(playedCharm), ownerHand.playerID, false);
                }
            }
        }
    }

    //0 - center, 1 - north, 2 - south
    public int CanPlayBoneCharm(BoneCharm newCharm)
    {
        if (boardChains.IsBoardEmpty()) { return 0; }
        eCharmType[] types = newCharm.GetTypes();
        for(int i = 0; i < types.Length; i++)
        {
            if(types[i] == GetNorthType()) { return 1; }
            if(types[i] == GetSouthType()) { return 2; }
        }
        return -1;
    }

    public bool CanPlayBoneCharm(BoneCharm newCharm, bool northTrack)
    {
        if (boardChains.IsBoardEmpty()) { return true; }
        eCharmType[] types = newCharm.GetTypes();
        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] == GetNorthType() && northTrack) { return true; }
            if (types[i] == GetSouthType() && !northTrack) { return true; }
        }
        return false;
    }
   

    public bool PlayBoneCharm(BoneCharm newCharm, BaseHand originHand, bool northTrack, ulong clientID)
    {
        if(TurnManager.instance)
            TurnManager.instance.HideTurnToken();


        if (boardChains.IsBoardEmpty())
        {
            originHand.RemoveCharmFromHand(newCharm);
            //First Play
            newCharm.transform.position = ConvertToConsistentBoardZ(transform.position);
            //newCharm.transform.SetParent(boardScalar);
            newCharm.transform.localRotation = Quaternion.identity;
            newCharm.transform.localScale = Vector3.one;
            
            //newCharm.ClearBoneCharmSelectedEvent();
            UpdateNorthCharm(newCharm);
            UpdateSouthCharm(newCharm);
            northTrackCount++;
            southTrackCount++;
            //PlayBoneCharmClientRpc(BoneCharmManager.GetNetDataFromCharm(newCharm), (int)originHand.playerID, true);

            TurnManager.instance.TickTurnIdx();
            //GameplayTransitions.instance.PassTurn();

            return true;
        }
        else
        {
            eCharmType[] types = newCharm.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == GetNorthType() && northTrack)
                {
                    originHand.RemoveCharmFromHand(newCharm);
                    PlaceNewNorthTile(newCharm, clientID);
                    //PlayBoneCharmClientRpc(BoneCharmManager.GetNetDataFromCharm(newCharm), (int)originHand.playerID, true);
                    return true;
                }
                if (types[i] == GetSouthType() && !northTrack)
                {
                    originHand.RemoveCharmFromHand(newCharm);
                    PlaceNewSouthTile(newCharm, clientID);
                    //PlayBoneCharmClientRpc(BoneCharmManager.GetNetDataFromCharm(newCharm), (int)originHand.playerID, false);
                    return true;
                }
            }

        }
        return false;
    }

    //True - North, Fale - South
    public bool IsVaidPlayPosition(Vector3 mousePos)
    {
        Vector3 cursorPosition = BoneCharmDragAndDrop.instance.GetCursorPositionOnBoard();
        if(IsBoardEmpty())
        {
            float middleDistance = Vector3.Distance(cursorPosition, transform.position);
            if (middleDistance >= maximumPlacementDistance) { return false; }
            return true;
        }
        float northDistance = Vector3.Distance(cursorPosition, northEndIcon.transform.position);
        float southDistance = Vector3.Distance(cursorPosition, southEndIcon.transform.position);

        if (southDistance >= maximumPlacementDistance && northDistance >= maximumPlacementDistance) { return false; }
        return true;
    }

    //True - North, Fale - South
    public bool GetClosestEnd(Vector3 mousePos)
    {
        float northDistance = Vector3.Distance(mousePos, northEndIcon.transform.position);
        float southDistance = Vector3.Distance(mousePos, southEndIcon.transform.position);

        if(northDistance <= southDistance)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool LookTarget(BoneCharm dragTarget, out Vector3 lookTarget)
    {
        lookTarget = Vector3.zero;
        return false;

        if (IsBoardEmpty())
        {
            lookTarget = Vector3.zero;
            return false;
        }
        else
        {
            lookTarget = Vector3.zero;
            float northDistance = Vector3.Distance(dragTarget.transform.position, northEndIcon.transform.position);
            float southDistance = Vector3.Distance(dragTarget.transform.position, southEndIcon.transform.position);
            
            if (southDistance >= maximumPlacementDistance && northDistance >= maximumPlacementDistance) { return false; }

            if(northDistance < southDistance)
            {
                if(!dragTarget.IsMatching(northCharm, GetNorthType()))
                    lookTarget = dragTarget.transform.position - northEndIcon.transform.position;
                else
                    lookTarget = northEndIcon.transform.position - dragTarget.transform.position;

                lookTarget.z = 0;
                return true;
            }
            else
            {
                if(!dragTarget.IsMatching(southCharm, GetSouthType()))
                    lookTarget = dragTarget.transform.position - southEndIcon.transform.position;
                else
                    lookTarget = southEndIcon.transform.position - dragTarget.transform.position;

                lookTarget.z = 0;
                return true;
            }
        }
    }

    void UpdateNorthCharm(BoneCharm newCharm, bool fromRemoval = false)
    {
        if (newCharm == null) { Debug.LogError("NewCharm Null"); return; }
        //if (northCharm != null)
        //{
        //    northCharm.SetNextCharm(newCharm);
        //    //if (northChain.Count <= 1)
        //    //    northCharm.SetPreviousCharm(newCharm);
        //    //else
        //    //    northCharm.SetNextCharm(northCharm);
        //}
        //if (newCharm != null)
        //    newCharm.SetPreviousCharm(northCharm);

        northCharm = newCharm;
        northCharm.SetRevealedState(true);
        //if(!northChain.Contains(northCharm))
          //  northChain.Add(northCharm);

        boardChains.AddToBoard(northCharm, true);

        northEndIcon.transform.position = ConvertToConsistentBoardZ(northCharm.GetAttachPosition(northTrackDirection, northCharm));
        if (IsServer)
        {
            if (northType.Value == eCharmType.eSizeOfCharms)
            {
                northType.Value = northCharm.topCharmType.Value;
            }
            else if (fromRemoval)
            {
                northType.Value = northCharm.GetClosestPhysicalType(northEndIcon.gameObject);
            }
            else
            {
                northType.Value = northCharm.GetNotType(northType.Value);
            }
            //northType.Value = northCharm.GetClosestPhysicalType(northEndIcon.gameObject);//northCharm.topCharmType; //Sometimes Bot Type
        }

        northEndIcon.sprite = BoneCharmManager.instance.GetBoneCharmSprite(GetNorthType());
        northEndIcon.color = BoneCharmManager.instance.GetBoneCharmColor(GetNorthType());

        UpdateBounds();
    }

    public void PlaceNewNorthTile(BoneCharm newCharm, ulong clientID)
    {
        

        //newCharm.transform.SetParent(boardScalar);
        newCharm.transform.localRotation = Quaternion.identity;
        newCharm.transform.localScale = Vector3.one;
        Vector3 newPos = northCharm.GetAttachPosition(northTrackDirection, newCharm);
        newPos = ConvertToConsistentBoardZ(newPos);
        newCharm.transform.position = ConvertToConsistentBoardZ(newPos);

        newCharm.ClearBoneCharmSelectedEvent();
        newCharm.SetOrientation(northTrackDirection);

        if(!newCharm.IsMatching(northCharm, GetNorthType()))
        {
            newCharm.RotateToMatch(false);
        }

        //if (!IsServer) { return; }
        //Wrap some of this into a UpdateEndType/Icon Func
        eCharmType resolveType = northType.Value;
        BoneCharm previousCharm = northCharm;
        UpdateNorthCharm(newCharm);

        northTrackCount++;
        UpdateNorthChainDirection();


        //GameplayTransitions.instance.PlayBoneCharmOnBoard(northCharm, previousCharm, newPos, resolveType, true);
        BoneCharmManager.instance.ResolveBoneCharmEffect(resolveType, true, clientID);
        //TurnManager.instance.TickTurnIdx();

    }

    void UpdateNorthChainDirection()
    {
        if ((northTrackDirection == eDirection.eNorth || northTrackDirection == eDirection.eSouth) && northTrackCount % maxVerticalCount == 0)
        {
            SetNorthDirection(eDirection.eWest);
            northTrackCount = 0;
        }
        else if ((northTrackDirection == eDirection.eEast || northTrackDirection == eDirection.eWest) && northTrackCount % maxHorizontalCount == 0)
        {
            SetNorthDirection(northVerticalSwitch ? eDirection.eNorth : eDirection.eSouth);
            northTrackCount = 0;
            northVerticalSwitch = !northVerticalSwitch;
        }
        SetNorthDirection(GetDirection(true, boardChains.GetNorthCount() - 1));
    }

    public BoneCharm RemoveNorthTrack()
    {
        //BoneCharm retval = northCharm;
        //BoneCharm newCharm = northCharm.GetPreviousCharm();
        //if(newCharm != null)
        //    newCharm.RemoveEndCharm(retval);
        

        //retval = null;
        //newCharm = null;

        BoneCharm retval = boardChains.GetNorthEnd(true);
        BoneCharm newCharm = boardChains.GetNorthEnd(false);

        
        //northTrackCount--;
        //northTrackDirection = prevNorthDir;
        UpdateNorthChainDirection();


        northCharm = null;
        UpdateNorthCharm(newCharm, true);
       // retval.SetNextCharm(null);
        //retval.SetPreviousCharm(null);

        return retval;
    }

    public void UpdateNorthIcon(eCharmType prevCharm, eCharmType nextCharm)
    {
        northEndIcon.sprite = BoneCharmManager.instance.GetBoneCharmSprite(nextCharm);
        northEndIcon.color = BoneCharmManager.instance.GetBoneCharmColor(nextCharm);
    }

    public void UpdateSouthIcon(eCharmType prevCharm, eCharmType nextCharm)
    {
        southEndIcon.sprite = BoneCharmManager.instance.GetBoneCharmSprite(nextCharm);
        southEndIcon.color = BoneCharmManager.instance.GetBoneCharmColor(nextCharm);
    }

    public void UpdateEndIcons()
    {
        if(northCharm != null)
        {
            northEndIcon.transform.position = ConvertToConsistentBoardZ(northCharm.GetAttachPosition(northTrackDirection, northCharm));
            northEndIcon.sprite = BoneCharmManager.instance.GetBoneCharmSprite(GetNorthType());
            northEndIcon.color = BoneCharmManager.instance.GetBoneCharmColor(GetNorthType());
        }

        if(southCharm != null)
        {
            southEndIcon.transform.position = ConvertToConsistentBoardZ(southCharm.GetAttachPosition(southTrackDirection, southCharm));
            southEndIcon.sprite = BoneCharmManager.instance.GetBoneCharmSprite(GetSouthType());
            southEndIcon.color = BoneCharmManager.instance.GetBoneCharmColor(GetSouthType());
        }
    }

    void UpdateSouthCharm(BoneCharm newCharm, bool fromRemoval = false)
    {
        if(newCharm == null) { Debug.LogError("NewCharm Null"); return; }
        //if (southCharm != null)
        //{
        //    if (southCharm.GetPreviousCharm() == null)
        //        southCharm.SetPreviousCharm(newCharm);
            
        //    //else
        //    southCharm.SetNextCharm(newCharm);
        //}
        //if(newCharm != null)
        //    newCharm.SetPreviousCharm(southCharm);

        southCharm = newCharm;
        southCharm.SetRevealedState(true);

        boardChains.AddToBoard(southCharm, false);

        southEndIcon.transform.position = ConvertToConsistentBoardZ(southCharm.GetAttachPosition(southTrackDirection, southCharm));
        if (IsServer)
        {
            if(southType.Value == eCharmType.eSizeOfCharms)//First Play
            {
                southType.Value = southCharm.botCharmType.Value;
            }
            else if (fromRemoval)
            {
                southType.Value = southCharm.GetClosestPhysicalType(southEndIcon.gameObject);
            }
            else //Connection Plays
            {
                southType.Value = southCharm.GetNotType(southType.Value);
            }
            //southType.Value = southCharm.GetClosestPhysicalType(southEndIcon.gameObject);//southChain.Count <= 1 ? southCharm.botCharmType : southCharm.topCharmType; // Sometimes Bot Type
        }

        southEndIcon.sprite = BoneCharmManager.instance.GetBoneCharmSprite(GetSouthType());
        southEndIcon.color = BoneCharmManager.instance.GetBoneCharmColor(GetSouthType());

        UpdateBounds();
    }
    
    public void PlaceNewSouthTile(BoneCharm newCharm, ulong clientID)
    {


        //newCharm.transform.SetParent(boardScalar);
        newCharm.transform.localScale = Vector3.one;
        newCharm.transform.localRotation = Quaternion.identity;
        Vector3 newPos = southCharm.GetAttachPosition(southTrackDirection, newCharm);
        newPos = ConvertToConsistentBoardZ(newPos);
        newCharm.transform.position = ConvertToConsistentBoardZ(newPos);

        newCharm.ClearBoneCharmSelectedEvent();
        newCharm.SetOrientation(southTrackDirection);


        if (!newCharm.IsMatching(southCharm, GetSouthType()))
        {
            newCharm.RotateToMatch(false);
        }

        //if (boardChains.GetSouthCount() <= 1) //The first awkward south placement, Need a better way to do this
        //{
        //    if (newCharm.botCharmType != southCharm.botCharmType)
        //    {
        //        newCharm.RotateToMatch(false);
        //    }
        //    awkwardPlacement = true;
        //}
        //else
        //{
        //    if (!newCharm.IsMatching(southCharm, GetSouthType()))
        //    {
        //        newCharm.RotateToMatch(false);
        //    }
        //}



        //Rotate to proper Position
        eCharmType resolveType = southType.Value;
        BoneCharm previousCharm = southCharm;
        UpdateSouthCharm(newCharm);

        southTrackCount++;
        UpdateSouthChainDirection();

        //GameplayTransitions.instance.PlayBoneCharmOnBoard(southCharm, previousCharm, newPos, resolveType, false);
        BoneCharmManager.instance.ResolveBoneCharmEffect(resolveType, false, clientID);
        //TurnManager.instance.TickTurnIdx();
    }

    eDirection GetDirection(bool north, int chainCount)
    {
        //True is north/south False is east/west
        int forcedBuffer = 1;
        if(chainCount < forcedBuffer)
        {
            return north ? eDirection.eNorth : eDirection.eSouth;
        }
        else
        {
            chainCount -= forcedBuffer;
        }

        List<bool> directions = new List<bool>();

        for (int i = 0; i < maxHorizontalCount; i++) { directions.Add(false); }
        for (int i = 0; i < maxVerticalCount; i++) { directions.Add(true); }

        int cycles = 0;
        while(chainCount >= directions.Count) { chainCount -= directions.Count; cycles++; }
        if(chainCount < directions.Count)
        {
            bool grabDir = directions[chainCount];
            if(grabDir == true)//north.south
            {
                if(!north)
                {
                    return cycles % 2 == 0 ? eDirection.eNorth : eDirection.eSouth;
                }
                else
                {
                    return cycles % 2 == 0 ? eDirection.eSouth : eDirection.eNorth;
                }
            }
            else
            {
                if(north)
                {
                    return eDirection.eWest;
                }
                else
                {
                    return eDirection.eEast;
                }
            }
        }
        Debug.LogError(string.Format("Bad Direction. ChainCount: {0}", chainCount));
        return eDirection.eSouth;
    }

    void UpdateSouthChainDirection()
    {
        //if ((southTrackDirection == eDirection.eNorth || southTrackDirection == eDirection.eSouth) && southTrackCount % maxVerticalCount == 0)
        //{
        //    SetSouthDirection(eDirection.eEast);
        //    southTrackCount = 0;
        //}
        //else if ((southTrackDirection == eDirection.eEast || southTrackDirection == eDirection.eWest) && southTrackCount % maxHorizontalCount == 0)
        //{
        //    SetSouthDirection(southVerticalSwitch ? eDirection.eSouth : eDirection.eNorth);
        //    southTrackCount = 0;
        //    southVerticalSwitch = !southVerticalSwitch;
        //}
        SetSouthDirection(GetDirection(false, boardChains.GetSouthCount() - 1));
    }

    public BoneCharm RemoveSouthTrack()
    {
        //BoneCharm retval = southCharm;
        //BoneCharm newCharm = southCharm.GetPreviousCharm();
        //if(newCharm == null) { Debug.LogError("White Charm Broke"); }
        //if(newCharm != null)
        //    newCharm.RemoveEndCharm(retval);

        BoneCharm retval = boardChains.GetSouthEnd(true);
        BoneCharm newCharm = boardChains.GetSouthEnd(false);
        
        //southTrackCount--;
        //southTrackDirection = prevSouthDir;
        UpdateSouthChainDirection();

        southCharm = null;
        UpdateSouthCharm(newCharm, true);
        //retval.SetNextCharm(null);
        //retval.SetPreviousCharm(null);
        
        return retval;
    }

    //BoneCharm GetClosestCharm(BoneCharm charm)
    //{
    //    BoneCharm retval = null;
    //    if(northChain.Contains(charm) || southChain.Contains(charm))
    //    {
    //        float minDist = float.MaxValue;
    //        for(int i = 0; i < northChain.Count; i++)
    //        {
    //            float dist = Vector3.Distance(charm.transform.position, northChain[i].transform.position);
    //            if(dist < minDist)
    //            {
    //                minDist = dist;
    //                retval = northChain[i];
    //            }
    //        }
    //        for (int i = 0; i < southChain.Count; i++)
    //        {
    //            float dist = Vector3.Distance(charm.transform.position, southChain[i].transform.position);
    //            if (dist < minDist)
    //            {
    //                minDist = dist;
    //                retval = southChain[i];
    //            }
    //        }
    //    }
    //    return retval;
    //}

    void UpdateBounds()
    {
        charmsBounds = new Bounds(transform.position, Vector3.zero);
        //charmsBounds.Encapsulate(northEndIcon.transform.position);
        //charmsBounds.Encapsulate(southEndIcon.transform.position);

        for (int i = 0; i < boardChains.board.Count; i++)
        {
            charmsBounds.Encapsulate(boardChains.board[i].transform.position);
        }
        //for(int i = 0; i < southChain.Count; i++)
        //{
        //    charmsBounds.Encapsulate(southChain[i].transform.position);
        //}

        boardExtentTopLeft.transform.position = charmsBounds.center + (charmsBounds.extents);
        boardExtentBotRight.transform.position = charmsBounds.center - (charmsBounds.extents);
        Vector3 minScale = Vector3.one * minBoardScale;
        int currOnBoard = boardChains.board.Count;//northChain.Count + southChain.Count;
        float percent = currOnBoard / (float)BoneCharmManager.instance.GetTotalCharmsInSet();
        boardScalar.localScale = Vector3.Lerp(Vector3.one, minScale, percent);
        //boardScalar.transform.position = transform.position - (Vector3.right * (charmsBounds.extents.x / 2));
    }

   

    public eCharmType GetNorthType()
    {
        if(northCharm != null)
        {
            return northType.Value;
            //return northCharm.topCharmType;
            //if(northTrackDirection == eDirection.eSouth)
            //{
            //    return northCharm.botCharmType;
            //}
        }
        return eCharmType.eSizeOfCharms;
    }

    public eCharmType GetSouthType()
    {
        if(southCharm != null)
        {
            return southType.Value;
            //return southCharm.botCharmType;
            //if(southTrackDirection == eDirection.eNorth)
            //{
            //    return southCharm.topCharmType;
            //}
        }
        return eCharmType.eSizeOfCharms;
    }

    Vector3 ConvertToConsistentBoardZ(Vector3 vector)
    {
        vector.z = boardScalar.position.z;
        return vector;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position, Vector3.one);

        if(prefab != null)
        {
            Vector3 size = Vector3.zero;
            size.z = 1;
            size.x = maxHorizontalCount * prefab.GetHeight();
            size.y = maxVerticalCount * prefab.GetHeight();
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, size);
        }

        Gizmos.DrawWireSphere(northEndIcon.transform.position, maximumPlacementDistance);
        Gizmos.DrawWireSphere(southEndIcon.transform.position, maximumPlacementDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, boardExtents);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(charmsBounds.center, charmsBounds.extents * 2);

        //Gizmos.DrawWireSphere(northEndPoint, 0.25f);
        //Gizmos.DrawWireSphere(southEndPoint, 0.25f);
    }
}
