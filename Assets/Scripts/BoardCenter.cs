using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public class BoardContainer
{
    public List<BoneCharm> board = new List<BoneCharm>();
    public BoneCharm centerPiece = null;
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
            if(retval.IsBoneCharmEqual(centerPiece))
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
            if(retval.IsBoneCharmEqual(centerPiece))
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
            if(board[i].IsBoneCharmEqual(centerPiece))
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
            if (board[i].IsBoneCharmEqual(centerPiece))
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
                playedCharm.UpdateLocation(eLocation.eBoard);
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
            Vector3 oldPos = newCharm.transform.position;
            newCharm.UpdateBoneCharmPosition(ConvertToConsistentBoardZ(transform.position));
            //newCharm.transform.SetParent(boardScalar);
            newCharm.SetOrientation(eDirection.eNorth);
            newCharm.transform.localScale = Vector3.one;
            GameplayTransitions.instance.PlayBoneCharmOnBoard(newCharm, null, oldPos, eCharmType.eSizeOfCharms, true);
            
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
                    return true;
                }
                if (types[i] == GetSouthType() && !northTrack)
                {
                    originHand.RemoveCharmFromHand(newCharm);
                    PlaceNewSouthTile(newCharm, clientID);
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

    void UpdateNorthCharm(BoneCharm newCharm, bool fromRemoval = false, bool wasDouble = false)
    {
        if (newCharm == null) { Debug.LogError("NewCharm Null"); return; }
        BoneCharm prevCharm = northCharm;
        northCharm = newCharm;
        northCharm.SetRevealedState(true);
        northCharm.LockCharmPosition(true);
        //if(!northChain.Contains(northCharm))
          //  northChain.Add(northCharm);

        boardChains.AddToBoard(northCharm, true);

        northEndIcon.transform.position = ConvertToConsistentBoardZ(northCharm.GetAttachPosition(true, northTrackDirection));
        if (IsServer)
        {
            //northType.Value = northCharm.GetNorthType();
            if (northType.Value == eCharmType.eSizeOfCharms)
            {
                northType.Value = northCharm.topCharmType.Value;
            }
            else
            {
                eCharmType newType = northCharm.GetOutwardType(true, fromRemoval ? prevNorthDir : northTrackDirection);
                northType.Value = newType;
            }
            //else if (fromRemoval)
            //{
            //    if (!wasDouble)
            //    {
            //        northType.Value = prevCharm.GetNotType(northType.Value);
            //        //southType.Value = southCharm.GetNotType(southType.Value);
            //    }
            //    //northType.Value = northCharm.GetClosestPhysicalType(northEndIcon.gameObject);
            //}
            //else
            //{
            //    northType.Value = northCharm.GetNotType(northType.Value);
            //}
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
        Vector3 newPos = northCharm.GetAttachPosition(true, northTrackDirection);
        newPos = ConvertToConsistentBoardZ(newPos);
        Vector3 oldPos = newCharm.transform.position;
        newCharm.UpdateBoneCharmPosition(ConvertToConsistentBoardZ(newPos));

        eCharmType resolveType = northType.Value;
        GameplayTransitions.instance.PlayBoneCharmOnBoard(newCharm, northCharm, newPos, resolveType, true);

        newCharm.ClearBoneCharmSelectedEvent();
        newCharm.SetOrientation(northTrackDirection);

        if(!newCharm.IsMatching(northCharm, GetNorthType()))
        {
            newCharm.RotateToMatch(false);
        }

        //if (!IsServer) { return; }
        //Wrap some of this into a UpdateEndType/Icon Func
        
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
        //if ((northTrackDirection == eDirection.eNorth || northTrackDirection == eDirection.eSouth) && northTrackCount % maxVerticalCount == 0)
        //{
        //    SetNorthDirection(eDirection.eWest);
        //    northTrackCount = 0;
        //}
        //else if ((northTrackDirection == eDirection.eEast || northTrackDirection == eDirection.eWest) && northTrackCount % maxHorizontalCount == 0)
        //{
        //    SetNorthDirection(northVerticalSwitch ? eDirection.eNorth : eDirection.eSouth);
        //    northTrackCount = 0;
        //    northVerticalSwitch = !northVerticalSwitch;
        //}
        SetNorthDirection(GetDirection(true, boardChains.GetNorthCount() - 1));
    }

    public BoneCharm RemoveNorthTrack()
    {

        BoneCharm retval = boardChains.GetNorthEnd(true);
        retval.LockCharmPosition(false);
        BoneCharm newCharm = boardChains.GetNorthEnd(false);

        


        northCharm = null;
        UpdateNorthCharm(newCharm, true, retval.IsDouble());
        UpdateNorthChainDirection();

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
            northEndIcon.transform.position = ConvertToConsistentBoardZ(northCharm.GetAttachPosition(true, northTrackDirection));
            northEndIcon.sprite = BoneCharmManager.instance.GetBoneCharmSprite(GetNorthType());
            northEndIcon.color = BoneCharmManager.instance.GetBoneCharmColor(GetNorthType());
        }

        if(southCharm != null)
        {
            southEndIcon.transform.position = ConvertToConsistentBoardZ(southCharm.GetAttachPosition(false, southTrackDirection));
            southEndIcon.sprite = BoneCharmManager.instance.GetBoneCharmSprite(GetSouthType());
            southEndIcon.color = BoneCharmManager.instance.GetBoneCharmColor(GetSouthType());
        }
    }

    void UpdateSouthCharm(BoneCharm newCharm, bool fromRemoval = false, bool wasDouble = false)
    {
        if(newCharm == null) { Debug.LogError("NewCharm Null"); return; }

        BoneCharm prevCharm = northCharm;
        southCharm = newCharm;
        southCharm.SetRevealedState(true);
        southCharm.LockCharmPosition(true);

        boardChains.AddToBoard(southCharm, false);

        southEndIcon.transform.position = ConvertToConsistentBoardZ(southCharm.GetAttachPosition(false, southTrackDirection));
        if (IsServer)
        {
            //southType.Value = newCharm.GetSouthType();
            if (southType.Value == eCharmType.eSizeOfCharms)//First Play
            {
                southType.Value = southCharm.botCharmType.Value;
            }
            else
            {
                eCharmType newType = southCharm.GetOutwardType(false, fromRemoval ? prevSouthDir : southTrackDirection);
                southType.Value = newType;//southCharm.GetOutwardType(false, prevSouthDir);
            }
            //else if (fromRemoval)
            //{
            //    if (!wasDouble)
            //    {
            //        southType.Value = prevCharm.GetNotType(southType.Value);
            //        //southType.Value = southCharm.GetNotType(southType.Value);
            //    }
            //    //southType.Value = southCharm.GetClosestPhysicalType(southEndIcon.gameObject);
            //}
            //else //Connection Plays
            //{
            //    southType.Value = southCharm.GetNotType(southType.Value);
            //}
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
        Vector3 newPos = southCharm.GetAttachPosition(false, southTrackDirection);
        newPos = ConvertToConsistentBoardZ(newPos);
        Vector3 oldPos = newCharm.transform.position;
        newCharm.UpdateBoneCharmPosition(ConvertToConsistentBoardZ(newPos));

        eCharmType resolveType = southType.Value;
        GameplayTransitions.instance.PlayBoneCharmOnBoard(newCharm, southCharm, oldPos, resolveType, false);
        newCharm.ClearBoneCharmSelectedEvent();
        newCharm.SetOrientation(southTrackDirection);


        if (!newCharm.IsMatching(southCharm, GetSouthType()))
        {
            newCharm.RotateToMatch(false);
        }

        



        //Rotate to proper Position
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
        SetSouthDirection(GetDirection(false, boardChains.GetSouthCount() - 1));
    }

    public BoneCharm RemoveSouthTrack()
    {

        BoneCharm retval = boardChains.GetSouthEnd(true);
        retval.LockCharmPosition(false);
        BoneCharm newCharm = boardChains.GetSouthEnd(false);
        

        southCharm = null;
        UpdateSouthCharm(newCharm, true, retval.IsDouble());
        UpdateSouthChainDirection();
        
        return retval;
    }

   

    void UpdateBounds()
    {
        charmsBounds = new Bounds(transform.position, Vector3.zero);

        for (int i = 0; i < boardChains.board.Count; i++)
        {
            charmsBounds.Encapsulate(boardChains.board[i].transform.position);
        }

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
        }
        return eCharmType.eSizeOfCharms;
    }

    public eCharmType GetSouthType()
    {
        if(southCharm != null)
        {
            return southType.Value;
        }
        return eCharmType.eSizeOfCharms;
    }

    public Vector3 ConvertToConsistentBoardZ(Vector3 vector)
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
