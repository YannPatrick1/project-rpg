using Godot;

// Attach to a node under World (e.g. "PlayerStats"). Holds base stats
// (set in the Inspector) plus the current equipment bonus, which
// Equipment.cs recalculates and pushes in whenever gear changes.
public partial class PlayerStats : Node
{
	[Signal] public delegate void StatsChangedEventHandler();

	[Export] public int BaseMaxHealth = 10;
	[Export] public int BaseStrength = 1;
	[Export] public int BaseDefense = 1;
	[Export] public int BaseRangeStrength = 1;
	[Export] public int BaseAgility = 1;
	[Export] public int BaseMagic = 1;
	[Export] public int BaseResistance = 1;

	private int _bonusMaxHealth = 0;
	private int _bonusStrength = 0;
	private int _bonusDefense = 0;
	private int _bonusRangeStrength = 0;
	private int _bonusAgility = 0;
	private int _bonusMagic = 0;
	private int _bonusResistance = 0;

	// Effective totals -- base stat + whatever's currently equipped.
	public int MaxHealth => BaseMaxHealth + _bonusMaxHealth;
	public int Strength => BaseStrength + _bonusStrength;
	public int Defense => BaseDefense + _bonusDefense;
	public int RangeStrength => BaseRangeStrength + _bonusRangeStrength;
	public int Agility => BaseAgility + _bonusAgility;
	public int Magic => BaseMagic + _bonusMagic;
	public int Resistance => BaseResistance + _bonusResistance;

	// Raw bonus-only values, for UI display purposes (e.g. "(+4)").
	public int BonusMaxHealth => _bonusMaxHealth;
	public int BonusStrength => _bonusStrength;
	public int BonusDefense => _bonusDefense;
	public int BonusRangeStrength => _bonusRangeStrength;
	public int BonusAgility => _bonusAgility;
	public int BonusMagic => _bonusMagic;
	public int BonusResistance => _bonusResistance;

	// Called by Equipment.cs after any equip/unequip. Replaces the whole
	// bonus set at once -- simplest way to stay in sync with "sum of
	// everything currently equipped".
	public void SetEquipmentBonuses(int maxHealth, int strength, int defense, int rangeStrength, int agility, int magic, int resistance)
	{
		_bonusMaxHealth = maxHealth;
		_bonusStrength = strength;
		_bonusDefense = defense;
		_bonusRangeStrength = rangeStrength;
		_bonusAgility = agility;
		_bonusMagic = magic;
		_bonusResistance = resistance;

		EmitSignal(SignalName.StatsChanged);
	}
}
