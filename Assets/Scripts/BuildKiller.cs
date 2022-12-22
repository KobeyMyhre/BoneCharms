using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildKiller : MonoBehaviour
{
    float timerDuration = 120;
    float timer = 0;
    // Start is called before the first frame update
    void Start()
    {
        timer = 0;
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.anyKey)
        {
            timer = 0;
        }
        else
        {
            timer += Time.deltaTime;
            if(timer >= timerDuration)
            {
                Application.Quit();
            }
        }
    }
}
