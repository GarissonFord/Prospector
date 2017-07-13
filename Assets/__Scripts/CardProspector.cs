using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Enums are used when you only need "a few possible named values"
public enum CardState {
	drawpile,
	tableau,
	target,
	discard
}

public class CardProspector : Card {

	public CardState state = CardState.drawpile;
	//Will determine which cards keeps this instance face down
	public List<CardProspector> hiddenBy = new List<CardProspector>();
	//If it's a tableau card, matches to a Layout XML id
	public int layoutID;
	public SlotDef slotDef;

	override public void OnMouseUpAsButton() {
		Prospector.S.CardClicked (this);
		//Debug.Log (this);
		base.OnMouseUpAsButton ();
	}
}
