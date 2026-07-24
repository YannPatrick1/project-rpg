using System.Collections.Generic;

public static class ItemDatabase
{
	// Add one line here per new item as you create them.
	// Anything not listed defaults to "not stackable" — safe default.
	private static readonly Dictionary<string, bool> _stackable = new()
	{
		{ "Coins", true },
		{ "Key", false },
		{ "Bones", false },
		{ "Gold", false },
		{ "Gem", false },
	};

	public static bool IsStackable(string itemName)
	{
		return _stackable.TryGetValue(itemName, out bool stackable) && stackable;
	}
}
