using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SlotDef {
	public float x;
	public float y;
	public bool faceUp = false;
	public string layerName = "Default";
	public int layerID = 0;
	public int id;
	public List<int> hiddenBy = new List<int>();
	public string type = "slot";
	public Vector2 stagger;
}

public class Layout : MonoBehaviour {

	public PT_XMLReader xmlr;
	public PT_XMLHashtable xml; //Easier xml access
	public Vector2 multiplier; //Spacing of the tableau
	//SlowDef references
	public List<SlotDef> slotDefs;
	public SlotDef drawPile;
	public SlotDef discardPile;
	//All the possible names for layerID
	public string[] sortingLayerNames = new string[] {"Row0", "Row1", "Row2", "Row3", "Discard", "Draw"};

	//Reads in the LayoutXML file
	public void ReadLayout(string xmlText) {
		xmlr = new PT_XMLReader ();
		xmlr.Parse (xmlText); //Parse the xml file
		xml = xmlr.xml["xml"][0]; //xml is set as a shortcut to the XML file

		//Reads in multiplier for card spacing
		multiplier.x = float.Parse(xml["multiplier"][0].att("x"));
		multiplier.y = float.Parse (xml["multiplier"][0].att ("y"));

		//Reads in slots
		SlotDef tSD;
		//slotsX is a shortut to all the <slot>s
		PT_XMLHashList slotsX = xml["slot"];

		for (int i = 0; i < slotsX.Count; i++) {
			tSD = new SlotDef (); //new SlotDef instance
			if (slotsX [i].HasAtt ("type")) {
				//If this <slot> has a type attribute then we parse it
				tSD.type = slotsX [i].att ("type");
			} else {
				//Otherwise set its type to slot to be a tableau card
				tSD.type = "slot";
			}
			//Many attributes are parsed to numbers
			tSD.x = float.Parse(slotsX[i].att("x"));
			tSD.y = float.Parse (slotsX [i].att ("y"));
			tSD.layerID = int.Parse (slotsX [i].att ("layer"));
			//The number of layerID will become a text
			tSD.layerName = sortingLayerNames[tSD.layerID];

			switch (tSD.type) {
				//Gets additional attributes based on the <slot> type
			case "slot":
				tSD.faceUp = (slotsX [i].att ("faceup") == "1");
				tSD.id = int.Parse (slotsX [i].att ("id"));
				if (slotsX [i].HasAtt ("hiddenby")) {
					string[] hiding = slotsX [i].att ("hiddenby").Split (',');
					foreach (string s in hiding) {
						tSD.hiddenBy.Add (int.Parse (s));
					}
				}
				slotDefs.Add (tSD);
				break;

			case "drawpile":
				tSD.stagger.x = float.Parse (slotsX [i].att ("xstagger"));
				drawPile = tSD;
				break;
			case "discardpile":
				discardPile = tSD;
				break;
			}
		}
	}
}
