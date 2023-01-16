using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class BaseHand : NetworkBehaviour
{
    public BoneCharm prefab;
    public BoardCenter.eDirection charmDirection;
    public ulong playerID;
    //public float xSpacing;
    //public float ySpacing;
    //public bool right = true;
    public TextMeshPro nameText;
    public TextMeshPro scoreText;
    public GameObject drawHint;
    //public Transform handPosition;
    public BoundedTransformHolder handPositionHandler;
    public Transform turnTokenPosition;
    public List<BoneCharm> myHand;
    public OnEventBaseHand onHandUpdate;
    bool isAssigned = false;

    private void Start()
    {
        ShowDrawHint(false);
    }

    public void ShowDrawHint(bool val)
    {
        drawHint.SetActive(val);
    }

    public void SetNameText(string text)
    {
        nameText.text = text;
    }

    public void SetScoreText(int score)
    {
        scoreText.text = score.ToString();
    }

    public void InitHand(List<BoneCharm> charms)
    {
        myHand = charms;
        for(int i = 0; i < myHand.Count; i++)
        {
            // myHand[i].transform.SetParent(handPositionHandler.transform);
            //myHand[i].UpdateBoneCharmSelectedEvent(PlayCharmFromHand);
            myHand[i].SetOrientation(charmDirection);
            myHand[i].SetOwnerHand(this);
            myHand[i].UpdateLocation(eLocation.ePlayerHand, (int)playerID);
            if (OverrideReveal())
            {
                myHand[i].SetFlipOverride();
            }
        }
        PlaceHandPositions();
        //InitHandClientRpc(BoneCharmManager.GetCharmNetDataList(charms));
    }

   

    public void OverrideCharmsInHandSelectEvent(OnEventBoneCharm eventBoneCharm)
    {
        for (int i = 0; i < myHand.Count; i++)
        {
            myHand[i].UpdateBoneCharmSelectedEvent(eventBoneCharm);
        }
    }

    public void ClearCharmsSelected()
    {
        for (int i = 0; i < myHand.Count; i++)
        {
            myHand[i].UpdateBoneCharmSelectedEvent(null);
        }
    }

    public virtual void UpdateCharmsInHandToPlayOnSelect()
    {
       
    }

    protected virtual bool OverrideReveal()
    {
        return false;
    }


    public void AddBoneToHand_FromBoneyard(BoneCharm charm)
    {
        AddBoneToHand(charm);
        if(!HasValidPlay() && BoneCharmManager.instance.boneYard.IsEmpty()) //No Valid Moves
        {
            //TurnManager.instance.TickTurnIdx();
            GameplayTransitions.instance.PassTurn();
        }
    }

    //public void AddBoneToHand_ServerRequest(BoneCharm charm)
    //{

    //    AddBoneToHandServerRpc(charm.GetCharmNetData(), playerID);
    //}

    //[ServerRpc]
    //public void AddBoneToHandServerRpc(BoneCharmNetData charmData, ulong handClientID)
    //{
    //    Debug.Log("Requesting BoneYard Draw Server Rpc");
    //    AddBoneToHandClientRpc(charmData, handClientID);
    //    //BoneCharm charm = BoneCharmManager.instance.GetCharmFromNetData(charmData);
    //    //if(charm != null)
    //    //{
    //    //    AddBoneToHand_FromBoneyard(charm);
    //    //}
    //}

    //[ClientRpc]
    //public void AddBoneToHandClientRpc(BoneCharmNetData charmData, ulong handClientID)
    //{
    //    //if (IsServer) { return; }
    //    Debug.Log("BoneYard Draw Client Rpc");

    //    BoneCharm charm = BoneCharmManager.instance.GetCharmFromNetData(charmData);
    //    if(charm != null)
    //    {
    //        AddBoneToHand_FromBoneyard(charm);
    //    }
    //}

    public bool GetIsAssigned()
    {
        return isAssigned;
    }

    public void SetIsAssigned(bool val)
    {
        isAssigned = val;
    }

    public virtual void AddBoneToHand(BoneCharm charm)
    {
        if (!myHand.Contains(charm) && !TurnManager.instance.IsCharmInOtherPlayerHand(charm))
        {
            myHand.Add(charm);
            //charm.transform.SetParent(handPositionHandler.transform);
            charm.SetOrientation(charmDirection);
            //charm.UpdateBoneCharmSelectedEvent(PlayCharmFromHand);
            charm.UpdateLocation(eLocation.ePlayerHand, (int)playerID);
            charm.SetOwnerHand(this);
            if (OverrideReveal())
            {
                charm.SetFlipOverride();
            }
            if (BoardCenter.instance.IsCharmValidOnBoard(charm))
            {
                ShowDrawHint(false);
                //BoneCharmManager.instance.boneYard.DisplayBoneYardDrawHint(false);
            }
            PlaceHandPositions();
            onHandUpdate?.Invoke(this);
        }
    }

    public virtual bool StartTurn()
    {
        PlaceHandPositions();
        TurnManager.instance.SetTurnToken(turnTokenPosition);
        if(!HasValidPlay())
        {
            ShowDrawHint(true);
        }
        else if (BoneCharmManager.instance.boneYard.IsEmpty())
        {
            GameplayTransitions.instance.PassTurn();
            //TurnManager.instance.TickTurnIdx();
            Debug.Log("PASS!NO PLAY!");
            return false;
        }
        return true;
    }

    public virtual void DraftAction()
    {
        BoneCharmManager.instance.boneYard.SetBoneYardDraftActionEvent();
        TurnManager.instance.SetTurnToken(turnTokenPosition);
    }

    public virtual void EndTurn()
    {
        ShowDrawHint(false);
    }

    public bool HasValidPlay()
    {
        for (int i = 0; i < myHand.Count; i++)
        {
            if(BoardCenter.instance.IsCharmValidOnBoard(myHand[i]))
            {
                return true;
            }
        }
        return false;
    }

    public BoneCharm RevealRandomCharm()
    {
        for(int i = 0; i < myHand.Count; i++)
        {
            if(!myHand[i].IsRevealed())
            {
                BoneCharm retval = myHand[i];
                myHand[i].SetRevealedState(true);
                return retval;
            }
        }
        return null;
    }


    public bool IsEmpty()
    {
        return myHand.Count == 0;
    }

    public BoneCharm GetRandomCharm()
    {
        if(myHand.Count == 0) { return null; }
        int r = Random.Range(0, myHand.Count);
        BoneCharm retval = myHand[r];
        myHand.RemoveAt(r);
        return retval;
    }

    public void RemoveCharmFromHand(BoneCharm charm)
    {
        charm.ClearBoneCharmSelectedEvent();
        charm.SetOwnerHand(null);
        myHand.Remove(charm);
        onHandUpdate?.Invoke(this);
    }

    public void PlaceHandPositions()
    {
        foreach(BoneCharm charm in myHand)
        {
            charm.SetOrientation(charmDirection);
        }
        handPositionHandler.FitBoneCharmsInBounds(myHand);
    }

    public bool ContainsCharmType(eCharmType charmType)
    {
        for(int i =0; i < myHand.Count; i++)
        {
            eCharmType[] types = myHand[i].GetTypes();
            if(BoneCharmManager.DoesCharmTypesMatch(charmType, types))
            {
                return true;
            }
        }
        return false;
    }

    //private void OnDrawGizmosSelected()
    //{
    //    if(prefab != null)
    //    {
    //        for(int i = 0; i < 20; i++)
    //        {
    //            Vector3 dir = right ? transform.right : -transform.right;
    //            Vector3 yOffset = -transform.up * (((int)(i / 5.0f)) * prefab.GetHeight() * ySpacing);
    //            int x = i % 5;
    //            Vector3 pos = handPosition.position + (dir * x * prefab.GetWidth() * xSpacing) + yOffset;
    //            Gizmos.DrawWireCube(pos, new Vector3(prefab.GetWidth(), prefab.GetHeight(), 1));
    //        }
    //    }
    //}

    protected void PlayCharmFromHand(BoneCharm charm)
    {
        BoneCharmDragAndDrop.instance.SetDragTarget(charm, this);
        //This is usually wrong on the client?
        if (TurnManager.instance.IsItMyTurn(this))
        {
            
        }
        //myHand.Remove(charm);
        //BoardCenter.instance.PlayBoneCharm(charm);
    }

    public int GetNonRevealedCharms()
    {
        int retval = 0;

        for(int i = 0; i < myHand.Count; i++)
        {
            if(!myHand[i].IsRevealed())
            {
                retval++;
            }
        }

        return retval;
    }

    public List<BoneCharm> GetRevealedCharms()
    {
        List<BoneCharm> retval = new List<BoneCharm>();

        for(int i = 0; i < myHand.Count; i++)
        {
            if(myHand[i].IsRevealed())
            {
                retval.Add(myHand[i]);
            }
        }

        return retval;
    }

    protected void HighlightPlayables()
    {
        for (int i = 0; i < myHand.Count; i++)
        {
            myHand[i].PlayPlayableEffect(BoardCenter.instance.IsCharmValidOnBoard(myHand[i]));
        }
    }

    public void ClearPlayables()
    {
        for (int i = 0; i < myHand.Count; i++)
        {
            myHand[i].PlayPlayableEffect(false);
        }
    }
}
