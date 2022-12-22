using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerResultsUI : MonoBehaviour
{

    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;

    public void SetNameScore(string name, int score)
    {
        nameText.text = name;
        scoreText.text = string.Format("Bones {0}", score.ToString());
    }
}
