using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultsDisplay : MonoBehaviour
{
    public Image fillPanel;
    public TextMeshProUGUI resultsText;
    public float duration = 1.5f;

    private void Start()
    {
        fillPanel.enabled = false;
        resultsText.gameObject.SetActive(false);
    }

    public void ShowResults(bool winner)
    {
        fillPanel.gameObject.SetActive(true);
        fillPanel.enabled = true;
        resultsText.gameObject.SetActive(true);
        resultsText.text = winner ? "Winner" : "Loser";
        resultsText.color = winner ? Color.yellow : Color.red;
        StartCoroutine(AnimateResults());
    }

    IEnumerator AnimateResults()
    {
        float t = 0;
        while(t < 1)
        {
            t += Time.deltaTime / duration;
            fillPanel.fillAmount = Mathf.Lerp(0, 1, t);
            yield return null;
        }
        yield return new WaitForSeconds(1.0f);
        if (BCPlayersInGame.instance)
        {
            BCPlayersInGame.instance.ProgressToNextRound();
        }
        //if(SceneLoader.instance)
        //{
        //    SceneLoader.instance.LoadMainMenuScene();
        //}
    }
}
