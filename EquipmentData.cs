using Godot;

// A Resource, not a Node -- this means each equipment item's stats live
// in their own .tres file, fully editable in the Inspector, with no code
// changes needed to add or rebalance gear. This is the pattern to follow
// for every future equipment item.
[GlobalClass]
public partial class EquipmentData : Resource
{
	// Must exactly match the item name string used everywhere else
	// (Chest loot lists, ItemDatabase, etc.) -- this is how the game
	// looks up "does this inventory item have equipment stats?"
	[Export] public string ItemName = "";

	[Export] public EquipSlot Slot = EquipSlot.Body;
	[Export] public ElementType Element = ElementType.None;

	[ExportGroup("Stat Bonuses")]
	[Export] public int MaxHealthBonus = 0;
	[Export] public int StrengthBonus = 0;
	[Export] public int DefenseBonus = 0;
	[Export] public int RangeStrengthBonus = 0;
	[Export] public int AgilityBonus = 0;
	[Export] public int MagicBonus = 0;
	[Export] public int ResistanceBonus = 0;
}
