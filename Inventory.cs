using Godot;
using System.Collections.Generic;

public partial class Inventory : Node
{
	public const int SlotCount = 16; // 4x4 grid = 16 slots

	private string[] _slots = new string[SlotCount];

	public bool AddItem(string itemName)
	{
		for (int i = 0; i < SlotCount; i++)
		{
			if (_slots[i] == null)
			{
				_slots[i] = itemName;
				GD.Print("Added " + itemName + " to slot " + i);
				return true;
			}
		}
		GD.Print("Inventory full, could not add " + itemName);
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
				_slots[i] = null;
				GD.Print("Removed " + itemName);
				return true;
			}
		}
		return false;
	}
}
