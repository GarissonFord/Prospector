using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Card : MonoBehaviour {

	public string    suit;
	public int       rank;
	public Color     color = Color.black;
	public string    colS = "Black";  // or "Red"
	
	public List<GameObject> decoGOs = new List<GameObject>();
	public List<GameObject> pipGOs = new List<GameObject>();
	
	public GameObject back;     // back of card;
	public CardDefinition def;  // from DeckXML.xml		

	//List of SpriteRenderer components of this object
	public SpriteRenderer[] spriteRenderers;

	void Start() {
		SetSortOrder(0);
	}
	
	// property
	public bool faceUp {
		get {
			return (!back.activeSelf);
		}		
		set {
			back.SetActive(!value);
		}
	}	

	//If spriteRenderers is not yet defined, this does so
	public void PopulateSpriteRenderers() {
		if (spriteRenderers == null || spriteRenderers.Length == 0) {
			spriteRenderers = GetComponentsInChildren<SpriteRenderer> ();
		}
	}

	//Sets sortingLayerName on all SR components
	public void SetSortingLayerName(string tSLN) {
		PopulateSpriteRenderers ();

		foreach (SpriteRenderer tSR in spriteRenderers) {
			tSR.sortingLayerName = tSLN;
		}
	}

	public void SetSortOrder(int sOrd) {
		PopulateSpriteRenderers ();

		//White background of the card is on the bottom (sOrd)
		//Then the pips, decorators, face and such (sOrd + 1)
		//The back is on top when visible (sOrd + 2)

		//Iterate through all spriteRenderers
		foreach (SpriteRenderer tSR in spriteRenderers) {
			if (tSR.gameObject == this.gameObject) {
				tSR.sortingOrder = sOrd;
				continue;
			}

			switch (tSR.gameObject.name) {
			case "back":
				tSR.sortingOrder = sOrd + 2;

				break;
			case "face":
			default:
				tSR.sortingOrder = sOrd + 1;
				break;
			}
		}
	}

	//Virtual methods can be overridden by subclass methods with the same name
	virtual public void OnMouseUpAsButton() {
		//print (name);
	}

} // class Card

[System.Serializable]
public class Decorator{
	public string	type;			// For card pips, tyhpe = "pip"
	public Vector3	loc;			// location of sprite on the card
	public bool		flip = false;	//whether to flip vertically
	public float 	scale = 1.0f;
}

[System.Serializable]
public class CardDefinition{
	public string	face;	//sprite to use for face cart
	public int		rank;	// value from 1-13 (Ace-King)
	public List<Decorator>	
					pips = new List<Decorator>();  // Pips Used					
} // Class CardDefinition
