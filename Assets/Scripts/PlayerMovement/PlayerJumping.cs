using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumping : MonoBehaviour {

	public Vector2 jumpForce = new Vector2 (0, 300);
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown("space")){

			GetComponent<Rigidbody2D> ().velocity = Vector2.zero;
			GetComponent<Rigidbody2D> ().AddForce (jumpForce);
		}
	
	}
}
