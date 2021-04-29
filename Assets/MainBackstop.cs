using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainBackstop : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void OnTriggerEnter(Collider col) {
		if(col.gameObject.name != "BowlingBall") {
			return;
		}
		Central.EndOfRoll();
	}
	
	void OnCollisionEnter(Collision wham) {
		if(wham.transform.name != "BowlingBall") {
			return;
		}
		Central.EndOfRoll();
	}
		
}
