using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Pin : MonoBehaviour
{
	public GameObject apex;
	
	bool finished=false; //has this pin finished moving since the ball hit the backstop?
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       if(Central.state == State.COUNTING && !finished) {
		   if(GetComponent<Rigidbody>().velocity.magnitude <= .0025f) {
			   Central.UpCount();
			   finished=true;
			   return;
		   }
		   if(apex.transform.position.y < 0.6f) { //it's knocked over
			   Central.UpCount();
			   finished=true;
			   return;
		   }
		   Assert.IsFalse(transform.position.y<-1f,"BOOM");
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
