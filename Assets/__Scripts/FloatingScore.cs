using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//possible states of a floating score
public enum FSState {
	idle, 
	pre,
	active,
	post
}

public class FloatingScore : MonoBehaviour {
	public FSState state = FSState.idle;
	//[Serialize Field]
	private int _score = 0;
	public string scoreString;

	public int score {
		get {
			return(_score);
		}
		set {
			_score = value;
			scoreString = Utils.AddCommasToNumber (_score);
			GetComponent<GUIText> ().text = scoreString;
		}
	}

	public List<Vector3> bezierPts; //Bezier points for movement
	public List<float> fontSizes; //font scaling
	public float timeStart = -1f;
	public float timeDuration = 1f;
	public string easingCurve = Easing.InOut; //from Utils

	//Will receive the SendMessage when this is done moving
	public GameObject reportFinishTo = null;

	//These parameters have default values
	public void Init(List<Vector3> ePts, float eTimeS = 0, float eTimeD = 1) {
		bezierPts = new List<Vector3> (ePts);

		if (ePts.Count == 1) { //only one point
			transform.position = ePts[0];
			return;
		}

		//If eTimeS is default
		if(eTimeS == 0) eTimeS = Time.time;
		timeStart = eTimeS;
		timeDuration = eTimeD;

		state = FSState.pre;
	}

	public void FSCallback(FloatingScore fs) {
		score += fs.score;
	}

	void Update() {
		if (state == FSState.idle) return;

		float u = (Time.time - timeStart) / timeDuration;
		float uC = Easing.Ease (u, easingCurve);
		if (u < 0) {
			state = FSState.pre;
			transform.position = bezierPts [0];
		} else {
			if (u >= 1) { //u >=1 means we're done moving
				uC = 1; 
				state = FSState.post;
				if (reportFinishTo != null) {
					//SendMessage to call FSCallback
					reportFinishTo.SendMessage ("FSCallback", this);
					Destroy (gameObject);
				} else { //If there is nothing to callback
					state = FSState.idle;
				}
			} else {
				state = FSState.active;
			}

			Vector3 pos = Utils.Bezier (uC, bezierPts);
			transform.position = pos;
			if (fontSizes != null && fontSizes.Count > 0) {
				int size = Mathf.RoundToInt (Utils.Bezier (uC, fontSizes));
				GetComponent<GUIText>().fontSize = size;
			}
		}
	}
}
