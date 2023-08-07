using System;
using HarmonyLib;
using UnityModManagerNet;

namespace TimeTravel;

[EnableReloading]
public static class TimeTravel
{
	public static UnityModManager.ModEntry.ModLogger logger;
	private static bool Load(UnityModManager.ModEntry modEntry)
	{
		Harmony? harmony = null;

		try
		{
			harmony = new Harmony(modEntry.Info.Id);
			harmony.PatchAll();
			modEntry.OnUnload = Unload;
			logger = modEntry.Logger;
		}
		catch (Exception ex)
		{
			modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
			harmony?.UnpatchAll();
			return false;
		}

		return true;
	}

	static bool Unload(UnityModManager.ModEntry modEntry)
	{
		Harmony harmony = new Harmony(modEntry.Info.Id);
		harmony.UnpatchAll();

		return true;
	}

}
