using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Assertions;

public enum State {
	PREROLL,
	PRIMED,
	LAUNCH,
	STARTED,
	COUNTING,
	RELOADING,
	GAMEOVER
}

public struct Frame {
	public int[] roll;
	public int score;
}	
public class Central : MonoBehaviour
{
	const int STRIKE = -2;
	const int SPARE = -1;
	//hints at the inner workings of an e-golf program, no?
	protected GameObject ball; 
	protected static float tMinus;
	protected bool launched = false;
	public static State state = State.PREROLL;
	public GameObject[] cameras;
	public float cameraTransition = 2f;
	public GameObject[] pins;
	public Dictionary<string,Vector3> pinsetter = new Dictionary<string,Vector3>();
	public int startingPins = 0;
	public int roll = 1;
	protected string pinTag = "Pin";
	public Text statusBar;
	public float moveRatio = 1f;
	public int maxFrames = 5;
	int currentFrame = 0;
	public Frame[] tally;
	private int bonusFrame = -1;
	private int bonusRolls = 0;
	public bool worldScoring = true;
	public Transform hudCanvas;
	public string[] rollMsg = {"Roll1","Roll2"};
	public Dictionary<string,GameObject> hud ;
		
	static int stoppedPins;
	
    // Start is called before the first frame update
    void Start()
    {
		tally = new Frame[maxFrames];
		for(int i=0;i<maxFrames;i++) {
			tally[i].roll = new int[2];
		}
		
		hud = new Dictionary<string,GameObject>() {
			{"Roll1",hudCanvas.Find("Roll1").gameObject},
			{"Roll2",hudCanvas.Find("Roll2").gameObject},
			{"Frame",hudCanvas.Find("Frame").gameObject},
			{"Score",hudCanvas.Find("Score").gameObject}
		};
        ball = GameObject.Find("BowlingBall");
        pins = GameObject.FindGameObjectsWithTag(pinTag);
        foreach(GameObject pin in pins) {
			pinsetter.Add(pin.name,pin.transform.position);
		}
        Reset();
    }

    // Update is called once per frame
    void Update()
    {
		switch(state) {
			case State.PREROLL:
				tMinus = 5f;
				state = State.PRIMED;
				break;
			case State.PRIMED:
				/*
				
				if(tMinus > 0f) {
					tMinus -= Time.deltaTime;
				}
				else {
					state = State.LAUNCH;
				}
				*/
				//TouchControl toca = touchscreen.primaryTouch;
				//if(toca.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended)
				//if(toca != null)
				
				break;
			case State.LAUNCH:
				Launch();
				tMinus = cameraTransition;
				state = State.STARTED;
				break;
			case State.STARTED:
				if(tMinus > 0f) {
					tMinus -=Time.deltaTime;
				}
				else {
					SwitchCamera(1);
				}
				break;
			case State.COUNTING:
				//wait until all pins stop moving, just in case of a wobbler (FLJ, 4/29/21)
				if(stoppedPins == startingPins) {
					Score();
					if(currentFrame < maxFrames)
					{
						tMinus = 2f;
						state = State.RELOADING;
					}
					else {
						PostGame();
					}
				}
				break;
			case State.RELOADING:
				if(tMinus > 0f) {
					tMinus -=Time.deltaTime;
				}
				else {
					Reset();
					state = State.PREROLL;
				}
				break;		
			case State.GAMEOVER:
				break;
				
		}
		
    }
    
    void Launch() {
        Rigidbody rock = ball.GetComponent<Rigidbody>();
		//if(rock == null)
		//	Application.Quit();
        //Assert.IsNotNull(rock,"blew it getting rigidbody");
        rock.AddForce(new Vector3(-4000f,0f,0)); //4000 is the max
        launched = true;
	}
	
