using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchScreenPositionTarget : MonoBehaviour
{
    public LayerMask boardLayer;
    public Transform screenPositionTarget;
    Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        Ray ray = cam.ScreenPointToRay(screenPositionTarget.position);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, boardLayer))
        {
            Vector3 targetPos = hit.point + -ray.direction.normalized;
            targetPos.z = transform.position.z;
            transform.position = targetPos;
        }
    }
}
