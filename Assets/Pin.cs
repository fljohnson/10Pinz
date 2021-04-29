using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pin : MonoBehaviour
{
	
	
	bool finished=false; //has this pin finished moving since the ball hit the backstop?
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       if(Central.state == State.COUNTING && !finished) {
		   if(GetComponent<Rigidbody>().velocity == Vector3.zero) {
			   Central.UpCount();
			   finished=true;
		   }
	   } 
    }
    
    void OnCollisionEnter() {
		if(Central.state == State.STARTED) {
			GetComponent<AudioSource>().Play();
		}
	}
	
	public void NewRoll() {
		finished=false;
	}
}
