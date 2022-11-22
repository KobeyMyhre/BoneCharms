using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Unity.Netcode;

public class GameplayTransitions : MonoBehaviour
{
    public static GameplayTransitions instance;

    public float transitionDelay;
    public float attachCharmDelay;
    public AnimationCurve attachAnim;

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


    public void ResolveBoneCharm(BoneCharm charmA, BoneCharm charmB, eCharmType charmType, bool north)
    {
        StartCoroutine(ResolveBoneCharm_Anim(charmA, charmB, charmType, north));
    }

    IEnumerator ResolveBoneCharm_Anim(BoneCharm charmA, BoneCharm charmB, eCharmType charmType, bool north)
    {
        if (charmA != null)
        {
            charmA.PlayResolveEffect(charmType);
        }
        if (charmB != null)
        {
            charmB.PlayResolveEffect(charmType);
        }
        yield return new WaitForSeconds(transitionDelay);
        //BoneCharmManager.instance.ResolveBoneCharmEffect(charmA, charmB, charmType, north);
    }


    //Maybe this gets called after the charm is "Attached", this plays and then we ResolveBoneCharmEffects
    public void PlayBoneCharmOnBoard(BoneCharm newCharm, BoneCharm prevCharm, Vector3 attachedPosition, eCharmType charmType, bool north)
    {
        StartCoroutine(AttachBoneCharmToboard(newCharm, prevCharm, attachedPosition, charmType, north));
    }

    IEnumerator AttachBoneCharmToboard(BoneCharm newCharm, BoneCharm prevCharm, Vector3 attachedPosition, eCharmType charmType, bool north)
    {
        Vector3 startPos = newCharm.transform.position;
        float t = 0;
        while(t < 1)
        {
            t += Time.deltaTime / attachCharmDelay;
            newCharm.transform.position = Vector3.Lerp(startPos, attachedPosition, attachAnim.Evaluate(t));
            yield return null;
        }
        //Snap the BoneCharm Direction?
        //Lerp the BoneCharm to the Attach Point
        newCharm.transform.position = attachedPosition;
        BoardCenter.instance.UpdateEndIcons();
        yield return null;

        ResolveBoneCharm(newCharm, prevCharm, charmType, north);

        //Resolve BoneCharm Effects
    }


    public void PassTurn(bool blueCharm = false)
    {
        StartCoroutine(PassTurn_Anim(blueCharm));
    }

    IEnumerator PassTurn_Anim(bool blueCharm)
    {
        //Move the Turn Token during this anim
        yield return new WaitForSeconds(transitionDelay);


        TurnManager.instance.TickTurnIdx(blueCharm);
    }
}
