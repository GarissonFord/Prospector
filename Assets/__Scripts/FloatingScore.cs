using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

//possible states of a floating score
public enum FSState {
	idle, 
	pre,
	active,
	post
}

//Bezier curves allow this text to float around on screen
public class FloatingScore : MonoBehaviour {
	public FSState state = FSState.idle;
	[SerializeField]
	private int _score = 0;
	public string scoreString;

	public int score {
		get {
			return(_score);
		}
		set {
			_score = value;
			scoreString = Utils.AddCommasToNumber (_score);
			GetComponent<Text>().text = scoreString;
		}
	}

	//Sets up the score and its movement
	public List<Vector3> bezierPts; //Bezier points for movement
	public List<float> fontSizes; //font scaling
	public float timeStart = -1.0f;
	public float timeDuration = 1.0f;
	public string easingCurve = Easing.InOut; //from Utils

	//Will receive the SendMessage when this is done moving
	public GameObject reportFinishTo = null;

	//These parameters have default values
	public void Init(List<Vector3> ePts, float eTimeS = 0.0f, float eTimeD = 1.0f) {
		bezierPts = new List<Vector3> (ePts);

		if (ePts.Count == 1) { //only one point?
			//just go to that point then
			transform.position = ePts[0];
			return;
		}

		//If eTimeS is the default value, just set it to the current time
		if(eTimeS == 0) eTimeS = Time.time;
		timeStart = eTimeS;
		timeDuration = eTimeD;

		state = FSState.pre; //ready to move
	}

	public void FSCallback(FloatingScore fs) {
		//When this is called by SendMessage
		//add the score from the calling FloatingScore
		score += fs.score;
	}

	public void setPos(Vector3 pos) {
		Vector2 pos2d = new Vector2 (pos.x, pos.y);
		RectTransform rt = GetComponent<RectTransform> ();
		rt.anchorMin = pos2d;
		rt.anchorMax = pos2d;
		rt.anchoredPosition3D = Vector3.zero;
	}

	void Update() {
		//If not moving
		if (state == FSState.idle) return;

		//u ranges between 0 and 1
		float u = (Time.time - timeStart) / timeDuration;
		//The Easing class from Utils curves the u value
		float uC = Easing.Ease (u, easingCurve);
		if (u < 0) { //If u < 0, we shouldn't move yet
			state = FSState.pre;
			//transform.position = bezierPts [0];
			setPos(bezierPts[0]);
		} else {
			if (u >= 1) { //u >=1 means we're done moving
				uC = 1; //To avoid overshooting
				state = FSState.post;
				if (reportFinishTo != null) { //If we have a callback gameObject
					//SendMessage to call FSCallback
					reportFinishTo.SendMessage ("FSCallback", this);
					Destroy (gameObject);
				} else { //If there is nothing to callback
					//Just let it remain still
					state = FSState.idle;
				}
			} else {
				//0 <= u < 1 so we are active and moving
				state = FSState.active;
			}
			//Use Bezier curves to move
			Vector3 pos = Utils.Bezier (uC, bezierPts);
			//transform.position = pos;
			setPos (pos);
			if (fontSizes != null && fontSizes.Count > 0) {
				int size = Mathf.RoundToInt (Utils.Bezier (uC, fontSizes));
				//GetComponent<GUIText>().fontSize = size;
				GetComponent<Text>().fontSize = size;
			}
		}
	}
}
