using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class BobbingObject : MonoBehaviour 
{
	public float amplitude;          //Set in Inspector 
	public float speed;              //Set in Inspector 
	private Vector3 tempVal;
	private Vector3 tempPos;

	void Start () 
	{			
		tempVal = transform.position;
	}

	void Update ()
	{        
		tempPos = transform.position;
		tempPos.y = tempVal.y + amplitude * Mathf.Sin (speed * Time.time);
		transform.position = tempPos;
	}	
} 