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
		{ "Ring of Recall", false },
	};

	private static readonly Dictionary<string, string> _singularName = new()
	{
		{ "Coins", "Gold Coin" },
	};

	private static readonly Dictionary<string, string> _pluralName = new()
	{
		{ "Coins", "Gold Coins" },
	};

	private static readonly Dictionary<string, string> _singleInstanceName = new()
	{
		{ "Bones", "Bone" },
	};

	private static readonly Dictionary<string, string> _examineText = new()
	{
		{ "Coins", "Gold coins, jingling with the promise of better gear (eventually)." },
		{ "Key", "A simple iron key. Whatever it opens, it's not telling." },
		{ "Bones", "The bones of something that used to move. Best not to think about it too hard." },
		{ "Gold", "A lump of raw gold. Shiny, heavy, and not currently doing much." },
		{ "Gem", "A gem that catches the light nicely. Valuable, or just very convincing glass." },
		{ "Dull Sword", "A sword so dull it might bruise before it cuts. Still technically a weapon." },
		{ "Ring of Recall", "A plain silver band, faintly warm to the touch. It always seems to know where the rest of the party is." },
	};

	public static bool IsStackable(string itemName)
	{
		return _stackable.TryGetValue(itemName, out bool stackable) && stackable;
	}

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

	public static string GetSingleInstanceName(string itemName)
	{
		return _singleInstanceName.GetValueOrDefault(itemName, itemName);
	}

	public static string GetExamineText(string itemName)
	{
		return _examineText.GetValueOrDefault(itemName, "It's " + itemName + ". Nothing more to say about it.");
	}
}
