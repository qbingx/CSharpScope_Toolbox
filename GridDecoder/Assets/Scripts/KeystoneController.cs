﻿using System;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Keystone settings (loaded from JSON).
/// </summary>
[System.Serializable]
public class KeystoneSettings {
	
	// Quad control variables
	public Vector3[] vertices = new Vector3[4];

	public KeystoneSettings(Vector3[] newVertices) {
		this.vertices = newVertices;
	}
}

public class KeystoneController : MonoBehaviour
{

	KeystoneSettings settings;

	public Vector3[] _vertices;
	private Vector3[] vertices;
	private GameObject[] _corners;
	public int selectedCorner;
	private Mesh mesh;
	private Vector2[] widths_heights = new Vector2[4];
	private bool needUpdate = true;

	public string _settingsFileName = "_keystoneSettings.json";

	public bool _useKeystone = true;
	public bool _debug = false;
	private float speed = 0.005f;

	/// <summary>
	/// Start this instance.
	/// </summary>
	void Start ()
	{
		EventManager.StartListening ("reload", OnReloadKeystone);

		Destroy (this.GetComponent <MeshCollider> ()); //destroy so we can make one in dynamically 
		transform.gameObject.AddComponent <MeshCollider> (); //add new collider 
	
		mesh = GetComponent<MeshFilter> ().mesh; // get this GO mesh
		mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };	
		LoadSettings ();

		CornerMaker (); //make the corners for visual controls 
	}

	/// <summary>
	/// Update this instance.
	/// </summary>
	void Update ()
	{
		if (_useKeystone) {
			OnSceneControl ();

			if (needUpdate) {
				SetupMesh ();
				needUpdate = false;
			}
		}
			
		onOffObjects (_useKeystone); // toggles onoff at each click
	}

	/// <summary>
	/// Reinitializes the mesh.
	/// </summary>
	private void SetupMesh() {
		mesh.vertices = vertices;

		// Zero out the left and bottom edges, 
		// leaving a right trapezoid with two sides on the axes and a vertex at the origin.
		var shiftedPositions = new Vector2[] {
			Vector2.zero,
			new Vector2 (0, vertices [1].y - vertices [0].y),
			new Vector2 (vertices [2].x - vertices [1].x, vertices [2].y - vertices [3].y),
			new Vector2 (vertices [3].x - vertices [0].x, 0)
		};
		mesh.uv = shiftedPositions;

		widths_heights [0].x = widths_heights [3].x = shiftedPositions [3].x;
		widths_heights [1].x = widths_heights [2].x = shiftedPositions [2].x;
		widths_heights [0].y = widths_heights [1].y = shiftedPositions [1].y;
		widths_heights [2].y = widths_heights [3].y = shiftedPositions [2].y;
		mesh.uv2 = widths_heights;

		transform.GetComponent<MeshCollider> ().sharedMesh = mesh;
	}

	/// Methods section 
	private void CornerMaker ()
	{
		_corners = new GameObject[vertices.Length]; // make corners spheres 
		for (int i = 0; i < vertices.Length; i++) {
			var obj = GameObject.CreatePrimitive (PrimitiveType.Sphere);
			obj.transform.SetParent (transform);
			obj.GetComponent<Renderer> ().material.color = i == selectedCorner ? Color.green : Color.red;
			_corners [i] = obj;
		}
	}

	/// <summary>
	/// Ons the off objects.
	/// </summary>
	/// <param name="visible">If set to <c>true</c> visible.</param>
	private void onOffObjects (bool visible)
	{
		for (int i = 0; i < vertices.Length; i++) {
			GameObject sphere = _corners [i];
			sphere.transform.position = transform.TransformPoint (vertices [i]);
			sphere.SetActive (visible);
		}
	}

	/// <summary>
	/// Raises the scene control event.
	/// </summary>
	private void OnSceneControl ()
	{
		if (Input.anyKey && _debug)
			Debug.Log("Current input is " + Input.inputString);

		if (Input.GetKey (KeyCode.S)) {
			SaveSettings ();
			return;
		} else if (Input.GetKey (KeyCode.L)) {
			LoadSettings ();
			return;
		}

		if (!_useKeystone)
			return;
		UpdateSelection ();
	}

	/// <summary>
	/// Saves the settings to a JSON.
	/// </summary>
	/// <returns><c>true</c>, if settings were saved, <c>false</c> otherwise.</returns>
	private bool SaveSettings() {
		if (_debug)
			Debug.Log ("Saving keystone settings.");

		settings.vertices = vertices;

		string dataAsJson = JsonUtility.ToJson (settings);
		JsonParser.writeJSON (_settingsFileName, dataAsJson);

		return true;
	}

	/// <summary>
	/// Loads the settings.
	/// </summary>
	/// <returns><c>true</c>, if settings were loaded, <c>false</c> otherwise.</returns>
	private bool LoadSettings() {
		if (_debug)
			Debug.Log ("Loading keystone settings.");

		string dataAsJson = JsonParser.loadJSON (_settingsFileName, _debug);

		if (dataAsJson.Length == 0)
			settings = new KeystoneSettings (_vertices);
		else {
			settings = JsonUtility.FromJson<KeystoneSettings> (dataAsJson);
			vertices = settings.vertices;
		}
		SetupMesh ();

		return true;
	}

	/// <summary>
	/// Updates the selection for each keypress event.
	/// </summary>
	private void UpdateSelection() {
		if (Input.anyKey)
			needUpdate = true;
		
		var corner = Input.GetKeyDown ("1") ? 0 : (Input.GetKeyDown ("2") ? 1 : (Input.GetKeyDown ("3") ? 2 : (Input.GetKeyDown ("4") ? 3 : selectedCorner)));

		if (corner != selectedCorner) {
			_corners [selectedCorner].GetComponent<Renderer> ().material.color = Color.red;
			_corners [corner].GetComponent<Renderer> ().material.color = Color.green;
			selectedCorner = corner;
			if (_debug) 
				Debug.Log ("Selection changed to " + selectedCorner.ToString ());
		}

		if (Input.GetKey (KeyCode.LeftShift))
			speed *= 10;
		else if (Input.GetKey (KeyCode.LeftControl) && Input.GetKey (KeyCode.LeftAlt))
			speed *= 0.1f;
		else if (Input.GetKey (KeyCode.LeftAlt))
			speed *= 0.01f; 

		var v = vertices [selectedCorner];

		if (Input.GetKeyDown (KeyCode.UpArrow))
			v = v + speed * Vector3.up;
		else if (Input.GetKeyDown (KeyCode.DownArrow))
			v = v + speed * Vector3.down;
		else if (Input.GetKeyDown (KeyCode.LeftArrow))
			v = v + speed * Vector3.left;
		else if (Input.GetKeyDown (KeyCode.RightArrow))
			v = v + speed * Vector3.right;
		else needUpdate = false;

		vertices [selectedCorner] = v;
	}


	/// <summary>
	/// Reloads configuration / keystone settings when the scene is refreshed.
	/// </summary>
	void OnReloadKeystone() {
		Debug.Log ("Keystone config was reloaded!");

		LoadSettings ();
	}
}



