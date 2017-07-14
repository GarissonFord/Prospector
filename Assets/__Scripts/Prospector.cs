using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Enum to handle scoring 
public enum ScoreEvent {
	draw,
	mine,
	mindGold,
	gameWin,
	gameLoss
}

public class Prospector : MonoBehaviour {

	static public Prospector 	S;
	static public int SCORE_FROM_PREV_ROUND = 0;
	static public int HIGH_SCORE = 0;

	public float reloadDelay = 1f; //Delay between rounds

	public Vector3 fsPosMid = new Vector3 (0.5f, 0.90f, 0);
	public Vector3 fsPosRun = new Vector3(0.5f, 0.75f, 0);
	public Vector3 fsPosMid2 = new Vector3 (0.5f, 0.5f, 0);
	public Vector3 fsPosEnd = new Vector3(1.0f, 0.65f, 0);

	public Deck					deck;
	public TextAsset			deckXML;

	public Layout layout;
	public TextAsset layoutXML;

	public Vector3 layoutCenter;
	public float xOffset = 3f;
	public float yOffset = -2.5f;
	public Transform layoutAnchor;

	public CardProspector target;
	public List<CardProspector> tableau;
	public List<CardProspector> discardPile;
	public List<CardProspector> drawPile;

	//Fields to track score
	public int chain = 0; //# of cards in a run
	public int scoreRun = 0;
	public int score = 0;
	public FloatingScore fsRun;

	public Text GTGameOver;
	public Text GTRoundResult;
	public Text highScore;
	public Text scoreText;

	void UpdateScore() {
		scoreText.text = score.ToString();
	}

	void Awake(){
		S = this;
		//Check for a high score in Player Prefs
		if(PlayerPrefs.HasKey("ProspectorHighScore")){
			HIGH_SCORE = PlayerPrefs.GetInt("ProspectorHighScore");
		}
		//Add score from last round, which will be >0 if it was a win
		score += SCORE_FROM_PREV_ROUND;
		//Reset
		SCORE_FROM_PREV_ROUND = 0;

		//Set up texts that show at the end of the round
		highScore.text = "High Score: " + Utils.AddCommasToNumber(HIGH_SCORE);
		GTGameOver.text = "";
		UpdateScore();
	}

	void ShowResultGTs(bool show) {
		GTGameOver.gameObject.SetActive (show);
		GTRoundResult.gameObject.SetActive (show);
	}

	void Start() {
		Scoreboard.S.score = score;

		deck = GetComponent<Deck> ();
		deck.InitDeck (deckXML.text);
		Deck.Shuffle (ref deck.cards);

		layout = GetComponent<Layout> ();
		layout.ReadLayout (layoutXML.text);

		drawPile = ConvertListCardsToListCardProspectors (deck.cards);
		LayoutGame ();
	}

	//Pulls a single card from the drawPile and returns it
	CardProspector Draw() {
		CardProspector cd = drawPile [0];
		drawPile.RemoveAt (0);
		return(cd);
	}

	CardProspector FindCardByLayoutID(int layoutID) {
		foreach (CardProspector tCP in tableau) {
			if (tCP.layoutID == layoutID) {
				return (tCP);
			}
		}

		return (null);
	}

	//Positions the initial tableau, like the "mine"
	void LayoutGame() {
		//Empty GameObject serves as an anchor
		if (layoutAnchor == null) {
			GameObject tGO = new GameObject ("_LayoutAnchor");
			// ^ create an empty in the hierarchy
			layoutAnchor = tGO.transform;
			layoutAnchor.transform.position = layoutCenter;
		}

		CardProspector cp;
		foreach (SlotDef tSD in layout.slotDefs) {
			cp = Draw (); 
			cp.faceUp = tSD.faceUp;
			cp.transform.parent = layoutAnchor;
			//Replaces the previous parent
			cp.transform.localPosition = new Vector3 (
				layout.multiplier.x * tSD.x,
				layout.multiplier.y * tSD.y,
				-tSD.layerID
			);
			// ^ set localPosition based on slotDef
			cp.layoutID = tSD.id;
			cp.slotDef = tSD;
			cp.state = CardState.tableau;

			cp.SetSortingLayerName (tSD.layerName);

			tableau.Add (cp); // add to the List<> tableau
		}

		foreach (CardProspector tCP in tableau) {
			foreach (int hid in tCP.slotDef.hiddenBy) {
				cp = FindCardByLayoutID (hid);
				tCP.hiddenBy.Add (cp);
			}
		}

		//Set up the initial target card
		MoveToTarget(Draw ());

		//Set up draw pile
		UpdateDrawPile();
	}

