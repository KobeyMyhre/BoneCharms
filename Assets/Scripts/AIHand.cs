using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIHand : BaseHand
{
    public class CharmAIWeight
    {
        public BoneCharm boneCharm;
        public int playabilityScore;

        public CharmAIWeight(BoneCharm bc, int ps)
        {
            boneCharm = bc;
            playabilityScore = ps;
        }
    }


    bool isThinking = false;
    public bool holdPriotity = false;
    public bool makeAIDecision = false;
    public override bool StartTurn()
    {
        if(base.StartTurn())
        {
            if (!isThinking && makeAIDecision)
            {
                StartCoroutine(TakeAITurn());
            }
            return true;
        }
        return false;
    }

    public override void DraftAction()
    {
        base.DraftAction();
        BoneCharmManager.instance.boneYard.ClearBoneYardSelectedEvents();
        if (!isThinking)
        {
            StartCoroutine(DraftThink());
        }
    }

    IEnumerator DraftThink()
    {
        isThinking = true;
        yield return new WaitForSeconds(0.75f);
        isThinking = false;
        BoneCharmManager.instance.boneYard.BoneCharmSelected_BoneYard(BoneCharmManager.instance.boneYard.GetRandomHiddenCharm(false));
    }


    //Redo AI to decide a BoneCharm to play and which track to put it on
    //Probably bypass PlayBoneCharm(), and just do direct to north/south
    //Calculate the plan for the turn, then do the delay
    IEnumerator TakeAITurn()
    {
        isThinking = true;
        bool madePlay = false;
        while(holdPriotity)
        {
            if(Input.GetKey(KeyCode.P))
            {
                break;
            }
            yield return null;
        }
        yield return new WaitForSeconds(1);
        if(BoardCenter.instance.IsBoardEmpty())
        {
            int r = Random.Range(0, myHand.Count);
            BoardCenter.instance.PlayBoneCharm(myHand[r], this, true);
            madePlay = true;
        }
        else
        {
            eCharmType northType = BoardCenter.instance.GetNorthType();
            eCharmType southType = BoardCenter.instance.GetSouthType();

            List<BoneCharm> options = new List<BoneCharm>();
            for (int i = 0; i < myHand.Count; i++)
            {
                eCharmType[] charmTypes = myHand[i].GetTypes();
                foreach (eCharmType charm in charmTypes)
                {
                    if (charm == northType)
                    {
                        options.Add(myHand[i]);
                        break;
                    }
                    if (charm == southType)
                    {
                        options.Add(myHand[i]);
                        break;
                    }
                }
            }
            yield return null;
            if (options.Count > 0)
            {
                //TODO 
                //Weight the options, make a function that given handsizes, the 2 ends, and then look at both side of the
                //charm and give it a point value.
                int r = Random.Range(0, options.Count);
                Debug.Log("AI Played: " + options[r].gameObject.name);
                if (!BoardCenter.instance.PlayBoneCharm(options[r], this, true))
                {
                    Debug.Log("AI SAD: It Didnt Get Played");
                }
                madePlay = true;
            }
            else
            {
                //Need to hit BoneYard
                options.Clear();
                List<BoneCharm> boneyardRevealed = BoneCharmManager.instance.boneYard.GetRevealedCharms();
                for (int i = 0; i < boneyardRevealed.Count; i++)
                {
                    eCharmType[] charmTypes = boneyardRevealed[i].GetTypes();
                    foreach (eCharmType charm in charmTypes)
                    {
                        if (charm == northType)
                        {
                            options.Add(boneyardRevealed[i]);
                            break;
                        }
                        if (charm == southType)
                        {
                            options.Add(boneyardRevealed[i]);
                            break;
                        }
                    }
                }
                yield return null;
                if (options.Count > 0)
                {
                    //TODO 
                    //Weight the options, make a function that given handsizes, the 2 ends, and then look at both side of the
                    //charm and give it a point value.
                    int r = Random.Range(0, options.Count);
                    Debug.Log("AI Added from BoneYard: " + options[r].gameObject.name);
                    AddBoneToHand_FromBoneyard(options[r]);
                }
                else
                {
                    BoneCharm drawnCharm = BoneCharmManager.instance.boneYard.GetRandomCharm_AI();
                    if (drawnCharm != null)
                    {
                        AddBoneToHand_FromBoneyard(drawnCharm);
                        Debug.Log("AI Added from BoneYard: -Hidden-");
                    }
                    else
                    {
                        //No Valid Plays, Pass
                        //TurnManager.instance.TickTurnIdx();
                        GameplayTransitions.instance.PassTurn();
                        madePlay = true;
                    }
                }
                if(!madePlay)
                    StartCoroutine(TakeAITurn());
            }
        }
        
        isThinking = false;
    }
}