	void Score() {
		int pinsLeft = 0;
		foreach(GameObject pin in pins) {
			if(!pin.activeInHierarchy) {
				continue;
			}
			//The direct approach introduced "359.75" where I was expecting "-0.25f", so 
			float biAngle = Quaternion.Angle( Quaternion.identity,pin.transform.rotation);
			if(Mathf.Abs(biAngle) < 7f) { //just under the square root of 25+25
				pinsLeft++;
			}
			else
			{
				pin.SetActive(false);
			}
		}
		if(pinsLeft == 0)
		{
			if(roll == 2) {
				UpdateScore(SPARE);
			}
			if(roll == 1) {
				UpdateScore(STRIKE);
			}
			startingPins = 0;
		}
		else {
			
			UpdateScore(startingPins - pinsLeft);
			if(roll == 1) {
				startingPins = pinsLeft;
			}
			else {
				startingPins = 0;
			}
		}
		if(startingPins == 0) { //we just finished a frame
			currentFrame += 1;
		}
	}
	
	
	void Reset() {
		stoppedPins=0;
		Rigidbody rock = ball.GetComponent<Rigidbody>();
		rock.velocity = Vector3.zero;
		rock.angularVelocity = Vector3.zero;
		ball.transform.eulerAngles = new Vector3(0f, 74.29501f,0f);
		if(startingPins == 0)
			ball.transform.position = new Vector3(18.288f,0.072f, 0.432f);
		else
			ball.transform.position = new Vector3(18.288f,0.072f,-0.432f);
		SwitchCamera(0);
		/*either 
		-knocked down all ten (a strike or a spare)
		-it's the second roll, and didn't pick up a spare 
		*/
		if(startingPins == 0) {
			StartNewFrame();
			return;
		}
		//it was the first roll, leaving one or more pins still standing
		if(roll != 1) {
			statusBar.text= "ERROR:Second roll of frame already happened";
		}
		roll = 2;
		foreach(GameObject pin in pins) {
			if(!pin.activeInHierarchy) {
				continue;
			}
			pin.GetComponent<Pin>().NewRoll();
			pin.transform.position=pinsetter[pin.name];
			Rigidbody block = pin.GetComponent<Rigidbody>();
			block.angularVelocity = Vector3.zero;
			block.velocity = Vector3.zero;
			pin.transform.eulerAngles = Vector3.zero;
		}
	}
	
	void StartNewFrame() {
		roll = 1;
		startingPins = 10;
		SetContent("Frame",(1+currentFrame).ToString());	
		SetContent("Roll1","");
		SetContent("Roll2","");
		foreach(GameObject pin in pins) {
			pin.SetActive(true);
			pin.GetComponent<Pin>().NewRoll();
			pin.transform.position=pinsetter[pin.name];
			Rigidbody block = pin.GetComponent<Rigidbody>();
			block.angularVelocity = Vector3.zero;
			block.velocity = Vector3.zero;
			pin.transform.eulerAngles = Vector3.zero;
		}
	}
	
	void SwitchCamera(int whichOne) {
		for(int i=cameras.Length-1;i>-1;i--) {
			cameras[i].SetActive(i == whichOne);
		}
				
	}
	
	void CheckPins() {
		foreach(GameObject pin in pins) {
			Rigidbody block = pin.GetComponent<Rigidbody>();
			Debug.Log(pin.name+" "+block.velocity.ToString("F2"));
		}
	}
	
	public static void EndOfRoll() {
		if(state != State.STARTED)
		{
			return;	
		}
		tMinus = 2f;
		state = State.COUNTING;
		
	}
	
	public void OnLaunch(InputAction.CallbackContext ctx) {
		if(state != State.PRIMED) {
			return;
		}
		#if UNITY_STANDALONE
			state = State.LAUNCH;
		#else
		
		TouchControl ctl = ctx.control as TouchControl;
		
		if(ctl.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended) {
			state = State.LAUNCH;
		}
		#endif
		
	}
	
