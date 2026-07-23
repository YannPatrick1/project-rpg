using Godot;

public partial class InventorySlot : Panel
{
	private Label _label;

	public override void _Ready()
	{
		_label = GetNode<Label>("Label");
	}

	public void SetItem(string itemName)
	{
		_label.Text = itemName;
	}

	public void Clear()
	{
		_label.Text = "";
	}
}
