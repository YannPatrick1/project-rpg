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

		if (partyIndex == _activeIndex)
		{
			EmitSignal(SignalName.ActiveCharacterChanged);
		}
	}

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

	// Teleports every OTHER registered party member (released or not) to
	// the caster's position, fanned out slightly so they don't spawn
	// perfectly stacked. Called by Equipment.UseEquipped() when a Ring
	// of Recall (or any future item with AbilityId == "recall_party") is
	// used.
	public void RecallPartyTo(Node3D caster)
	{
		if (caster == null) return;

		Vector3 casterPos = caster.GlobalPosition;

		for (int i = 0; i < MaxPartySize; i++)
		{
			if (!_party[i].HasValue) continue;
			if (_party[i].Value.Character is not Node3D characterNode) continue;
			if (characterNode == caster) continue;

			float angle = i * Mathf.Pi * 2f / MaxPartySize;
			Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 1.2f;

			characterNode.GlobalPosition = casterPos + offset;
		}

		GD.Print("Recall cast — party gathered.");
	}

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
