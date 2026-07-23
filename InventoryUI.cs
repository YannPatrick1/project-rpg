using Godot;

public partial class InventoryUI : Control
{
	private GridContainer _grid;
	private Inventory _inventory;
	private int _selectedIndex = -1;

	public override void _Ready()
	{
		_grid = GetNode<GridContainer>("GridContainer");
		_inventory = GetNode<Inventory>("/root/World/PlayerInventory");
		_inventory.InventoryChanged += RefreshDisplay;

		for (int i = 0; i < _grid.GetChildCount(); i++)
		{
			var slot = _grid.GetChild<InventorySlot>(i);
			slot.SetIndex(i);
			slot.SlotClicked += OnSlotClicked;
		}

		RefreshDisplay();
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

		_selectedIndex = index;
		UpdateSelectionVisuals();
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
				slot.SetItem(item);
			}
		}
	}
}
