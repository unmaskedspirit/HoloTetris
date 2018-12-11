using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;


public class Credits : MonoBehaviour, IInputClickHandler
{

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnInputClicked(InputClickedEventData eventData)
    {
        float gameZDistance = FindObjectOfType<GameControl>().GetGameZDistance();
        var creditsSplash = (GameObject)Instantiate(Resources.Load("Prefabs/CreditsCanvas", typeof(GameObject)), new Vector3(-4.7f, -0.5f, (gameZDistance+3.5f)), Quaternion.identity);
    }
}
