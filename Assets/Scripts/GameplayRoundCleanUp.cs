using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class GameplayRoundCleanUp : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public float duration = 5;


    private void Update()
    {
        duration -= Time.deltaTime;
        timerText.text = Mathf.RoundToInt(duration).ToString();
        if(duration <= 0)
        {
            if(BCPlayersInGame.instance)
                BCPlayersInGame.instance.EndGameSession();
            
            NetworkManager.Singleton.Shutdown();
            
            SceneLoader.instance.LoadMainMenuScene();
        }
    }
}
