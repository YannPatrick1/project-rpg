using Godot;
using System.Collections.Generic;

public partial class EquipmentSlotUI : Panel
{
	[Signal] public delegate void SlotClickedEventHandler(int slotIndex);

	// Set this per-instance in the Inspector (Head/Body/Legs/Feet/LeftHand/RightHand).
	[Export] public EquipSlot Slot;

	private Label _label;

	public override void _Ready()
	{
		_label = GetNode<Label>("Label");
		Clear();
	}

	public void SetItem(string itemName, EquipmentData data)
	{
		string text = itemName;

		if (data != null)
		{
			string bonuses = FormatBonuses(data);
			if (!string.IsNullOrEmpty(bonuses))
			{
				text += "\n" + bonuses;
			}
		}

		_label.Text = text;
	}

	public void Clear()
	{
		_label.Text = "";
	}

	private string FormatBonuses(EquipmentData data)
	{
		var parts = new List<string>();

		if (data.StrengthBonus != 0) parts.Add(FormatStat("STR", data.StrengthBonus));
		if (data.DefenseBonus != 0) parts.Add(FormatStat("DEF", data.DefenseBonus));
		if (data.RangeStrengthBonus != 0) parts.Add(FormatStat("RNG", data.RangeStrengthBonus));
		if (data.AgilityBonus != 0) parts.Add(FormatStat("AGI", data.AgilityBonus));
		if (data.MagicBonus != 0) parts.Add(FormatStat("MAG", data.MagicBonus));
		if (data.ResistanceBonus != 0) parts.Add(FormatStat("RES", data.ResistanceBonus));
		if (data.MaxHealthBonus != 0) parts.Add(FormatStat("HP", data.MaxHealthBonus));

		return string.Join(" ", parts);
	}

	private string FormatStat(string label, int value)
	{
		string sign = value > 0 ? "+" : "";
		return sign + value + " " + label;
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
		{
			EmitSignal(SignalName.SlotClicked, (int)Slot);
		}
	}
}
