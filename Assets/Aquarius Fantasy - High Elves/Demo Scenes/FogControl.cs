using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogControl : MonoBehaviour
{

    [Header("Underwater Fog")]
    public Color fogColorUnderwater;
    public float fogDensityUnderwater = 0.125f;
    public float transitionSpeedUnder = 2f;


    [Header("Environment Fog")]
    public Color fogColorEnvironment;
    public float fogDensityEnvironment = 0.015f;
    public float transitionSpeedAbove = 0.15f;

    private IEnumerator enterCoroutine;
    private IEnumerator exitCoroutine;

    private IEnumerator enterCoroutine2;
    private IEnumerator exitCoroutine2;


    void Awake ()
    {
        fogDensityEnvironment = RenderSettings.fogDensity;
    }

    void OnTriggerEnter(Collider other)
    {
        
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Enter!");

            if (exitCoroutine != null)

                StopCoroutine(exitCoroutine);
            enterCoroutine = (DoTheFade(fogDensityEnvironment, fogDensityUnderwater, transitionSpeedUnder, fogColorUnderwater));
            StartCoroutine(enterCoroutine);
            

            if (exitCoroutine2 != null)
             
                StopCoroutine(exitCoroutine2);
                enterCoroutine2 = (ChangeColor(fogColorEnvironment, fogColorUnderwater, transitionSpeedUnder));
                StartCoroutine(enterCoroutine2);
            

        }
            
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag ("Player"))
        {
            Debug.Log("Exit!");

            if (enterCoroutine != null)

                StopCoroutine(enterCoroutine);
            exitCoroutine = (DoTheFade(fogDensityUnderwater, fogDensityEnvironment, transitionSpeedAbove, fogColorEnvironment));
            StartCoroutine(exitCoroutine);

            if (enterCoroutine2 != null)
                StopCoroutine(enterCoroutine2);
            exitCoroutine2 = (ChangeColor(fogColorUnderwater, fogColorEnvironment, transitionSpeedAbove));
            StartCoroutine(exitCoroutine2);
        }
            
    }

    private IEnumerator DoTheFade(float current, float goal, float time, Color color)
    {
        float endTime = Time.time + time;
        var value = current;
        while (value != goal)
        {
            float t = 1f - (endTime - Time.time) / time;
            var newVal = Mathf.Lerp(current, goal, t);
            RenderSettings.fogDensity = newVal;
            RenderSettings.fogColor = color;
            yield return null;

        }
    }

    private IEnumerator ChangeColor(Color color1, Color color2, float time2)
    {
        float endTime = Time.time + time2;
        var value = color1;
        while (value != color2)
        {
            float t = 1f - (endTime - Time.time) / time2;
            var colorA = Color.Lerp(color1, color2, t);
            //RenderSettings.fogDensity = newVal2;
            RenderSettings.fogColor = colorA;
            yield return null;

        }
    }
}

