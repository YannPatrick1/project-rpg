using Godot;

public partial class InventorySlot : Panel
{
	[Signal]
	public delegate void SlotClickedEventHandler(int index);

	private Label _label;
	private int _index;
	private StyleBoxFlat _normalStyle;
	private StyleBoxFlat _selectedStyle;

	public override void _Ready()
	{
		_label = GetNode<Label>("Label");

		_normalStyle = new StyleBoxFlat();
		_normalStyle.BgColor = new Color(0.2f, 0.2f, 0.2f);

		_selectedStyle = new StyleBoxFlat();
		_selectedStyle.BgColor = new Color(0.7f, 0.6f, 0.1f);

		AddThemeStyleboxOverride("panel", _normalStyle);
	}

	public void SetIndex(int index)
	{
		_index = index;
	}

	public void SetItem(string itemName, int quantity)
	{
		_label.Text = quantity > 1 ? itemName + " x" + quantity : itemName;
	}

	public void Clear()
	{
		_label.Text = "";
	}

	public void SetSelected(bool selected)
	{
		AddThemeStyleboxOverride("panel", selected ? _selectedStyle : _normalStyle);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
		{
			EmitSignal(SignalName.SlotClicked, _index);
		}
	}
}
