using System.Collections.Generic;

public static class ItemDatabase
{
	private static readonly Dictionary<string, bool> _stackable = new()
	{
		{ "Coins", true },
		{ "Key", false },
		{ "Bones", false },
		{ "Gold", false },
		{ "Gem", false },
	};

	// Only stackable items need singular/plural names — non-stackable
	// items just display their raw item name as-is.
	private static readonly Dictionary<string, string> _singularName = new()
	{
		{ "Coins", "Gold Coin" },
	};

	private static readonly Dictionary<string, string> _pluralName = new()
	{
		{ "Coins", "Gold Coins" },
	};

	public static bool IsStackable(string itemName)
	{
		return _stackable.TryGetValue(itemName, out bool stackable) && stackable;
	}

	// Returns the full display text for an item + quantity,
	// e.g. "1 Gold Coin" or "3 Gold Coins". Non-stackable items
	// just return their raw name, ignoring quantity.
	public static string GetDisplayText(string itemName, int quantity)
	{
		if (!IsStackable(itemName))
		{
			return itemName;
		}

		string name = quantity == 1
			? _singularName.GetValueOrDefault(itemName, itemName)
			: _pluralName.GetValueOrDefault(itemName, itemName + "s");

		return quantity + " " + name;
	}
}
