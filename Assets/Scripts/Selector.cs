using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selector : MonoBehaviour
{
    public LayerMask clickLayer;
    Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out hit, Mathf.Infinity, clickLayer.value))
        {
            if(Input.GetMouseButtonDown(0))
            {
                BoneCharm charm = hit.collider.GetComponentInParent<BoneCharm>();
                if (charm != null)
                {
                    Debug.Log("Charm Selected");
                    charm.BoneCharmSelected();
                }
            }
            if(Input.GetMouseButton(1))
            {
                BoneCharm charm = hit.collider.GetComponentInParent<BoneCharm>();
                PreviewBoneCharmUI.instance.SetUI(charm, Input.mousePosition);
            }
            else
            {
                PreviewBoneCharmUI.instance.SetUI(null, Vector3.zero);
            }
        }
    }
}
