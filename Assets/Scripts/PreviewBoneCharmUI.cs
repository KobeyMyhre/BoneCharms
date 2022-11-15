using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewBoneCharmUI : MonoBehaviour
{
    public static PreviewBoneCharmUI instance;

    BoneCharm currentCharm;
    public BoneCharm_UI charmUI;
    public RectTransform charmUIRect;


    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            SetUI(null, Vector3.zero);
        }
        else
        {
            Destroy(this);
        }
    }

    public void SetUI(BoneCharm charm, Vector3 position)
    {
        currentCharm = charm;
        if(currentCharm != null)
        {
            charmUI.InitBoneCharmUI(currentCharm);
        }
        charmUI.gameObject.SetActive(currentCharm != null);
        charmUIRect.anchoredPosition = position;
    }
}
