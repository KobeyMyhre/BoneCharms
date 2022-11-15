using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneCharmDragAndDrop : MonoBehaviour
{
    public static BoneCharmDragAndDrop instance;

    public BoneCharm currentSelected;
    public BaseHand dragHandReturn;
    public LayerMask dragLayer;
    Camera cam;

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
    private void Start()
    {
        cam = Camera.main;
    }

    public void SetDragTarget(BoneCharm boneCharm, BaseHand hand)
    {
        if(currentSelected == null)
        {
            currentSelected = boneCharm;
            dragHandReturn = hand;
        }
    }

    public Vector3 GetCursorPositionOnBoard()
    {
        RaycastHit hit;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, dragLayer))
        {
            return hit.point;
        }
        return Input.mousePosition;
    }

    private void Update()
    {
        if(currentSelected != null && Input.GetMouseButton(0))
        {
            //Point the BoneCharms matching side towards the closest North/South end of the board
            //Do nothing if its the first play
            
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, dragLayer))
            {
                currentSelected.transform.position = hit.point;
                Vector3 lookTarget = Vector3.zero;
                if(BoardCenter.instance.LookTarget(currentSelected, out lookTarget))
                {
                    currentSelected.transform.up = lookTarget;
                }
                else
                {
                    currentSelected.transform.rotation = Quaternion.identity;
                }
            }
        }
        else
        {
            bool madePlay = false;
            if(currentSelected != null && BoardCenter.instance.IsVaidPlayPosition(Input.mousePosition))
            {
                int playType = BoardCenter.instance.CanPlayBoneCharm(currentSelected);
                if (playType != -1)
                {
                    currentSelected.transform.rotation = Quaternion.identity;
                    currentSelected.PlayPlayableEffect(false);
                    currentSelected.DisplayRevealedIcon(false);
                    dragHandReturn.RemoveCharmFromHand(currentSelected);
                    dragHandReturn.ClearPlayables();
                    madePlay = true;
                    Vector3 cursorPosition = GetCursorPositionOnBoard();
                    bool trackChosen = BoardCenter.instance.GetClosestEnd(cursorPosition);
                    BoardCenter.instance.PlayBoneCharmServerRpc(BoneCharmManager.GetNetDataFromCharm(currentSelected), (int)dragHandReturn.playerID, trackChosen);
                }
                //if (BoardCenter.instance.PlayBoneCharm(currentSelected, dragHandReturn))
                //{
                //    dragHandReturn.ClearPlayables();
                //    dragHandReturn.myHand.Remove(currentSelected);
                //    madePlay = true;
                //}
            }
            ClearDragTarget(!madePlay);
        }
    }

    public void ClearDragTarget(bool placeValidHands)
    {
        if(currentSelected != null)
        {
            //Send back to ActivePlayers hand
            dragHandReturn.PlaceHandPositions(placeValidHands);
            dragHandReturn = null;
            currentSelected = null;
        }
    }
}
