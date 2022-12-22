using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MidRoundResultsDisplay : MonoBehaviour
{
    public TextMeshProUGUI roundTitleText;
    public List<PlayerResultsUI> playerResults;

    public void DisplayMidRoundResults(List<BCPlayerInLobby> bcPlayers, int currentRound)
    {
        roundTitleText.text = string.Format("Round {0} Results", currentRound + 1);
        for(int i = 0; i < playerResults.Count; i++)
        {
            if(i < bcPlayers.Count)
            {
                playerResults[i].SetNameScore(bcPlayers[i].GetPlayerName(), bcPlayers[i].roundPoints.Value);
            }
            else
            {
                playerResults[i].gameObject.SetActive(false);
            }
        }
    }

    public void DisplayMidRoundResults(PlayerRoundScore[] bcPlayers, int currentRound)
    {
        roundTitleText.text = string.Format("Round {0} Results", currentRound + 1);
        for (int i = 0; i < playerResults.Count; i++)
        {
            if (i < bcPlayers.Length)
            {
                string playerName = BCPlayersInGame.instance.GetPlayerName(bcPlayers[i].playerID);
                playerResults[i].SetNameScore(playerName, bcPlayers[i].currentScore);
            }
            else
            {
                playerResults[i].gameObject.SetActive(false);
            }
        }
    }
}
