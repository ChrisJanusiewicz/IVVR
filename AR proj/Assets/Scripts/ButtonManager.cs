﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour {
	
	private bool showingDebug = false;
	private bool showingOptions = false;
	//public GameObject[] display;

	public GameObject debugPanelObject;
	public GameObject optionsPanelObject;
	public GameObject poseEstimationDropdownObject;

	private UnityARCameraManager ARCameraManager;

	public Text selectedObjectText;

	//List<string> poseEstimationTechniques = new List<string>() { "ARKit","Attached Tracker","Calibration" };

	void Start() {
		debugPanelObject.SetActive (showingDebug);
		optionsPanelObject.SetActive (showingOptions);



		ARCameraManager = GameObject.FindObjectOfType<UnityARCameraManager>();
		//selectedObjectText = poseEstimationDropdownObject.GetComponentInChildren<Text>();
	}

	public void ToggleOptions() {

		showingOptions = !showingOptions;
		optionsPanelObject.SetActive (showingOptions);

	}

	public void ToggleDebug(){
		/*
		foreach(GameObject uiElement in display){
			uiElement.active = true;
		}
		*/
		showingDebug = !showingDebug;
		debugPanelObject.SetActive (showingDebug);

	}

	public void poseEstimationDropdownIndexChange()
	{
		int index = poseEstimationDropdownObject.GetComponent<Dropdown> ().value;
		switch (index) {
		case 0:
			ARCameraManager.htcTrackerOffsetEnabled = false;
			ARCameraManager.htcTrackerRelayEnabled = false;
			break;
		case 1:
			ARCameraManager.htcTrackerOffsetEnabled = false;
			ARCameraManager.htcTrackerRelayEnabled = true;
			break;
		case 2:
			ARCameraManager.htcTrackerOffsetEnabled = true;
			ARCameraManager.htcTrackerRelayEnabled = false;
			break;
		}
	}

	public void poseEstimationDropdown()
	{
		/*switch(actions[index]){
		case "Throw": objects = new List<string>() {"Select Object","Ball"}; break;
		case "Spawn": objects = new List<string>() { "Select Object", "Brick", "Wall", "Cube","Ball" };break;
		case "Fire": objects = new List<string>() { "Select Object", "Brick", "Wall", "Cube","Ball" };break;
		case "Remove": objects = new List<string>() { "Select Object", "Brick", "Wall", "Cube","Ball" };break;
		}
		objectDropdown.ClearOptions();
		objectDropdown.AddOptions(objects);*/
	}

	public void populatePoseEstimationDropdown() {
		

	}

}