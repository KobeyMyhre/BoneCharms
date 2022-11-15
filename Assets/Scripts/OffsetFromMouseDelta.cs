using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OffsetFromMouseDelta : MonoBehaviour
{
    public float distance = 5.0f;
    Vector3 startPos;
    Vector3 lastMousePos;
    private void Start()
    {
        startPos = transform.position;
    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButton(0))
        {
            Vector3 dir = Input.mousePosition - lastMousePos;
            transform.position = startPos + (dir.normalized * distance);
        }
        else
        {
            lastMousePos = Input.mousePosition;
            transform.position = startPos;
        }
    }
}
