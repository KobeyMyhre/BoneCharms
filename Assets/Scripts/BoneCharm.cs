using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;


public enum eCharmType
{
    eBlankCharm,
    ePinkCharm,
    eYellowCharm,
    eGreenCharm,
    eWhiteCharm,
    eBlueCharm,
    ePurpleCharm,
    eSizeOfCharms
}

public enum eOrientation
{
    eStandard,
    eFlipped,
    eSideways,
    eSidewaysFlipped
}

public enum eLocation
{
    eBoneYard,
    ePlayerHand,
    eBoard
}

[System.Serializable]
public class LocationData
{
    public eLocation location;
    public ulong playerID;
}

//maybe something like this to figure out the non connected end?
[System.Serializable]
public class BoneCharmCharmTypeEnd
{
    public eCharmType charmType;
    public SpriteRenderer spriteRenderer;
    public bool isConnected = false;
}

public enum eAttached
{
    topIsAttached,
    botIsAttached,
    noAttach
}
//

public class BoneCharm : NetworkBehaviour
{
    BaseHand myHand;
    //public LocationData location;
    public GameObject meshObject;
    public Transform rotator;
    bool isSwapped = false;
    bool isLocked = false;
    public SpriteRenderer backdropRender;
    public MeshRenderer charmMesh;
    public Sprite revealedBackdrop;
    public Sprite hiddenBackdrop;
    public eOrientation orientation;
    public eAttached attachedType;
    //Network This
    public NetworkVariable<eCharmType> topCharmType = new NetworkVariable<eCharmType>();
    public SpriteRenderer topSprite;
    
    //Network This
    public NetworkVariable<eCharmType> botCharmType = new NetworkVariable<eCharmType>();
    public SpriteRenderer botSprite;

    public NetworkVariable<eLocation> charmLocation = new NetworkVariable<eLocation>();
    public NetworkVariable<int> playerID = new NetworkVariable<int>();

    public GameObject attachHolder;
    public Transform north_Attach;
    public Transform east_AttachN;
    public Transform east_AttachS;
    public Transform west_AttachN;
    public Transform west_AttachS;
    public Transform south_Attach;

    public ParticleSystem targettedAoE;
    public ParticleSystem resolvedEffect;
    public ParticleSystem playableEffect;

    //Network This
    NetworkVariable<bool> isRevealed = new NetworkVariable<bool>();
    public GameObject isRevealedIcon;

    public BoneCharm previousCharm = null;
    public BoneCharm nextCharm = null;

