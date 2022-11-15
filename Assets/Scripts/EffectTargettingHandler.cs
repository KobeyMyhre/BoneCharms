using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class EffectTargettingHandler : MonoBehaviour
{
    public TextMeshProUGUI selectPlayerInfoText;
    public GameObject selectPlayerUI;
    List<PlayerUI> playersToTarget;

    public GameObject selectBoneCharmUI;
    class HandSelectedBoneCharmSwap
    {
        BoneCharm charm;
        BaseHand hand;

        public HandSelectedBoneCharmSwap(BoneCharm bc, BaseHand bh)
        {
            charm = bc;
            hand = bh;
        }
    }


    List<BoneCharm> swapCharms;
    BoneYard swapYard = null;

    BoneCharm charmA;
    BoneCharm charmB;
    eCharmType charmType;
    bool north;



    private void Start()
    {
        selectPlayerUI.SetActive(false);
        selectBoneCharmUI.SetActive(false);
    }

    public void InitiateSelectBoneCharmUI(BoneCharm ca, BoneCharm cb, eCharmType ct, bool n)
    {
        swapCharms = new List<BoneCharm>();
        selectBoneCharmUI.SetActive(true);

        //Update all BoneCharms in Boneyard Selection Event
        BoneCharmManager.instance.boneYard.OverrideCharmsInYardSelectEvent(AddBoneCharmToSwap);
        //Update all BoneCharms in Hand Selection Event
        List<BaseHand> players = TurnManager.instance.GetPlayerHandsInGame();
        foreach (BaseHand hand in players)
        {
            hand.OverrideCharmsInHandSelectEvent(AddBoneCharmToSwap);
        }

        charmA = ca;
        charmB = cb;
        charmType = ct;
        north = n;
    }

    

    public void AddBoneCharmToSwap(BoneCharm charm)
    {
        if (!swapCharms.Contains(charm))
        {
            for(int i =0; i < swapCharms.Count; i++)
            {
                if(charm.GetOwnerHand() == swapCharms[i].GetOwnerHand())
                {
                    //Cant Target 2 in our own hand, and this also handles Boneyard?
                    return;
                }
            }
            swapCharms.Add(charm);
            charm.PlayPlayableEffect(true);
            if (swapCharms.Count == 2)
            {
                foreach(BoneCharm cm in swapCharms)
                {
                    cm.PlayPlayableEffect(false);
                }
                //Swap Them
                BoneCharmManager.instance.ResolveBoneCharmEffect(swapCharms[0], swapCharms[1], charmType, north, null);
                //Restore Previous Boneyard/Hand Selection Events
                BoneCharmManager.instance.boneYard.UpdateCharmsInYardToDrawOnSelect();
                List<BaseHand> players = TurnManager.instance.GetPlayerHandsInGame();
                foreach(BaseHand hand in players)
                {
                    hand.UpdateCharmsInHandToPlayOnSelect();
                }
                selectBoneCharmUI.SetActive(false);
            }
            else
            {
                charm.UpdateBoneCharmSelectedEvent(RemoveBoneCharmFromSwap);
            }
        }
    }

    public void RemoveBoneCharmFromSwap(BoneCharm charm)
    {
        swapCharms.Remove(charm);
        charm.UpdateBoneCharmSelectedEvent(AddBoneCharmToSwap);
    }

    public void InitiateSelectPlayerUI(BoneCharm ca, BoneCharm cb, eCharmType ct, bool n)
    {
        selectPlayerUI.SetActive(true);
        playersToTarget = TurnManager.instance.GetNonActivePlayerUI();
        foreach(PlayerUI player in playersToTarget)
        {
            player.UpdateOnSelected(ResolvePlayerSelection);
        }

        if(ct == eCharmType.eGreenCharm)
        {
            selectPlayerInfoText.text = "Select Target player To Reveal A BoneCharm In Their Hand.";
        }
        if(ct == eCharmType.ePurpleCharm)
        {
            selectPlayerInfoText.text = "Select Target Player To Draw A BoneCharm";
        }

        charmA = ca;
        charmB = cb;
        charmType = ct;
        north = n;
    }

    public void ResolvePlayerSelection(BaseHand target)
    {
        foreach(PlayerUI player in playersToTarget)
        {
            player.UpdateOnSelected(null);
        }
        selectPlayerUI.SetActive(false);
        BoneCharmManager.instance.ResolveBoneCharmEffect(charmA, charmB, charmType, north, target);
    }

}
