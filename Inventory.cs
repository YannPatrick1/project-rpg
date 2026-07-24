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

	public bool AddItem(string itemName, int quantity = 1)
	{
		// If this item type stacks, look for an existing stack of it first.
		// This is allowed even when the inventory is otherwise full, since
		// it doesn't need a new slot.
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

		// No existing stack (or not stackable) — find a fresh empty slot.
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

	// Removes ONE unit of the item. For stackable items, this decrements the
	// stack; the slot only clears once quantity reaches 0.
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
