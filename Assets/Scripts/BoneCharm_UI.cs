using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class BoneCharm_UI : MonoBehaviour
{
    public Image topIcon;
    public Image botIcon;

    public void InitBoneCharmUI(BoneCharm charm)
    {
        topIcon.sprite = BoneCharmManager.instance.GetBoneCharmSprite(charm.topCharmType.Value);
        topIcon.color = BoneCharmManager.instance.GetBoneCharmColor(charm.topCharmType.Value);

        botIcon.sprite = BoneCharmManager.instance.GetBoneCharmSprite(charm.botCharmType.Value);
        botIcon.color = BoneCharmManager.instance.GetBoneCharmColor(charm.botCharmType.Value);
    }
}
