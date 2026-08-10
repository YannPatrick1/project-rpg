using Godot;
using System.Collections.Generic;
using System.Linq;

// Attach to the "EquipmentPanel" Control (a sibling of the inventory
// GridContainer, inside InventoryUI's root). Finds every EquipmentSlotUI
// child automatically, wherever it's placed in the grid layout.
public partial class EquipmentUI : Control
{
	private List<EquipmentSlotUI> _slots;
	private Equipment _equipment;
	private EquipmentDatabase _database;
	private InventoryUI _inventoryUI;

	public override void _Ready()
	{
		_equipment = GetNode<Equipment>("/root/World/PlayerEquipment");
		_database = GetNode<EquipmentDatabase>("/root/World/EquipmentDatabase");
		_inventoryUI = GetNode<InventoryUI>("/root/World/InventoryUI");

		_equipment.EquipmentChanged += RefreshDisplay;

		_slots = FindSlotChildren(this);
		foreach (var slot in _slots)
		{
			slot.SlotClicked += OnSlotClicked;
		}

		RefreshDisplay();
	}

	private List<EquipmentSlotUI> FindSlotChildren(Node root)
	{
		var found = new List<EquipmentSlotUI>();
		foreach (Node child in root.GetChildren())
		{
			if (child is EquipmentSlotUI slot)
			{
				found.Add(slot);
			}
			found.AddRange(FindSlotChildren(child));
		}
		return found;
	}

	private void OnSlotClicked(int slotIndex)
	{
		EquipSlot slot = (EquipSlot)slotIndex;

		string selectedItem = _inventoryUI.GetSelectedItem();
		int selectedIndex = _inventoryUI.GetSelectedIndex();

		if (selectedItem != null)
		{
			_equipment.EquipFromInventory(selectedIndex, slot);
			_inventoryUI.ClearSelection();
			return;
		}

		// Nothing selected in the inventory -- clicking an occupied
		// equipment slot unequips it instead.
		_equipment.Unequip(slot);
	}

	private void RefreshDisplay()
	{
		foreach (var slot in _slots)
		{
			string itemName = _equipment.GetEquipped(slot.Slot);

			if (string.IsNullOrEmpty(itemName))
			{
				slot.Clear();
			}
			else
			{
				var data = _database.GetData(itemName);
				slot.SetItem(itemName, data);
			}
		}
	}
}
