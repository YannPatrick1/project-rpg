using Godot;
using System.Collections.Generic;

// Attach as a child of a character node (e.g. "Player"), alongside
// sibling "PlayerInventory" and "PlayerStats" nodes. Tracks what's
// currently equipped in each slot and keeps that character's
// PlayerStats in sync.
//
// EquipmentDatabase is the one exception still fetched by absolute
// path -- it's shared item data, not per-character state, so every
// character reads from the same one.
public partial class Equipment : Node
{
	[Signal] public delegate void EquipmentChangedEventHandler();

	private Dictionary<EquipSlot, string> _equipped = new();

	private Inventory _inventory;
	private EquipmentDatabase _database;
	private PlayerStats _stats;

	public override void _Ready()
	{
		_inventory = GetParent().GetNode<Inventory>("PlayerInventory");
		_stats = GetParent().GetNode<PlayerStats>("PlayerStats");
		_database = GetNode<EquipmentDatabase>("/root/World/EquipmentDatabase");
	}

	public string GetEquipped(EquipSlot slot)
	{
		return _equipped.GetValueOrDefault(slot);
	}

	public bool EquipFromInventory(int inventoryIndex, EquipSlot targetSlot)
	{
		string itemName = _inventory.GetItemAt(inventoryIndex);
		if (string.IsNullOrEmpty(itemName)) return false;

		var data = _database.GetData(itemName);
		if (data == null)
		{
			GD.Print("That item can't be equipped.");
			return false;
		}

		if (data.Slot != targetSlot)
		{
			GD.Print("That doesn't go in that slot.");
			return false;
		}

		string currentItem = GetEquipped(targetSlot);

		if (!string.IsNullOrEmpty(currentItem))
		{
			bool madeRoom = _inventory.AddItem(currentItem);
			if (!madeRoom)
			{
				GD.Print("Your inventory is full -- can't swap that out.");
				return false;
			}
		}

		_inventory.RemoveItemAt(inventoryIndex);
		_equipped[targetSlot] = itemName;

		RecalculateStats();
		EmitSignal(SignalName.EquipmentChanged);
		return true;
	}

	public bool Unequip(EquipSlot slot)
	{
		string itemName = GetEquipped(slot);
		if (string.IsNullOrEmpty(itemName)) return false;

		bool added = _inventory.AddItem(itemName);
		if (!added)
		{
			GD.Print("Your inventory is full.");
			return false;
		}

		_equipped.Remove(slot);
		RecalculateStats();
		EmitSignal(SignalName.EquipmentChanged);
		return true;
	}

	private void RecalculateStats()
	{
		int maxHealth = 0, strength = 0, defense = 0, rangeStrength = 0, agility = 0, magic = 0, resistance = 0;

		foreach (var kvp in _equipped)
		{
			var data = _database.GetData(kvp.Value);
			if (data == null) continue;

			maxHealth += data.MaxHealthBonus;
			strength += data.StrengthBonus;
			defense += data.DefenseBonus;
			rangeStrength += data.RangeStrengthBonus;
			agility += data.AgilityBonus;
			magic += data.MagicBonus;
			resistance += data.ResistanceBonus;
		}

		_stats.SetEquipmentBonuses(maxHealth, strength, defense, rangeStrength, agility, magic, resistance);
	}
}
