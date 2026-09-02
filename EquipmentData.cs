using Godot;

[GlobalClass]
public partial class EquipmentData : Resource
{
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

	// Minimal "use an ability while equipped" hook -- not the full
	// AbilityData/E-AP system from the Combat & Progression Design Doc
	// yet, just enough to let a specific equipped item do something when
	// right-clicked and "Use" is chosen. AbilityId is matched against a
	// switch statement in Equipment.UseEquipped(); more abilities can be
	// added there as they come up, and this can be generalized into a
	// real AbilityData resource later without changing this field.
	[ExportGroup("Ability")]
	[Export] public bool IsUsable = false;
	[Export] public string AbilityId = "";
}
