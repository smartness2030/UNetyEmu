using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{

    public float seconds;
    public float timer;
    public float speed;

    public bool useTimer;

    public Image TimerImage;

    public Text TimerText;
    
    public GameObject CountdownImage;
    public GameObject CountdownTimerBkg;

    // Update is called once per frame
    void Update()
    {

        if (useTimer)
        {

            if (timer <= 0)
            {
                CountdownImage.SetActive(false);
                CountdownTimerBkg.SetActive(false);
                return;
            }

            TimerImage.fillAmount = timer / seconds;
            timer -= Time.deltaTime * speed;
            TimerText.text = timer.ToString("0");
        
        }

    }
    
}
