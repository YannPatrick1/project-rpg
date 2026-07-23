using Godot;

public partial class InventoryUI : Control
{
	private GridContainer _grid;
	private Inventory _inventory;

	public override void _Ready()
	{
		_grid = GetNode<GridContainer>("GridContainer");
		_inventory = GetNode<Inventory>("/root/World/PlayerInventory");
		_inventory.InventoryChanged += RefreshDisplay;
		RefreshDisplay();
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
