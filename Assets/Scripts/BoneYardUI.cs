using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoneYardUI : MonoBehaviour
{
    public TextMeshProUGUI boneYardCountText;
    public RectTransform boneCharmHolder;
    List<BoneCharm_UI> boneCharmUIs = new List<BoneCharm_UI>();

    public void UpdateBoneYardUI(BoneYard boneYard)
    {
        boneYardCountText.text = boneYard.GetNonRevealedCharms().ToString();

        UpdateRevealedBoneCharms(boneYard.GetRevealedCharms());
    }

    void UpdateRevealedBoneCharms(List<BoneCharm> revealedCharms)
    {
        ClearUIBoneCharms();

        for (int i = 0; i < revealedCharms.Count; i++)
        {
            BoneCharm_UI newUICharm = BoneCharmManager.instance.GetBoneCharmUI(boneCharmHolder);
            newUICharm.InitBoneCharmUI(revealedCharms[i]);
            boneCharmUIs.Add(newUICharm);
        }
    }

    void ClearUIBoneCharms()
    {
        for (int i = 0; i < boneCharmUIs.Count; i++)
        {
            Destroy(boneCharmUIs[i].gameObject);
        }
        boneCharmUIs = new List<BoneCharm_UI>();
    }
}
