using Godot;
using System.Collections.Generic;

// Attach to a node under World. Populate the Entries array in the
// Inspector with one EquipmentData resource per equippable item -- this
// is the registry that answers "does this item name have equipment
// stats, and what are they?"
public partial class EquipmentDatabase : Node
{
	[Export] public Godot.Collections.Array<EquipmentData> Entries = new();

	private Dictionary<string, EquipmentData> _byName = new();

	public override void _Ready()
	{
		foreach (var entry in Entries)
		{
			if (entry == null || string.IsNullOrEmpty(entry.ItemName)) continue;
			_byName[entry.ItemName] = entry;
		}
	}

	public EquipmentData GetData(string itemName)
	{
		return _byName.GetValueOrDefault(itemName);
	}

	public bool IsEquippable(string itemName)
	{
		return _byName.ContainsKey(itemName);
	}
}
