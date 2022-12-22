using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : BaseHand
{
    protected override bool OverrideReveal()
    {
        return true;
    }

    public override bool StartTurn()
    {
        if (base.StartTurn())
        {
            BoneCharmManager.instance.boneYard.UpdateCharmsInYardToDrawOnSelect();
            UpdateCharmsInHandToPlayOnSelect();
            HighlightPlayables();
            return true;
        }
        return false;
    }

    public override void DraftAction()
    {
        base.DraftAction();
        BoneCharmManager.instance.boneYard.SetBoneYardDraftActionEvent();
    }

    public override void AddBoneToHand(BoneCharm charm)
    {
        base.AddBoneToHand(charm);
        charm.UpdateBoneCharmSelectedEvent(PlayCharmFromHand);
        if (TurnManager.instance.IsItMyTurn(this))
        {
            HighlightPlayables();
        }
    }

    public override void UpdateCharmsInHandToPlayOnSelect()
    {
        for (int i = 0; i < myHand.Count; i++)
        {
            myHand[i].UpdateBoneCharmSelectedEvent(PlayCharmFromHand);
        }
    }

    public override void EndTurn()
    {
        base.EndTurn();
        BoneCharmManager.instance.boneYard.ClearBoneYardSelectedEvents();
        ClearCharmsSelected();
        ClearPlayables();
    }

    
}
