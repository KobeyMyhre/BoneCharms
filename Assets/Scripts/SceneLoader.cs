using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System;

public enum eScenes
{
    StartUp,
    MainMenu,
    Lobby,
    BoneCharmsMultiplayer

}

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader instance;

    //public MainMenuCleanUp mainMenuCleanUp;
    public GameObject transitionHolder;
    public float loadingMinTime = 1.0f;
    public TextMeshProUGUI loadingText;
    public Image transitionTarget;
    public float transitionDuration;
    public AnimationCurve transitionCurve;

    bool isAnimating = false;

    bool hasTriggeredOnEnter;
    public OnEvent onTransitionEnterFinished;
    bool hasTriggeredOnExit;
    public OnEvent onTransitionExitFinished;
    public eScenes currScene;

    

    private void Awake()
    {
        if(instance == null)
        {
            loadingText.text = "";
            instance = this;
            DontDestroyOnLoad(gameObject);
            if (currScene != eScenes.StartUp)
            {
                
            }
        }else { Destroy(this); instance.Unhide(); }
    }

    private void Start()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton.SceneManager != null);

        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
    }

    public void AddOnEnterEvent(OnEvent newEvent)
    {
        if (!hasTriggeredOnEnter)
        {
            onTransitionEnterFinished += newEvent;
        }
        else
        {
            newEvent.Invoke();
        }
    }


    

    public void Unhide()
    {
        StartCoroutine(UnhideTransition());
    }

    IEnumerator UnhideTransition()
    {
        isAnimating = true;
        loadingText.gameObject.SetActive(false);
        hasTriggeredOnEnter = false;
        //Color startColor = new Color(0, 0, 0, 1);
        //Color endColor = new Color(0, 0, 0, 0);
        //transitionTarget.color = startColor;
        transitionTarget.fillAmount = 0;
        transitionHolder.SetActive(true);
        transitionTarget.gameObject.SetActive(true);
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / transitionDuration;
            transitionTarget.fillAmount = transitionCurve.Evaluate(1 - t);
            //transitionTarget.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        isAnimating = false;
        transitionHolder.SetActive(false);
        transitionTarget.gameObject.SetActive(false);
        onTransitionEnterFinished?.Invoke();
        hasTriggeredOnEnter = true;
    }

    IEnumerator LoadSceneWithTransition(string sceneName)
    {
        isAnimating = true;
        hasTriggeredOnExit = false;
        //Color startColor = new Color(0, 0, 0, 0);
        //Color endColor = new Color(0, 0, 0, 1);
        //transitionTarget.color = startColor;
        transitionTarget.fillAmount = 1.0f;
        transitionHolder.SetActive(true);
        transitionTarget.gameObject.SetActive(true);

       

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        float t = 0;
        while(t < 1)
        {
            t += Time.deltaTime / transitionDuration;
            transitionTarget.fillAmount = transitionCurve.Evaluate(t);
            //transitionTarget.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        onTransitionExitFinished?.Invoke();
        hasTriggeredOnExit = true;
        //SceneManager.LoadScene(sceneName);


        //loadingText.gameObject.SetActive(true);
        t = 0;
        while (!asyncLoad.isDone)
        {
            float asyncProgress = asyncLoad.progress;
            t += Time.deltaTime;
            if(t > loadingMinTime)
            {
                if(!loadingText.gameObject.activeInHierarchy)
                {
                    loadingText.gameObject.SetActive(true);
                }
                int loadPercent = Mathf.RoundToInt(asyncProgress * 100);
                loadingText.text = string.Format("{0}%", loadPercent);
            }
            
            
            if (asyncProgress >= 0.9f)
            {
                //loadingText.gameObject.SetActive(false);
                asyncLoad.allowSceneActivation = true;
            }
            yield return null;
        }
        isAnimating = false;
        //loadingText.gameObject.SetActive(false);
    }
    

    public void LoadSceneNetworked(eScenes scenesToLoad)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;
        currScene = scenesToLoad;
        NetworkManager.Singleton.SceneManager.LoadScene(scenesToLoad.ToString(), LoadSceneMode.Single);
    }

    private void OnLoadComplete(ulong clientID, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;


        //Enum.TryParse(sceneName, out currScene);

        if (!ClientConnection.instance.CanClientConnection(clientID))
            return;

        switch(currScene)
        {
            case eScenes.Lobby:
                LobbyHandler.instance.ServerSceneInit(clientID);
                break;
            case eScenes.BoneCharmsMultiplayer:
                TurnManager.instance.ServerSceneInit(clientID);
                break;
        }
    }

    public void LoadMainMenuScene()
    {
        if (isAnimating) { return; }
        StartCoroutine(LoadSceneWithTransition("MainMenu"));
    }

    public void LoadGameplayScene()
    {
        if (isAnimating) { return; }
        StartCoroutine(LoadSceneWithTransition("BoneCharms3DDemo"));
    }

    private void OnDestroy()
    {
        if(NetworkManager.Singleton)
        {
            if(NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
            }
        }
    }
}
