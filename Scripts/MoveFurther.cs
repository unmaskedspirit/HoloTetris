using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;


public class MoveFurther : MonoBehaviour, IInputClickHandler
{

    private float zIncrement = 0.2f;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnInputClicked(InputClickedEventData eventData)
    {
        float currentDistance = FindObjectOfType<GameControl>().GetGameZDistance();
        FindObjectOfType<GameControl>().SetGameZDistance(currentDistance + zIncrement);
        FindObjectOfType<GameControl>().CalibrateGame();

    }
}