	public void OnMove(InputAction.CallbackContext ctx) {
		if(state != State.PRIMED) {
			return;
		}
		Vector2Control ctl = ctx.control as Vector2Control;
		if(ctl == null) {
			Application.Quit();
		}
		float mo =  ctl.ReadValue().x;
		float delta = 0.036f*moveRatio;
		Vector3 basic = ball.transform.position;
		if(mo < 0f) 
			basic.z = Mathf.Max(basic.z - delta,-0.432f);
		if(mo > 0f)
			basic.z = Mathf.Min(0.432f,basic.z + delta);
		
		
		//statusBar.text =  mo.ToString("F2");
		ball.transform.position = basic;
	}
	
	void PostGame() {
		statusBar.text += "\r\nGAME COMPLETED";
		state = State.GAMEOVER;
	}
	
	void UpdateScore(int pincount) {
		if(worldScoring) 
			UpdateWBScore(pincount);
		else
			UpdateTraditionalScore(pincount);
	}
		
	void UpdateWBScore(int pincount) {
		switch(pincount) {
			case STRIKE:
				tally[currentFrame].roll[0] = 10;
				tally[currentFrame].roll[1] = 0; 
				//0 is hint to draw nothing
				//10 is hint to draw "X"
				tally[currentFrame].score = 30;
				break;
			case SPARE:
				tally[currentFrame].roll[1] = -1; //-1 is hint to draw "/"
				tally[currentFrame].score = 10 + tally[currentFrame].roll[0];
				break;
			default:
				tally[currentFrame].roll[roll-1] = pincount;
				if(roll == 2) {
					int framescore = 0;
					for(int i=0;i<2;i++) {
						framescore += tally[currentFrame].roll[i];
					}
					tally[currentFrame].score = framescore;
				}
				break;		
		}
		DrawStatus();
	}	
	void UpdateTraditionalScore(int pincount) {
		//in the strike/spare scenarios, we have to work in the bonus score BEFORE the number of bonus rolls is awarded; 
		switch(pincount) {
			case STRIKE:
				tally[currentFrame].roll[0] = 10;
				tally[currentFrame].roll[1] = 0; 
				//0 is hint to draw nothing
				//10 is hint to draw "X"
				tally[currentFrame].score = 10;
				//add the score from the next two rolls to this one
				bonusFrame = currentFrame;
				ApplyBonus();
				bonusRolls = 2;
				break;
			case SPARE:
				tally[currentFrame].roll[1] = -1; //-1 is hint to draw "/"
				tally[currentFrame].score = 10;
				//add the score from the next roll to this one
				bonusFrame = currentFrame;
				ApplyBonus();
				bonusRolls = 1;
				break;
			default:
				tally[currentFrame].roll[roll-1] = pincount;
				int framescore = 0;
				for(int i=0;i<2;i++) {
					framescore += tally[currentFrame].roll[i];
				}
				tally[currentFrame].score = framescore;
				ApplyBonus();
				break;
		}
		DrawStatus();
	}
	
	void DrawStatus() {
		
		for(int i=0;i<2;i++) {
			switch(tally[currentFrame].roll[i]) {
				case 10:
					SetContent(rollMsg[i],"X");
					break;
				case 0:
					SetContent(rollMsg[i]," ");
					break;
				case -1:
					SetContent(rollMsg[i],"/");
					break;
				default:
					SetContent(rollMsg[i],tally[currentFrame].roll[i].ToString());
					break;
			}
			
		}
		int total = 0;
		for(int i=0;i<=currentFrame;i++) {
			total+= tally[i].score;
		}
		SetContent("Frame",(1+currentFrame).ToString());		
		SetContent("Score",total.ToString());
	}
	
	void SetContent(string id,string content) {
		hud[id].GetComponentInChildren<Text>().text = content;
	}
	void ApplyBonus() {
		
		if(bonusFrame > -1) {
			tally[bonusFrame].score += tally[currentFrame].score;
		}
		if(bonusRolls > 0) {
			bonusRolls -= 1;
		}
		else {
			bonusFrame = -1;
		}
	}
	
	public static void UpCount(){
		stoppedPins+=1;
		Debug.Log(stoppedPins);
	}
}
