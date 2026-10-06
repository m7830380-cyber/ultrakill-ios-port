using System;
using GameConsole.CommandTree;
using plog;

namespace GameConsole.Commands;

internal class Style : CommandRoot, IConsoleLogger
{
	public Logger Log { get; } = new Logger("Style");

	public override string Name => "Style";

	public override string Description => "Modify your style score";

	public Style(Console con)
		: base(con)
	{
	}

	protected override Branch BuildTree(Console con)
	{
		return CommandRoot.Branch("style", BoolMenu("meter", () => MonoSingleton<StyleHUD>.Instance.forceMeterOn, (bool value) =>
		{
			MonoSingleton<StyleHUD>.Instance.forceMeterOn = value;
		}, inverted: false, requireCheats: true), CommandRoot.Branch("freshness", CommandRoot.Leaf("get", () =>
		{
			Log.Info($"Current weapon freshness is {MonoSingleton<StyleHUD>.Instance.GetFreshness(MonoSingleton<GunControl>.Instance.currentWeapon)}");
		}), CommandRoot.Leaf("set", (float amt) =>
		{
			Log.Info($"Set current weapon freshness to {amt}");
			MonoSingleton<StyleHUD>.Instance.SetFreshness(MonoSingleton<GunControl>.Instance.currentWeapon, amt);
		}, requireCheats: true), CommandRoot.Leaf("lock_state", (int slot, StyleFreshnessState state) =>
		{
			Log.Info($"Locking slot {slot} to {Enum.GetName(typeof(StyleFreshnessState), state)}");
			MonoSingleton<StyleHUD>.Instance.LockFreshness(slot, state);
		}, requireCheats: true), CommandRoot.Leaf("unlock", (int slot) =>
		{
			Log.Info($"Unlocking slot {slot}");
			MonoSingleton<StyleHUD>.Instance.UnlockFreshness(slot);
		}, requireCheats: true)));
	}
}
