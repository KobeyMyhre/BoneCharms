using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundedTransformHolder : MonoBehaviour
{
    public BoneCharm prefab;
    [Range(0,20)]
    public int boneCharmsInHand; //Max 20
    [Range(0,1)]
    public float minimumScale;
    //public float scaleFactor;
    Vector3 endScale;
    public Vector3 desiredBounds;
    public float xSpacing = 1.25f;
    public float ySpacing = 1.25f;
    public Vector2Int desiredColumnsRange;
    

    private void Start()
    {
        endScale = transform.localScale * minimumScale;
    }
    

    private void OnDrawGizmos()
    {
        if(prefab != null)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(transform.position, desiredBounds);

            float testScaleFactor = Mathf.Clamp01((boneCharmsInHand - desiredColumnsRange.x) / (20.0f - desiredColumnsRange.x));
            int desiredColumns = Mathf.RoundToInt(Mathf.Lerp(desiredColumnsRange.x, desiredColumnsRange.y, testScaleFactor));
            int charmsInColumn = Mathf.Min(boneCharmsInHand, desiredColumns);
            Gizmos.color = Color.white;
            Vector3 offset = new Vector3(-desiredBounds.x, desiredBounds.y, desiredBounds.z);
            if(boneCharmsInHand - 1 < desiredColumns)
            {
                offset.x += (desiredColumns - (charmsInColumn - 1)) * (prefab.GetWidth() + xSpacing);
            }
            Vector3 StartPos = transform.position + (offset / 2);
            StartPos -= transform.up * (prefab.GetHeight() / 2);
            StartPos += transform.right * (prefab.GetWidth() / 2);
            if(boneCharmsInHand <= desiredColumnsRange.x)
            {
                StartPos.y = transform.position.y;
            }
            Gizmos.DrawWireSphere(StartPos, 0.25f);

            
            //float width = desiredBounds.x;
            //int charmsPerRow = Mathf.RoundToInt(boneCharmsInHand / 2.0f);
            //charmsPerRow = Mathf.Max(desiredColumns, charmsPerRow);

            for(int i = 0; i < boneCharmsInHand; i++)
            {
                Vector3 targetScale = prefab.GetBounds().extents * 2;
                Vector3 minScale = targetScale * minimumScale;
                Vector3 usedScale = Vector3.Lerp(targetScale, minScale, testScaleFactor);


                Vector3 targetPos = StartPos;

                int x = i % desiredColumns;
                int y = (int)(i / (float)desiredColumns);

                targetPos += transform.right * ((xSpacing * x)+ (usedScale.x * x));
                targetPos += -transform.up * ((ySpacing * y) + (usedScale.y * y));

               
                Gizmos.DrawWireCube(targetPos, usedScale);
            }
        }
    }

    public void FitBoneCharmsInBounds(List<BoneCharm> boneCharms)
    {

        float scaleFactor = Mathf.Clamp01((boneCharms.Count - desiredColumnsRange.x) / (20.0f - desiredColumnsRange.x));
        int desiredColumns = Mathf.RoundToInt(Mathf.Lerp(desiredColumnsRange.x, desiredColumnsRange.y, scaleFactor));
        int charmsInColumn = Mathf.Min(boneCharms.Count, desiredColumns);
        Vector3 offset = new Vector3(-desiredBounds.x, desiredBounds.y, desiredBounds.z);
        if (boneCharms.Count - 1 < desiredColumns)
        {
            offset.x += (desiredColumns - (charmsInColumn - 1)) * (prefab.GetWidth() + xSpacing);
        }
        Vector3 StartPos = transform.position + (offset / 2);
        StartPos -= transform.up * (prefab.GetHeight() / 2);
        StartPos += transform.right * (prefab.GetWidth() / 2);
        if (boneCharms.Count <= desiredColumnsRange.x)
        {
            StartPos.y = transform.position.y;
        }

        Vector3 targetScale = prefab.GetBounds().extents * 2;
        Vector3 minScale = targetScale * minimumScale;
        Vector3 usedScale = Vector3.Lerp(targetScale, minScale, scaleFactor);
        for (int i = 0; i < boneCharms.Count; i++)
        {


            Vector3 targetPos = StartPos;

            int x = i % desiredColumns;
            int y = (int)(i / (float)desiredColumns);

            targetPos += transform.right * ((xSpacing * x) + (usedScale.x * x));
            targetPos += -transform.up * ((ySpacing * y) + (usedScale.y * y));


            boneCharms[i].transform.position = targetPos;
            boneCharms[i].transform.localScale = Vector3.one;
            boneCharms[i].transform.localRotation = Quaternion.identity;
        }
        transform.localScale = Vector3.Lerp(Vector3.one, endScale, scaleFactor);

    }
}
