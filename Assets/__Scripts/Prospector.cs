using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Prospector : MonoBehaviour {

	static public Prospector 	S;
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

	void Awake(){
		S = this;
	}

	public List<CardProspector> drawPile;

	void Start() {
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
			print ("Game Over. You won!. :)");
		} else {
			print ("Game Over. You Lost. :(");
		}

		Application.LoadLevel ("_Prospector_Scene_0");
	}
}
