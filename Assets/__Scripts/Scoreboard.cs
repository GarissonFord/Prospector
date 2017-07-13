using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Scoreboard : MonoBehaviour {
	public static Scoreboard S;

	public GameObject prefabFloatingScore;

	public bool _____________________;
	[SerializeField]
	private int _score = 0;
	public string _scoreString;
	public GameObject canvas;

	public int score {
		get {
			return(_score);
		}
		set {
			_score = value;
			_scoreString = Utils.AddCommasToNumber (_score);
		}
	}

	public string scoreString {
		get {
			return(_scoreString);
		}
		set {
			scoreString = value;
		}
	}

	void Awake() {
		S = this;
		canvas = GameObject.Find ("Canvas");
	}

	public void FSCallback(FloatingScore fs) {
		score += fs.score;
	}
		
	//Instantiates a new FloatingScore, initializes it
	//Returns a pointer to the created FloatingScore 
	//to allow more freedoms like altering font sizes
	public FloatingScore CreateFloatingScore(int amt, List<Vector3> pts) {
		GameObject go = Instantiate (prefabFloatingScore) as GameObject;
		go.transform.SetParent (canvas.transform, true);
		Vector3 fsPos = new Vector3 (0.5f, 100.0f, 0.0f);
		go.transform.position = fsPos;
		FloatingScore fs = go.GetComponent<FloatingScore> ();
		fs.score = amt;
		fs.reportFinishTo = this.gameObject;
		fs.Init (pts);
		return(fs);
	}
}
