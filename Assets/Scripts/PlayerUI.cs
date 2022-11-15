using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI handSizeText;

    public RectTransform boneCharmHolder;
    List<BoneCharm_UI> boneCharmUIs;

    BaseHand myHand;

    public OnEventBaseHand onSelected;

    public void SetUpPlayerUI(BaseHand hand)
    {
        playerNameText.text = hand.name;
        myHand = hand;
        boneCharmUIs = new List<BoneCharm_UI>();
        UpdateHandDisplay(hand);
        hand.onHandUpdate += UpdateHandDisplay;
    }

    public void UpdateOnSelected(OnEventBaseHand eventBaseHand)
    {
        onSelected = eventBaseHand;
    }

    public void OnSelected()
    {
        onSelected?.Invoke(myHand);
    }

    void UpdateHandDisplay(BaseHand hand)
    {
        UpdateHandSizeText(hand.GetNonRevealedCharms());
        UpdateRevealedBoneCharms(hand.GetRevealedCharms());
    }

    void UpdateHandSizeText(int size)
    {
        handSizeText.text = size.ToString();
    }

    void UpdateRevealedBoneCharms(List<BoneCharm> revealedCharms)
    {
        ClearUIBoneCharms();

        for(int i = 0; i < revealedCharms.Count; i++)
        {
            BoneCharm_UI newUICharm = BoneCharmManager.instance.GetBoneCharmUI(boneCharmHolder);
            newUICharm.InitBoneCharmUI(revealedCharms[i]);
            boneCharmUIs.Add(newUICharm);
        }
    }

    void ClearUIBoneCharms()
    {
        for(int i = 0; i < boneCharmUIs.Count; i++)
        {
            Destroy(boneCharmUIs[i].gameObject);
        }
        boneCharmUIs = new List<BoneCharm_UI>();
    }

    public BaseHand GetMyHand()
    {
        return myHand;
    }
}
