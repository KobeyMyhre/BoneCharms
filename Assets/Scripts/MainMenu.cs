using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class MainMenu : MonoBehaviour
{
    
    public void PlayGame()
    {
        SceneLoader.instance.LoadGameplayScene();
    }

    public void HostGame()
    {
        if(NetworkManager.Singleton)
        {
            NetworkManager.Singleton.StartHost();
            SceneLoader.instance.LoadSceneNetworked(eScenes.Lobby);
        }
    }

    public void JoinGame()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.StartClient();
            //SceneLoader.instance.LoadSceneNetworked(eScenes.Lobby);
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