    OnEventBoneCharm onBoneCharmSelected;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => BoneCharmManager.instance != null);
        if (IsClient && !IsServer)
        {
            InitBoneCharm(topCharmType.Value, botCharmType.Value);
            SetRevealedDisplay(isRevealed.Value);
            BoneCharmManager.instance.AddNetworkSpawnToNewSet(this);
        }
    }

    public void InitBoneCharm(eCharmType topCharm, eCharmType botCharm)
    {
        BoneCharmManager bcManager = BoneCharmManager.instance;
        if(bcManager)
        {
            gameObject.name = string.Format("{0} // {1}", topCharm.ToString(), botCharm.ToString());

            if(IsServer)
                topCharmType.Value = topCharm;

            topSprite.sprite = bcManager.GetBoneCharmSprite(topCharm);
            topSprite.color = bcManager.GetBoneCharmColor(topCharm);

            if(IsServer)
                botCharmType.Value = botCharm;

            botSprite.sprite = bcManager.GetBoneCharmSprite(botCharm);
            botSprite.color = bcManager.GetBoneCharmColor(botCharm);
            SetRevealedState(false);
        }
        orientation = eOrientation.eStandard;
        DisplayAttaches(false);

        topCharmType.OnValueChanged += OnTopCharmTypeChange;
        botCharmType.OnValueChanged += OnBotCharmTypeChange;
        isRevealed.OnValueChanged += SetRevealedDisplayNetwork;
    }

    private void OnTopCharmTypeChange(eCharmType prev, eCharmType curr)
    {
        //if(prev == curr) { return; }
        BoneCharmManager bcManager = BoneCharmManager.instance;
        topSprite.sprite = bcManager.GetBoneCharmSprite(curr);
        topSprite.color = bcManager.GetBoneCharmColor(curr);
    }


    private void OnBotCharmTypeChange(eCharmType prev, eCharmType curr)
    {
        //if(prev == curr) { return; }
        BoneCharmManager bcManager = BoneCharmManager.instance;
        botSprite.sprite = bcManager.GetBoneCharmSprite(curr);
        botSprite.color = bcManager.GetBoneCharmColor(curr);
    }

    public Bounds GetBounds()
    {
        return charmMesh.bounds;
    }

    public void DisplayAttaches(bool val)
    {
        attachHolder.SetActive(val);
    }

    public bool IsDouble()
    {
        return topCharmType.Value == botCharmType.Value;
    }

    

    public void SetOrientation(BoardCenter.eDirection newDir)
    {
        if (isLocked) { return; }
        myDirection = newDir;
        //Do Proper Quaternion Axis Rotations
        switch (newDir)
        {
            case BoardCenter.eDirection.eNorth:
                //Shift Position
                rotator.localPosition = Vector3.zero;
                rotator.localRotation = Quaternion.AngleAxis(0, Vector3.forward);
                break;
            case BoardCenter.eDirection.eSouth:
                //Shift Position
                rotator.localPosition += Vector3.down * (GetHeight() /2.0f);
                //rotator.localRotation = Quaternion.AngleAxis(180, Vector3.forward);
                break;
            case BoardCenter.eDirection.eWest:
                rotator.localPosition = Vector3.zero;
                rotator.localRotation = Quaternion.AngleAxis(90, Vector3.forward);
                break;
            case BoardCenter.eDirection.eEast:
                rotator.localRotation = Quaternion.AngleAxis(90, Vector3.forward);
                rotator.localPosition += Vector3.right * (GetWidth() / 2.0f);
                //rotator.localRotation = Quaternion.AngleAxis(-90, Vector3.forward);
                break;
        }
        //orientation = newDir;
    }

    //Dont Rotate it, just swap the sprites around
    public void RotateToMatch(bool vertical)
    {

        //If we just swap the display, maybe flag a bool to say it's backwards
        topSprite.sprite = BoneCharmManager.instance.GetBoneCharmSprite(botCharmType.Value);
        topSprite.color = BoneCharmManager.instance.GetBoneCharmColor(botCharmType.Value);

        botSprite.sprite = BoneCharmManager.instance.GetBoneCharmSprite(topCharmType.Value);
        botSprite.color = BoneCharmManager.instance.GetBoneCharmColor(topCharmType.Value);
        isSwapped = true;
    }

    public void RotateToDouble(bool vertical)
    {
        //SetOrientation(vertical ? eOrientation.eSideways : eOrientation.eSidewaysFlipped);
    }

    public void FlipRevealedState()
    {
        SetRevealedState(!isRevealed.Value);
    }

    public void SetFlipOverride()
    {
        botSprite.enabled = true;
        topSprite.enabled = true;
        backdropRender.sprite = revealedBackdrop;
        if (isRevealed.Value)
        {
            DisplayRevealedIcon(true);
        }
    }

    public void DisplayRevealedIcon(bool val)
    {
        isRevealedIcon.SetActive(val);
    }

    public void SetRevealedState()
    {
        SetRevealedState(isRevealed.Value);
    }

    void SetRevealedDisplayNetwork(bool prev, bool curr)
    {
        SetRevealedDisplay(curr);
    }

    void SetRevealedDisplay(bool revealed)
    {
        if (revealed)
        {
            botSprite.enabled = true;
            topSprite.enabled = true;

            //Only Turn on If it's our own charm
            //DisplayRevealedIcon(true);
        }
        else
        {
            botSprite.enabled = false;
            topSprite.enabled = false;

            DisplayRevealedIcon(false);
        }
    }

    public void SetRevealedState(bool revealed)
    {
        SetRevealedDisplay(revealed);
        if (IsServer)
        {
            isRevealed.Value = revealed;
        }
        backdropRender.sprite = revealed ? revealedBackdrop : hiddenBackdrop;
    }

    public bool IsRevealed()
    {
        return isRevealed.Value;
    }

    public float GetWidth()
    {
        return backdropRender.bounds.size.x;
    }

    public float GetHeight()
    {
        return backdropRender.bounds.size.y;
    }

    public void SetAttachedType(eCharmType charmType)
    {
        attachedType = eAttached.noAttach;
        if (topCharmType.Value == charmType) { attachedType = eAttached.topIsAttached; }
        if(botCharmType.Value == charmType) { attachedType = eAttached.botIsAttached; }
        
    }

    public eCharmType GetNonAttachedType()
    {
        switch (attachedType)
        {
            case eAttached.topIsAttached:
                return botCharmType.Value;
            case eAttached.botIsAttached:
                return topCharmType.Value;
            case eAttached.noAttach:
                return topCharmType.Value;
        }
        return eCharmType.eSizeOfCharms;
    }

    public eCharmType GetNorthType()
    {
        if (isSwapped) { return botCharmType.Value; }
        return topCharmType.Value;
    }

    public eCharmType GetSouthType()
    {
        if (isSwapped) { return topCharmType.Value; }
        return botCharmType.Value;
    }

    public eCharmType[] GetTypes()
    {
        eCharmType[] retval = new eCharmType[2];
        retval[0] = topCharmType.Value;
        retval[1] = botCharmType.Value;
        return retval;
    }

    public void SetOwnerHand(BaseHand hand)
    {
        myHand = hand;
    }

    public BaseHand GetOwnerHand()
    {
        return myHand;
    }

    public void UpdateBoneCharmSelectedEvent(OnEventBoneCharm selectEvent)
    {
        onBoneCharmSelected = selectEvent;
    }

    public void ClearBoneCharmSelectedEvent()
    {
        onBoneCharmSelected = null;
    }

    public void BoneCharmSelected()
    {
        onBoneCharmSelected?.Invoke(this);
    }

    List<Transform> GetAttaches(bool north, BoardCenter.eDirection direction)
    {
        List<Transform> retval = new List<Transform>();

        retval.Add(north_Attach);
        if (north)
        {
            if(myDirection == BoardCenter.eDirection.eSouth)
            {
                retval.Add(east_AttachS);
                retval.Add(west_AttachS);
            }
            else
            {
                retval.Add(east_AttachN);
                retval.Add(west_AttachN);
            }
        }
        else
        {
            if(myDirection == BoardCenter.eDirection.eNorth)
            {
                retval.Add(east_AttachN);
                retval.Add(west_AttachN);
            }
            else
            {
                retval.Add(east_AttachS);
                retval.Add(west_AttachS);
            }
        }
        retval.Add(south_Attach);
        return retval;
    }

    public SpriteRenderer GetCharmTypeRenderer(eCharmType charmType)
    {
        if(topCharmType.Value == charmType)
        {
            return topSprite;
        }
        if(botCharmType.Value == charmType)
        {
            return botSprite;
        }
        return null;
    }

    public eCharmType GetIsType(eCharmType charmType)
    {
        if (IsDouble()) { return topCharmType.Value; }
        if(topCharmType.Value == charmType) { return topCharmType.Value; }
        if (botCharmType.Value == charmType) { return botCharmType.Value; }
        return charmType;
    }

    public eCharmType GetNotType(eCharmType charmType)
    {
        if (IsDouble()) { return topCharmType.Value; }
        if(topCharmType.Value == charmType) { return botCharmType.Value; }
        if(botCharmType.Value == charmType) { return topCharmType.Value;}
        return charmType;
    }

    public eCharmType GetOutwardType(bool exception)
    {
        if (IsSpawned)
        {
            if (exception) { return topCharmType.Value; }
            return botCharmType.Value;
        }
        else
        {
            if (exception) { return botCharmType.Value; }
            return topCharmType.Value;
        }
    }

    public eCharmType GetOutwardType(bool north, BoardCenter.eDirection direction)
    {
        eCharmType retval = eCharmType.eSizeOfCharms;
        if(north)
        {
            if (direction == BoardCenter.eDirection.eSouth) { retval = isSwapped ? topCharmType.Value : botCharmType.Value; }
            else { retval = isSwapped ? botCharmType.Value : topCharmType.Value; }
        }
        else
        {
            if (direction == BoardCenter.eDirection.eNorth) { retval = isSwapped ? botCharmType.Value : topCharmType.Value; }
            else { retval = isSwapped ? topCharmType.Value : botCharmType.Value; }
        }
        return retval;
    }

    public bool IsMatching(BoneCharm attachTo, eCharmType charmType)
    {
        //Point the sprite renderers outwards in a direction
        //Then compare the FoV's of the sprites and see if they match?
        //Find the 2 Looking at Each Other, if they dont match swap them?
        eCharmType closestType = GetClosestPhysicalType(attachTo.GetCharmTypeRenderer(charmType).gameObject);
        if(closestType != charmType)
        {
            return false;
        }
        return true;

        //South needs to match north
        if (botCharmType != attachTo.topCharmType)
        {
            return false;
        }
        return true;


        SpriteRenderer typeRenderer = attachTo.GetCharmTypeRenderer(charmType);
        if(typeRenderer != null)
        {
            eCharmType attachToType = GetClosestPhysicalType(typeRenderer.gameObject);
            if(attachToType == charmType)
            {
                return true;
            }

        }

        return false;
    }

    public eCharmType GetClosestPhysicalType(GameObject target)
    {
        float dist1 = Vector3.Distance(topSprite.transform.position, target.transform.position);
        float dist2 = Vector3.Distance(botSprite.transform.position, target.transform.position);
        return dist1 >= dist2 ? botCharmType.Value : topCharmType.Value; 
    }

    public void LockCharmPosition(bool locked)
    {
        isLocked = locked;
    }
    
    public void UpdateBoneCharmPosition(Vector3 position)
    {
        if (isLocked) { return; }
        Vector3 newPos = position;
        if (BoardCenter.instance)
        {
            newPos = BoardCenter.instance.ConvertToConsistentBoardZ(newPos);
        }
        transform.position = newPos;
    }

    public BoardCenter.eDirection GetCharmDirection()
    {
        return myDirection;
    }

    BoardCenter.eDirection myDirection;
    public Vector3 GetAttachPosition(bool north, BoardCenter.eDirection direction)
    {
        Vector3 targetDir = Vector3.zero;
        switch (direction)
        {
            case BoardCenter.eDirection.eNorth:
                targetDir = Vector3.up;
                break;
            case BoardCenter.eDirection.eEast:
                targetDir = Vector3.right;
                break;
            case BoardCenter.eDirection.eSouth:
                targetDir = Vector3.down;
                break;
            case BoardCenter.eDirection.eWest:
                targetDir = Vector3.left;
                break;
        }

        List<Transform> attaches = GetAttaches(north, direction);
        for(int i = 0; i < attaches.Count; i++)
        {
            Vector3 dir = attaches[i].position - attachHolder.transform.position;
            float angle = Vector3.Angle(targetDir, dir);
            if(angle <= 45 && angle >= -45)
            {
                return attaches[i].position;
            }
        }


        return transform.position;



        //switch (direction)
        //{
        //    case BoardCenter.eDirection.eNorth:
        //        return north_Attach.position;// + transform.up * distance;
        //    case BoardCenter.eDirection.eEast:
        //        return east_Attach.position;// + transform.right * distance;
        //    case BoardCenter.eDirection.eSouth:
        //        return south_Attach.position;// + -transform.up * distance;
        //    case BoardCenter.eDirection.eWest:
        //        return west_Attach.position;// + -transform.right * distance;
        //}

        //float distance = GetHeight() / 4;
        //if (placedCharm.IsDouble()) { distance = 0; }
        //if(orientation == eOrientation.eStandard)
        //{
        //    switch (direction)
        //    {
        //        case BoardCenter.eDirection.eNorth:
        //            return north_Attach.position + transform.up * distance;
        //        case BoardCenter.eDirection.eEast:
        //            return east_Attach.position + transform.right * distance;
        //        case BoardCenter.eDirection.eSouth:
        //            return south_Attach.position + -transform.up * distance;
        //        case BoardCenter.eDirection.eWest:
        //            return west_Attach.position + -transform.right * distance;
        //    }
        //}
        //else if (orientation == eOrientation.eFlipped)
        //{
        //    switch (direction) // Backwards
        //    {
        //        case BoardCenter.eDirection.eSouth:
        //            return north_Attach.position + transform.up * distance;
        //        case BoardCenter.eDirection.eWest:
        //            return east_Attach.position + transform.right * distance;
        //        case BoardCenter.eDirection.eNorth:
        //            return south_Attach.position + -transform.up * distance;
        //        case BoardCenter.eDirection.eEast:
        //            return west_Attach.position + -transform.right * distance;
        //    }
        //}
        //else if(orientation == eOrientation.eSideways)
        //{
        //    switch (direction)
        //    {
        //        case BoardCenter.eDirection.eEast:
        //            return north_Attach.position + transform.up * (GetWidth() / 4);
        //        case BoardCenter.eDirection.eNorth:
        //            return east_Attach.position + transform.right * (GetWidth() / 4);
        //        case BoardCenter.eDirection.eWest:
        //            return south_Attach.position + -transform.up * (GetWidth() / 4);
        //        case BoardCenter.eDirection.eSouth:
        //            return west_Attach.position + -transform.right * (GetWidth() / 4);
        //    }
        //}
        //else if (orientation == eOrientation.eSidewaysFlipped)
        //{
        //    switch (direction)
        //    {
        //        case BoardCenter.eDirection.eWest:
        //            return north_Attach.position + transform.up * (GetWidth() / 4);
        //        case BoardCenter.eDirection.eSouth:
        //            return east_Attach.position + transform.right * (GetWidth() / 4);
        //        case BoardCenter.eDirection.eEast:
        //            return south_Attach.position + -transform.up * (GetWidth() / 4);
        //        case BoardCenter.eDirection.eNorth:
        //            return west_Attach.position + -transform.right * (GetWidth() / 4);
        //    }
        //}
        //return transform.position;
    }

    public void UpdateLocation(eLocation newLocation, int handID = -1)
    {
        if (IsServer)
        {
            charmLocation.Value = newLocation;
            playerID.Value = handID;
        }

        //switch (newLocation)
        //{
        //    case eLocation.eBoneYard:

        //        break;
        //    case eLocation.ePlayerHand:

        //        break;
        //    case eLocation.eBoard:

        //        break;
        //}

        //Make a thing to Update the Position of the BoneCharm based on Location
    }

    public void PlayTargettedAoE(eCharmType charmType)
    {
        ParticleSystem.MainModule settings = targettedAoE.main;
        settings.startColor = BoneCharmManager.instance.GetBoneCharmColor(charmType);
        targettedAoE.Play();
    }

    public void PlayPlayableEffect(bool val)
    {
        if(val)
        {
            playableEffect.Play();
        }
        else
        {
            playableEffect.Stop();
        }
    }

    public void PlayResolveEffect(eCharmType charmType)
    {
        if (isSwapped)
        {
            if (topCharmType.Value == charmType)
            {
                resolvedEffect.transform.position = botSprite.transform.position;
            }
            else
            {
                resolvedEffect.transform.position = topSprite.transform.position;
            }
        }
        else
        {
            if (topCharmType.Value == charmType)
            {
                resolvedEffect.transform.position = topSprite.transform.position;
            }
            else
            {
                resolvedEffect.transform.position = botSprite.transform.position;
            }
        }

        ParticleSystem.MainModule settings = resolvedEffect.main;
        settings.startColor = BoneCharmManager.instance.GetBoneCharmColor(charmType);
        resolvedEffect.Play();
    }

    public void SetPreviousCharm(BoneCharm charm)
    {
        previousCharm = charm;
    }
    public BoneCharm GetPreviousCharm()
    {
        return previousCharm;
    }
    public void SetNextCharm(BoneCharm charm)
    {
        nextCharm = charm;
    }
    public BoneCharm GetNextCharm()
    {
        return nextCharm;
    }

    public void RemoveEndCharm(BoneCharm charm)
    {
        if(charm == GetNextCharm())
        {
            SetNextCharm(null);
        }
        if(charm == GetPreviousCharm())
        {
            SetPreviousCharm(null);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (GetNextCharm())
        {
            Gizmos.color = BoneCharmManager.instance.GetBoneCharmColor(GetNextCharm().GetClosestPhysicalType(topSprite.gameObject));
            Vector3 offset = (Vector3.right * .25f) + (Vector3.forward * 0.25f);
            Gizmos.DrawLine(topSprite.transform.position + offset, GetNextCharm().botSprite.transform.position + offset);
        }
        if (GetPreviousCharm())
        {
            Gizmos.color = BoneCharmManager.instance.GetBoneCharmColor(GetPreviousCharm().GetClosestPhysicalType(botSprite.gameObject));
            Vector3 offset = (Vector3.right * -.25f) + (Vector3.forward * 0.25f);
            Gizmos.DrawLine(botSprite.transform.position + offset, GetPreviousCharm().topSprite.transform.position + offset);
        }
    }

    private void OnDrawGizmos()
    {
        
    }

    public BoneCharmNetData GetCharmNetData()
    {
        return new BoneCharmNetData(this);
    }

    public bool IsBoneCharmEqual(BoneCharm other)
    {
        if(topCharmType.Value == other.topCharmType.Value &&
            botCharmType.Value == other.botCharmType.Value)
        {
            return true;
        }
        return false;
    }
}
