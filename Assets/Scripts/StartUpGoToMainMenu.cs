using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartUpGoToMainMenu : MonoBehaviour
{
    public SceneLoader sceneloader;
    private void Start()
    {
        sceneloader.LoadMainMenuScene();
        //SceneManager.LoadScene("MainMenu");
    }
}
