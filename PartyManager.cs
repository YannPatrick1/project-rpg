using Godot;

// Autoload singleton (registered in Project Settings -> Autoload).
// Holds the full 3-character party roster and tracks which slot is
// currently under player control. Each Player instance registers itself
// in its own _Ready() via RegisterPartyMember(...), tagged with a
// PartyIndex (0, 1, or 2) set in the Inspector on that instance.
public partial class PartyManager : Node
{
	public const int MaxPartySize = 3;

	[Signal] public delegate void ActiveCharacterChangedEventHandler();

	public static PartyManager Instance { get; private set; }

	private struct PartyMember
	{
		public Node Character;
		public Inventory Inventory;
		public Equipment Equipment;
		public PlayerStats Stats;
	}

	private PartyMember?[] _party = new PartyMember?[MaxPartySize];
	private int _activeIndex = 0;

	public override void _Ready()
	{
		Instance = this;
	}

	// Called by a character node (e.g. Player.cs) in its own _Ready() to
	// announce "I exist, here are my nodes, this is my party slot."
	public void RegisterPartyMember(int partyIndex, Node character, Inventory inventory, Equipment equipment, PlayerStats stats)
	{
		if (partyIndex < 0 || partyIndex >= MaxPartySize)
		{
			GD.PrintErr("PartyManager: invalid PartyIndex " + partyIndex + " on " + character.Name);
			return;
		}

		_party[partyIndex] = new PartyMember
		{
			Character = character,
			Inventory = inventory,
			Equipment = equipment,
			Stats = stats
		};

		// If this slot happens to be the active one (slot 0 at game
		// start, most likely), tell everyone listening (UI, etc.) that
		// there's now an active character to read from. Registration
		// order between the three Player instances doesn't matter --
		// whichever one carries PartyIndex 0 fires this.
		if (partyIndex == _activeIndex)
		{
			EmitSignal(SignalName.ActiveCharacterChanged);
		}
	}

	// Switches control to a different party slot (0, 1, or 2). No-op if
	// that slot has no registered character yet, or is already active.
	public void SetActiveIndex(int partyIndex)
	{
		if (partyIndex < 0 || partyIndex >= MaxPartySize) return;
		if (!_party[partyIndex].HasValue) return;
		if (partyIndex == _activeIndex) return;

		_activeIndex = partyIndex;
		EmitSignal(SignalName.ActiveCharacterChanged);
	}

	public int GetActiveIndex() => _activeIndex;
	public bool IsActiveIndex(int partyIndex) => partyIndex == _activeIndex;

	public Node GetActiveCharacter() => _party[_activeIndex]?.Character;
	public Inventory GetActiveInventory() => _party[_activeIndex]?.Inventory;
	public Equipment GetActiveEquipment() => _party[_activeIndex]?.Equipment;
	public PlayerStats GetActiveStats() => _party[_activeIndex]?.Stats;

	// Global party-switch hotkeys. Lives here (not on Player.cs) because
	// it has to work regardless of which character is currently active --
	// an inactive character stops processing input entirely.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.Key1) SetActiveIndex(0);
			else if (keyEvent.Keycode == Key.Key2) SetActiveIndex(1);
			else if (keyEvent.Keycode == Key.Key3) SetActiveIndex(2);
		}
	}
}
