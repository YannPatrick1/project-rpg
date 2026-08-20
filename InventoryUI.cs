using Godot;
using System;
using System.Collections.Generic;

public partial class InventoryUI : Control
{
	private GridContainer _grid;
	private Inventory _inventory;
	private Equipment _equipment;
	private EquipmentDatabase _equipmentDatabase;
	private int _selectedIndex = -1;

	private Control _equipmentPanel;
	private Button _inventoryButton;
	private Button _equipmentButton;

	private PopupMenu _itemContextMenu;
	private List<Action> _contextMenuActions = new();

	public override void _Ready()
	{
		_grid = GetNode<GridContainer>("GridContainer");
		_inventory = GetNode<Inventory>("/root/World/PlayerInventory");
		_inventory.InventoryChanged += RefreshDisplay;

		_equipment = GetNode<Equipment>("/root/World/PlayerEquipment");
		_equipmentDatabase = GetNode<EquipmentDatabase>("/root/World/EquipmentDatabase");

		_itemContextMenu = GetNode<PopupMenu>("/root/World/ItemContextMenu");
		_itemContextMenu.IndexPressed += OnItemContextMenuIndexPressed;

		_equipmentPanel = GetNode<Control>("EquipmentPanel");
		_inventoryButton = GetNode<Button>("TabButtons/InventoryButton");
		_equipmentButton = GetNode<Button>("TabButtons/EquipmentButton");
		_inventoryButton.Pressed += ShowInventoryTab;
		_equipmentButton.Pressed += ShowEquipmentTab;

		for (int i = 0; i < _grid.GetChildCount(); i++)
		{
			var slot = _grid.GetChild<InventorySlot>(i);
			slot.SetIndex(i);
			slot.SlotClicked += OnSlotClicked;
			slot.SlotRightClicked += OnSlotRightClicked;
		}

		RefreshDisplay();
		ShowInventoryTab();
	}

	private void ShowInventoryTab()
	{
		_grid.Visible = true;
		_equipmentPanel.Visible = false;
		ClearSelection();
	}

	private void ShowEquipmentTab()
	{
		_grid.Visible = false;
		_equipmentPanel.Visible = true;
		ClearSelection();
	}

	public int GetSelectedIndex()
	{
		return _selectedIndex;
	}

	private void OnSlotClicked(int index)
	{
		if (_selectedIndex >= 0)
		{
			if (index == _selectedIndex)
			{
				ClearSelection();
				return;
			}

			string secondItem = _inventory.GetItemAt(index);
			if (string.IsNullOrEmpty(secondItem))
			{
				ClearSelection();
				return;
			}

			GD.Print("Nothing noteworthy happened");
			ClearSelection();
			return;
		}

		string item = _inventory.GetItemAt(index);
		if (string.IsNullOrEmpty(item)) return;

		// Left-clicking an equippable item attempts to equip it right away
		// (OSRS convention: left-click = default action). Non-equippable
		// items fall back to the old "select for use on target" flow
		// (e.g. selecting a key to use on a chest).
		var equipData = _equipmentDatabase.GetData(item);
		if (equipData != null)
		{
			// TODO: once levels exist, check the item's level requirement
			// against the player's level here before allowing the equip.
			_equipment.EquipFromInventory(index, equipData.Slot);
			return;
		}

		_selectedIndex = index;
		UpdateSelectionVisuals();
	}

	private void OnSlotRightClicked(int index)
	{
		string item = _inventory.GetItemAt(index);
		if (string.IsNullOrEmpty(item)) return;

		_itemContextMenu.Clear();
		_contextMenuActions.Clear();

		var equipData = _equipmentDatabase.GetData(item);
		if (equipData != null)
		{
			_itemContextMenu.AddItem("Equip");
			_contextMenuActions.Add(() =>
			{
				// TODO: once levels exist, check the item's level requirement
				// against the player's level here before allowing the equip.
				_equipment.EquipFromInventory(index, equipData.Slot);
			});
		}

		_itemContextMenu.AddItem("Examine");
		_contextMenuActions.Add(() =>
		{
			GD.Print(ItemDatabase.GetExamineText(item));
		});

		_itemContextMenu.Position = (Vector2I)GetGlobalMousePosition();
		_itemContextMenu.Popup();
	}

	private void OnItemContextMenuIndexPressed(long index)
	{
		if (index < 0 || index >= _contextMenuActions.Count) return;
		var action = _contextMenuActions[(int)index];
		_contextMenuActions.Clear();
		action?.Invoke();
	}

	public string GetSelectedItem()
	{
		if (_selectedIndex < 0) return null;
		return _inventory.GetItemAt(_selectedIndex);
	}

	public void ClearSelection()
	{
		_selectedIndex = -1;
		UpdateSelectionVisuals();
	}

	private void UpdateSelectionVisuals()
	{
		for (int i = 0; i < _grid.GetChildCount(); i++)
		{
			_grid.GetChild<InventorySlot>(i).SetSelected(i == _selectedIndex);
		}
	}

	private void RefreshDisplay()
	{
		for (int i = 0; i < _grid.GetChildCount(); i++)
		{
			var slot = _grid.GetChild<InventorySlot>(i);
			string item = _inventory.GetItemAt(i);

			if (string.IsNullOrEmpty(item))
			{
				slot.Clear();
			}
			else
			{
				slot.SetItem(item, _inventory.GetQuantityAt(i));
			}
		}
	}
}
