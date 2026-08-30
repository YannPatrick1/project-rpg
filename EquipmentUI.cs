using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class EquipmentUI : Control
{
	private List<EquipmentSlotUI> _slots;
	private Equipment _equipment;
	private EquipmentDatabase _database;
	private InventoryUI _inventoryUI;

	public override void _Ready()
	{
		_database = GetNode<EquipmentDatabase>("/root/World/EquipmentDatabase");
		_inventoryUI = GetNode<InventoryUI>("/root/World/InventoryUI");

		_slots = FindSlotChildren(this);
		foreach (var slot in _slots)
		{
			slot.SlotClicked += OnSlotClicked;
		}

		PartyManager.Instance.ActiveCharacterChanged += OnActiveCharacterChanged;
		if (PartyManager.Instance.GetActiveEquipment() != null)
		{
			OnActiveCharacterChanged();
		}
	}

	private void OnActiveCharacterChanged()
	{
		if (_equipment != null)
		{
			_equipment.EquipmentChanged -= RefreshDisplay;
		}

		_equipment = PartyManager.Instance.GetActiveEquipment();
		_equipment.EquipmentChanged += RefreshDisplay;
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
