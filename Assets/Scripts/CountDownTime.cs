using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CountDownTime : MonoBehaviour {

	private float startTime = 300.0f; // Time given to complete game
	private float timeRemaining;

	void Start() {
		GetComponent<Text>().material.color = Color.white; // GUI text color
	}

	void Update() {
		CountDown();
	}

	void CountDown() {
		timeRemaining = startTime - Time.timeSinceLevelLoad;
		ShowTime();

		if(timeRemaining < 0) {
			timeRemaining = 0;
			TimeIsUp();
		}
	}

	void ShowTime() {
		int minutes;
		int seconds;
		string timeString;

		minutes = (int)timeRemaining / 60; // Derive minutes by dividing seconds by 60 seconds
		seconds = (int)timeRemaining % 60; // Derive remainder after dividing by 60 seconds
		timeString = "Time Left > " + minutes.ToString() + ":" + seconds.ToString("d2");
		GetComponent<Text>().text = timeString;
	}

	void TimeIsUp() {
		SceneManager.LoadScene("Lose");
	}
}