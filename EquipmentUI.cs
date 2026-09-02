using Godot;
using System;
using System.Collections.Generic;

public partial class EquipmentUI : Control
{
	private List<EquipmentSlotUI> _slots;
	private Equipment _equipment;
	private EquipmentDatabase _database;
	private InventoryUI _inventoryUI;

	private PopupMenu _contextMenu;
	private List<Action> _contextMenuActions = new();

	public override void _Ready()
	{
		_database = GetNode<EquipmentDatabase>("/root/World/EquipmentDatabase");
		_inventoryUI = GetNode<InventoryUI>("/root/World/InventoryUI");

		_contextMenu = GetNode<PopupMenu>("/root/World/EquipmentContextMenu");
		_contextMenu.IndexPressed += OnContextMenuIndexPressed;

		_slots = FindSlotChildren(this);
		foreach (var slot in _slots)
		{
			slot.SlotClicked += OnSlotClicked;
			slot.SlotRightClicked += OnSlotRightClicked;
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

	// New: right-click on an equipped slot pops "Use" (only if the item
	// is usable), "Unequip", "Examine" -- mirrors the pattern InventoryUI
	// already uses for inventory items.
	private void OnSlotRightClicked(int slotIndex)
	{
		EquipSlot slot = (EquipSlot)slotIndex;
		string itemName = _equipment.GetEquipped(slot);
		if (string.IsNullOrEmpty(itemName)) return;

		var data = _database.GetData(itemName);

		_contextMenu.Clear();
		_contextMenuActions.Clear();

		if (data != null && data.IsUsable)
		{
			_contextMenu.AddItem("Use");
			_contextMenuActions.Add(() => _equipment.UseEquipped(slot));
		}

		_contextMenu.AddItem("Unequip");
		_contextMenuActions.Add(() => _equipment.Unequip(slot));

		_contextMenu.AddItem("Examine");
		_contextMenuActions.Add(() => GD.Print(ItemDatabase.GetExamineText(itemName)));

		_contextMenu.Position = (Vector2I)GetGlobalMousePosition();
		_contextMenu.Popup();
	}

	private void OnContextMenuIndexPressed(long index)
	{
		if (index < 0 || index >= _contextMenuActions.Count) return;
		var action = _contextMenuActions[(int)index];
		_contextMenuActions.Clear();
		action?.Invoke();
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
