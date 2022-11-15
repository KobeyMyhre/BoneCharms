using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCreationHandler : MonoBehaviour
{
    [System.Serializable]
    public class PlayerBoardComponents
    {
        public BaseHand baseHand;
        public PlayerUI playerUI;
    }

    public List<PlayerBoardComponents> playerBoardComponents;

    public List<BC_Player> CreateListOfPlayers(List<BC_Player> players)
    {
        for(int i = 0; i < players.Count; i++)
        {
            if(i < playerBoardComponents.Count)
            {
                players[i].playerHand = playerBoardComponents[i].baseHand;
                players[i].playerUI = playerBoardComponents[i].playerUI;
            }
        }
        return players;
    }
}
