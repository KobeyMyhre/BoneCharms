using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;

public class PlayerLobbyDisplay : MonoBehaviour
{

    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI playerReadyText;
    public Image playerColor;
    int myColorIdx = 0;
    Color myColor;

    public GameObject colorSelection;

    public void InitPlayerDisplay(string name, bool isOwner)
    {
        gameObject.SetActive(true);
        playerNameText.text = name;
        playerColor.color = LobbyHandler.instance.GetColor(myColorIdx);
        
        colorSelection.SetActive(isOwner);
        DisplayReadyUp(false);
    }

    public void DisplayReadyUp(bool val)
    {
        playerReadyText.color = val ? Color.green : Color.red;
    }

    public void UpdateColor(int val)
    {
        myColorIdx += val;
        if(myColorIdx >= LobbyHandler.instance.GetTotalColors())
        {
            myColorIdx = 0;
        }
        if(myColorIdx < 0)
        {
            myColorIdx = LobbyHandler.instance.GetTotalColors() - 1;
        }
        myColor = LobbyHandler.instance.GetColor(myColorIdx);
        playerColor.color = myColor;
    }
}
