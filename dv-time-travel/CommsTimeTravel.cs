using System.Collections.Generic;
using DV;
using HarmonyLib;
using UnityEngine;

/*
 * Copyright (c) 2023 MisfitMaid <misfit@misfitmaid.com>
 * Use of this source code is governed by the MIT license, which
 * can be found in the LICENSE file.
 */

namespace TimeTravel;


/**
 * largely pulled and adapted from the Skin Manager comms radio class 
 */

public class CommsTimeTravel : MonoBehaviour, ICommsRadioMode
{
	public static CommsRadioController Controller;

	public ButtonBehaviourType ButtonBehaviour { get; private set; }

	public CommsRadioDisplay display;
	public AudioClip SelectedCarSound;

	public void OverrideSignalOrigin(Transform signalOrigin) { }
	public void OnUpdate() { }

	private Time selectedTime;
	private State currentState;

	public Color GetLaserBeamColor()
	{
		return Color.clear;
	}

	#region Initialization

	public void Awake()
	{
		// steal components from other radio modes
		CommsRadioCarDeleter deleter = Controller.deleteControl;
		SelectedCarSound = deleter.warningSound;

		if (deleter != null)
		{
			display = deleter.display;
		}
		else
		{
			TimeTravel.logger.Log("CommsTimeTravel: couldn't get properties from siblings");
		}
	}

	public void Start()
	{
		if (display == null)
		{
			TimeTravel.logger.Error("CommsTimeTravel: display not set, can't function properly!");
		}
	}

	public void Enable() { }

	public void Disable()
	{
		ResetState();
	}

	public void SetStartingDisplay()
	{
		string content = "Change the time?";
		display.SetDisplay("TIME", content, "");
	}

	#endregion

	#region State Machine Actions

	private void SetState(State newState)
	{
		if (newState == currentState) return;

		currentState = newState;
		switch (newState)
		{
			case State.Entry:
				SetStartingDisplay();
				ButtonBehaviour = ButtonBehaviourType.Regular;
				break;
			case State.SelectTime:
				CommsRadioController.PlayAudioFromRadio(SelectedCarSound, transform);
				ButtonBehaviour = ButtonBehaviourType.Override;
				updateDisplay();
				break;
		}
	}

	private void ResetState()
	{
		SetState(State.Entry);
	}

	public void OnUse()
	{
		switch (currentState)
		{
			case State.Entry:
				CommsRadioController.PlayAudioFromRadio(SelectedCarSound, transform);
				SetState(State.SelectTime);
				break;
			case State.SelectTime:
				switch (selectedTime)
				{
					case Time.nevermind:
						SetState(State.Entry);
						break;
					default:
						CommsRadioController.PlayAudioFromRadio(SelectedCarSound, transform);
						DoTimeTravel(selectedTime);
						break;
				}
				break;
		}
	}

	private void DoTimeTravel(Time selectedTime)
	{
		uint hour;
		switch (selectedTime)
		{
			case Time.midnight: hour = 0; break;
			case Time.morning: hour = 6; break;
			case Time.noon: hour = 12; break;
			case Time.evening: hour = 18; break;
			default: hour = 0; break; // ????????????????????
		}

		TimeTravel.logger.Log($"Attempting to set time to {hour}:00");

		TOD_Sky sky = UnityEngine.Object.FindObjectOfType<TOD_Sky>();
		if (sky != null)
		{
			sky.Cycle.Hour = hour;
			SetState(State.Entry);
		}
		else
		{
			TimeTravel.logger.Error("Could not find TOD_Sky object! This is a bug, please report it!");
		}

	}


	private void updateDisplay()
	{
		if (selectedTime == Time.nevermind)
		{
			display.SetDisplay("TIME", "Cancel", "");
		}
		else
		{
			display.SetDisplay("TIME", $"Set to {selectedTime}", "");
		}

	}

	public bool ButtonACustomAction()
	{
		if (selectedTime == Time.nevermind)
		{
			selectedTime = Time.midnight;
		}
		else
		{
			selectedTime++;
		}
		updateDisplay();
		return true;
	}

	public bool ButtonBCustomAction()
	{
		if (selectedTime == Time.midnight)
		{
			selectedTime = Time.nevermind;
		}
		else
		{
			selectedTime--;
		}
		updateDisplay();
		return true;
	}

	#endregion

	protected enum Time
	{
		midnight,
		morning,
		noon,
		evening,
		nevermind
	}
	private enum State
	{
		Entry,
		SelectTime,
	}
}

[HarmonyPatch(typeof(CommsRadioController), "Awake")]
static class CommsRadio_Awake_Patch
{
	public static CommsTimeTravel timeTravel = null;

	static void Postfix(CommsRadioController __instance, List<ICommsRadioMode> ___allModes)
	{
		CommsTimeTravel.Controller = __instance;
		timeTravel = __instance.gameObject.AddComponent<CommsTimeTravel>();
		___allModes.Add(timeTravel);
	}
}
