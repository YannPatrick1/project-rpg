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
		{ "Dull Sword", false },
	};

	// Used for stackable items' combined display, e.g. "1 Gold Coin" / "3 Gold Coins".
	private static readonly Dictionary<string, string> _singularName = new()
	{
		{ "Coins", "Gold Coin" },
	};

	private static readonly Dictionary<string, string> _pluralName = new()
	{
		{ "Coins", "Gold Coins" },
	};

	// Used for NON-stackable items' per-instance display, e.g. one "Bones"
	// entry in the pile should read as "Bone" in the loot menu.
	private static readonly Dictionary<string, string> _singleInstanceName = new()
	{
		{ "Bones", "Bone" },
	};

	// Flavor text shown when the player right-clicks an item and chooses
	// "Examine". New items only need an entry here to get examine support —
	// no other code changes needed. Falls back to a generic line if missing.
	private static readonly Dictionary<string, string> _examineText = new()
	{
		{ "Coins", "Gold coins, jingling with the promise of better gear (eventually)." },
		{ "Key", "A simple iron key. Whatever it opens, it's not telling." },
		{ "Bones", "The bones of something that used to move. Best not to think about it too hard." },
		{ "Gold", "A lump of raw gold. Shiny, heavy, and not currently doing much." },
		{ "Gem", "A gem that catches the light nicely. Valuable, or just very convincing glass." },
		{ "Dull Sword", "A sword so dull it might bruise before it cuts. Still technically a weapon." },
	};

	public static bool IsStackable(string itemName)
	{
		return _stackable.TryGetValue(itemName, out bool stackable) && stackable;
	}

	// Full display text for a stackable item + quantity, e.g. "3 Gold Coins".
	// Non-stackable items just return their raw name (unaffected by quantity).
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

	// Display name for ONE instance of a non-stackable item, e.g.
	// "Bones" -> "Bone". Falls back to the raw item name if no override
	// is defined, so new items work without needing an entry right away.
	public static string GetSingleInstanceName(string itemName)
	{
		return _singleInstanceName.GetValueOrDefault(itemName, itemName);
	}

	// Examine text for an inventory item. Falls back to a generic line so
	// new items never crash the examine flow, just look a bit plain until
	// someone writes real flavor text for them.
	public static string GetExamineText(string itemName)
	{
		return _examineText.GetValueOrDefault(itemName, "It's " + itemName + ". Nothing more to say about it.");
	}
}
