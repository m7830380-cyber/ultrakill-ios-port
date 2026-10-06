using System;
using System.Collections.Generic;
using GameConsole.CommandTree;
using Logic;
using plog;

namespace GameConsole.Commands;

public class MapVar : CommandRoot, IConsoleLogger
{
	public Logger Log { get; } = new Logger("MapVar");

	public override string Name => "MapVar";

	public override string Description => "Map variables";

	public MapVar(Console con)
		: base(con)
	{
	}

	protected override Branch BuildTree(Console con)
	{
		return CommandRoot.Branch("mapvar", CommandRoot.Leaf("reset", delegate
		{
			MonoSingleton<MapVarManager>.Instance.ResetStores();
			Log.Info("Stores have been reset.");
		}, requireCheats: true), CommandRoot.Leaf("stash_info", delegate
		{
			bool hasStashedStore = MonoSingleton<MapVarManager>.Instance.HasStashedStore;
			Log.Info("Stash exists: " + hasStashedStore);
		}, requireCheats: true), CommandRoot.Leaf("stash_stores", delegate
		{
			MonoSingleton<MapVarManager>.Instance.StashStore();
			Log.Info("Stores have been stashed.");
		}, requireCheats: true), CommandRoot.Leaf("restore_stash", delegate
		{
			MonoSingleton<MapVarManager>.Instance.RestoreStashedStore();
			Log.Info("Stores have been restored.");
		}, requireCheats: true), CommandRoot.Leaf("list", delegate
		{
			List<VariableSnapshot> allVariables = MonoSingleton<MapVarManager>.Instance.GetAllVariables();
			foreach (VariableSnapshot item in allVariables)
			{
				Log.Info($"{item.name} ({GetFriendlyTypeName(item.type)}) - <color=orange>{item.value}</color>");
			}
			if (allVariables.Count == 0)
			{
				Log.Info("No map variables have been set.");
			}
		}, requireCheats: true), BoolMenu("logging", () => MapVarManager.LoggingEnabled, delegate(bool value)
		{
			MapVarManager.LoggingEnabled = value;
		}, inverted: false, requireCheats: true), CommandRoot.Leaf("set_int", delegate(string variableName, int value)
		{
			MonoSingleton<MapVarManager>.Instance.SetInt(variableName, value);
		}, requireCheats: true), CommandRoot.Leaf("set_bool", delegate(string variableName, bool value)
		{
			MonoSingleton<MapVarManager>.Instance.SetBool(variableName, value);
		}, requireCheats: true), CommandRoot.Leaf("toggle_bool", delegate(string variableName)
		{
			MonoSingleton<MapVarManager>.Instance.SetBool(variableName, MonoSingleton<MapVarManager>.Instance.GetBool(variableName) != true);
		}, requireCheats: true), CommandRoot.Leaf("set_float", delegate(string variableName, float value)
		{
			MonoSingleton<MapVarManager>.Instance.SetFloat(variableName, value);
		}, requireCheats: true), CommandRoot.Leaf("set_string", delegate(string variableName, string value)
		{
			MonoSingleton<MapVarManager>.Instance.SetString(variableName, value);
		}, requireCheats: true));
	}

	public static string GetFriendlyTypeName(Type type)
	{
		if (type == typeof(int))
		{
			return "int";
		}
		if (type == typeof(float))
		{
			return "float";
		}
		if (type == typeof(string))
		{
			return "string";
		}
		if (type == typeof(bool))
		{
			return "bool";
		}
		return type.Name;
	}
}
