using Godot;

// Autoload singleton (registered in Project Settings -> Autoload).
// Tracks which character is currently controlled and hands out that
// character's Inventory/Equipment/PlayerStats to anything that needs
// them -- UI, world objects like KeyPickup, etc. -- instead of those
// scripts hardcoding a path like "/root/World/PlayerInventory".
//
// Right now only one character ever registers (the solo Player), but
// this is the seam where character-switching (1/2/3 keys) will plug in
// later without requiring changes anywhere else.
public partial class PartyManager : Node
{
	[Signal] public delegate void ActiveCharacterChangedEventHandler();

	public static PartyManager Instance { get; private set; }

	private Node _activeCharacter;
	private Inventory _activeInventory;
	private Equipment _activeEquipment;
	private PlayerStats _activeStats;

	public override void _Ready()
	{
		Instance = this;
	}

	// Called by a character node (e.g. Player.cs) in its own _Ready() to
	// announce "I'm the active character, here are my nodes."
	public void RegisterActiveCharacter(Node character, Inventory inventory, Equipment equipment, PlayerStats stats)
	{
		_activeCharacter = character;
		_activeInventory = inventory;
		_activeEquipment = equipment;
		_activeStats = stats;

		EmitSignal(SignalName.ActiveCharacterChanged);
	}

	public Node GetActiveCharacter() => _activeCharacter;
	public Inventory GetActiveInventory() => _activeInventory;
	public Equipment GetActiveEquipment() => _activeEquipment;
	public PlayerStats GetActiveStats() => _activeStats;
}