	List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD){
		List<CardProspector> lCP = new List<CardProspector> ();
		CardProspector tCP;
		foreach (Card tCD in lCD) {
			tCP = tCD as CardProspector;
			lCP.Add (tCP);
		}
		return(lCP);
	}

	public void CardClicked(CardProspector cd) {
		switch (cd.state) {
		case CardState.target:
			break;
		case CardState.drawpile:
			MoveToDiscard (target); //Move the target to the discard pile
			MoveToTarget (Draw ()); //Move the next drawn card to the target
			UpdateDrawPile (); 
			ScoreManager (ScoreEvent.draw);
			break;
		case CardState.tableau:
			//Clicking card in tableau will check if it's a valid play
			bool validMatch = true;
			if (!cd.faceUp) {
				//If it is face down
				validMatch = false;
			}
			if (!AdjacentRank (cd, target)) {
				//If it's not adjacent
				validMatch = false;
			}
			if (!validMatch)
				return;

			tableau.Remove (cd);
			MoveToTarget (cd);
			SetTableauFaces ();
			ScoreManager (ScoreEvent.mine);
			break;
		}

		CheckForGameOver ();
	}

	//Moves card to the discard pile
	void MoveToDiscard(CardProspector cd){
		cd.state = CardState.discard;
		discardPile.Add (cd);
		cd.transform.parent = layoutAnchor;
		cd.transform.localPosition = new Vector3 (
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID + 0.5f
		);
		cd.faceUp = true;
		cd.SetSortingLayerName (layout.discardPile.layerName);
		cd.SetSortOrder (-100 + discardPile.Count);
	}

	//Make cd the new target card
	void MoveToTarget(CardProspector cd) {
		if (target != null)
			MoveToDiscard (target);

		target = cd;
		cd.state = CardState.target;
		cd.transform.parent = layoutAnchor;
		cd.transform.localPosition = new Vector3 (
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID
		);
		cd.faceUp = true;
		cd.SetSortingLayerName (layout.discardPile.layerName);
		cd.SetSortOrder (0);
	}

	//Arranges all the draw pile to show how many are left
	void UpdateDrawPile() {
		CardProspector cd;
		for (int i = 0; i < drawPile.Count; i++) {
			cd = drawPile [i];
			cd.transform.parent = layoutAnchor;
			Vector2 dpStagger = layout.drawPile.stagger;
			cd.transform.localPosition = new Vector3 (
				layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x),
				layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y),
				-layout.drawPile.layerID + 0.1f * i
			);
			cd.faceUp = false;
			cd.state = CardState.drawpile;
			cd.SetSortingLayerName (layout.drawPile.layerName);
			cd.SetSortOrder (-10 * i);
		}
	}

	public bool AdjacentRank(CardProspector c0, CardProspector c1){
		//If either is face-down
		if(!c0.faceUp || !c1.faceUp) return(false);

		//If they are 1 apart, they are adjacent
		if(Mathf.Abs(c0.rank - c1.rank) == 1) {
			return(true);
		}

		//If one is A and the other King, adjacent
		if(c0.rank == 1 && c1.rank == 13) return (true);
		if (c0.rank == 13 && c1.rank == 1) return (true);

		return(false);
	}

	void SetTableauFaces() {
		foreach (CardProspector cd in tableau) {
			bool fup = true; //assume card is face up 
			foreach (CardProspector cover in cd.hiddenBy) {
				if (cover.state == CardState.tableau) {
					fup = false;
				}
			}
			cd.faceUp = fup;
		}
	}

	void CheckForGameOver() {
		//If the tableau is empty
		if (tableau.Count == 0) {
			GameOver (true);
			return;
		}
		//If there are still cards in the draw pile
		if (drawPile.Count > 0) {
			return;
		}
		//Check for remaining valid palys
		foreach (CardProspector cd in tableau) {
			if (AdjacentRank (cd, target)) {
				return;
			}
		}

		GameOver (false);
	}

	void GameOver(bool won) {
		if (won) {
			//print ("Game Over. You won!. :)");
			ScoreManager(ScoreEvent.gameWin);
			GTGameOver.text = "Round Won";
		} else {
			//print ("Game Over. You Lost. :(");
			ScoreManager(ScoreEvent.gameLoss);
			GTGameOver.text = "Game Over";
		}

		Invoke ("ReloadLevel", reloadDelay);
	}

	void ReloadLevel() {
		//Application.LoadLevel ("_Prospector_Scene_0");
		SceneManager.LoadScene("__Prospector_Scene_0");
	}

	//Score handler
	void ScoreManager(ScoreEvent sEvt) {
		List<Vector3> fsPts;
		switch (sEvt) {
		case ScoreEvent.draw: //drawing a card
		case ScoreEvent.gameWin: //won round
		case ScoreEvent.gameLoss: //lost round
			chain = 0;
			score += scoreRun;
			scoreRun = 0;
			//Add fsRun to the scoreboard score
			if (fsRun != null) {
				//Bezier curve points
				fsPts = new List<Vector3> ();
				fsPts.Add (fsPosRun);
				fsPts.Add (fsPosMid2);
				fsPts.Add (fsPosEnd);
				fsRun.reportFinishTo = Scoreboard.S.gameObject;
				fsRun.Init (fsPts, 0, 1);
				fsRun.fontSizes = new List<float> (new float[] { 28, 36, 4 });
				fsRun = null;
			}
			break;
		case ScoreEvent.mine:
			chain++;
			scoreRun += chain;
			//Create the FloatingScore
			FloatingScore fs;
			//Move it from the mousePosition to fsPosRun
			Vector3 p0 = Input.mousePosition;
			p0.x /= Screen.width;
			p0.y /= Screen.height;
			fsPts = new List<Vector3> ();
			fsPts.Add (p0);
			fsPts.Add (fsPosMid);
			fsPts.Add (fsPosRun);
			fs = Scoreboard.S.CreateFloatingScore (chain, fsPts);
			//Debug.Log ("Chain: " + chain + " FloatingScore: " + fs);
			fs.fontSizes = new List<float> (new float[] { 4, 50, 28 });
			if (fsRun == null) {
				fsRun = fs;
				fsRun.reportFinishTo = null;
			} else {
				fs.reportFinishTo = fsRun.gameObject;
			}
			break;
		}

		//The second switch handles round wins and losses
		switch (sEvt) {
		case ScoreEvent.gameWin:
			//Add the score to the next round 
			Prospector.SCORE_FROM_PREV_ROUND = score;
			print ("You won this round!\nRound score: " + score);
			ShowResultGTs (true);
			break;
		case ScoreEvent.gameLoss:
			//Check against high score
			if (Prospector.HIGH_SCORE <= score) {
				print ("You got the high score!\nHigh score: " + score);
				Prospector.HIGH_SCORE = score;
				PlayerPrefs.SetInt ("ProspectorHighScore", score);
			} else {
				print ("Your final score for the game was: " + score);
			}
			break;
		default:
			//Commented out console print meant to test the scoring system
			//print ("score: " + score + " scoreRun:" + scoreRun + " chain:" + chain);
			break;
		}

		UpdateScore ();
	}
}
