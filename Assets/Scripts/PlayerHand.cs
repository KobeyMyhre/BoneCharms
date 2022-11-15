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
        if (TurnManager.instance.IsItMyTurn(this))
        {
            HighlightPlayables();
        }
    }

    public override void EndTUrn()
    {
        base.EndTUrn();
        BoneCharmManager.instance.boneYard.ClearBoneYardSelectedEvents();
        ClearPlayables();
    }

    
}
