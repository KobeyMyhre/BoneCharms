using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TokenMover : MonoBehaviour
{
    public float duration;
    float t = 0;
    Vector3 targetPos;
    Vector3 startPos;

    private void Start()
    {
        t = 0;
    }

    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }

    public void MoveToNewTarget(Vector3 newPos)
    {
        startPos = transform.position;
        targetPos = newPos;
        t = 0;
    }

    private void Update()
    {
        if(t < 1)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            t += Time.deltaTime / duration;
        }
    }
}
