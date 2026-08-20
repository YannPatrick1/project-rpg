using Godot;
using System.Collections.Generic;

public partial class Inventory : Node
{
	public const int SlotCount = 16; // 4x4 grid = 16 slots

	[Signal]
	public delegate void InventoryChangedEventHandler();

	private string[] _slots = new string[SlotCount];
	private int[] _quantities = new int[SlotCount];

	public string GetItemAt(int index)
	{
		if (index < 0 || index >= _slots.Length) return "";
		return _slots[index] ?? "";
	}

	public int GetQuantityAt(int index)
	{
		if (index < 0 || index >= _quantities.Length) return 0;
		return _quantities[index];
	}

	// Returns the index of the first slot containing this item, or -1 if
	// none found. Used e.g. by the chest "Open" context menu option to
	// auto-locate a matching key without the player manually selecting it.
	public int FindSlotIndex(string itemName)
	{
		for (int i = 0; i < SlotCount; i++)
		{
			if (_slots[i] == itemName)
			{
				return i;
			}
		}
		return -1;
	}

	// Removes one unit from a SPECIFIC slot, regardless of what other
	// slots might contain the same item name. Used when the player has
	// selected a particular slot in the UI (e.g. one of two "Key" slots).
	public bool RemoveItemAt(int index)
	{
		if (index < 0 || index >= SlotCount) return false;
		if (_slots[index] == null) return false;

		string itemName = _slots[index];
		_quantities[index] -= 1;
		GD.Print("Removed 1 " + itemName + " from slot " + index);

		if (_quantities[index] <= 0)
		{
			_slots[index] = null;
			_quantities[index] = 0;
		}

		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	public bool AddItem(string itemName, int quantity = 1)
	{
		if (ItemDatabase.IsStackable(itemName))
		{
			for (int i = 0; i < SlotCount; i++)
			{
				if (_slots[i] == itemName)
				{
					_quantities[i] += quantity;
					GD.Print("Added " + quantity + " " + itemName + " to existing stack in slot " + i + " (now " + _quantities[i] + ")");
					EmitSignal(SignalName.InventoryChanged);
					return true;
				}
			}
		}

		for (int i = 0; i < SlotCount; i++)
		{
			if (_slots[i] == null)
			{
				_slots[i] = itemName;
				_quantities[i] = quantity;
				GD.Print("Added " + itemName + " to slot " + i);
				EmitSignal(SignalName.InventoryChanged);
				return true;
			}
		}

		GD.Print("Your inventory is full");
		return false;
	}

	public bool HasItem(string itemName)
	{
		for (int i = 0; i < SlotCount; i++)
		{
			if (_slots[i] == itemName)
			{
				return true;
			}
		}
		return false;
	}

	public bool RemoveItem(string itemName)
	{
		for (int i = 0; i < SlotCount; i++)
		{
			if (_slots[i] == itemName)
			{
				_quantities[i] -= 1;
				GD.Print("Removed 1 " + itemName);

				if (_quantities[i] <= 0)
				{
					_slots[i] = null;
					_quantities[i] = 0;
				}

				EmitSignal(SignalName.InventoryChanged);
				return true;
			}
		}
		return false;
	}
}
