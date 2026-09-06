using Godot;

// Autoload singleton (registered in Project Settings -> Autoload).
// Holds the full 3-character party roster, tracks which slot is active,
// and now also owns released/joined state for every member -- moved
// here from individual Player instances so cascading rules ("last
// member left alone auto-releases", "regrouping sweeps nearby
// stragglers") can see the whole group at once instead of each
// character only knowing about itself.
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
	private bool[] _released = new bool[MaxPartySize];
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

	public bool IsReleased(int partyIndex)
	{
		if (partyIndex < 0 || partyIndex >= MaxPartySize) return false;
		return _released[partyIndex];
	}

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

	// Releases the given party member. If this leaves exactly one other
	// registered member still joined, that last member is automatically
	// released too -- a group of one doesn't mean anything (there's no
	// one left for it to lead or follow), so it collapses into everyone
	// being solo rather than leaving one character stuck half-grouped.
	public void Release(int partyIndex)
	{
		if (partyIndex < 0 || partyIndex >= MaxPartySize) return;
		if (!_party[partyIndex].HasValue) return;
		if (_released[partyIndex]) return;

		_released[partyIndex] = true;
		GD.Print(NameOf(partyIndex) + " released from the party — will hold position when not active.");

		int joinedIndex = -1;
		int joinedCount = 0;

		for (int i = 0; i < MaxPartySize; i++)
		{
			if (!_party[i].HasValue) continue;
			if (_released[i]) continue;
			joinedCount++;
			joinedIndex = i;
		}

		if (joinedCount == 1)
		{
			_released[joinedIndex] = true;
			GD.Print(NameOf(joinedIndex) + " has no one left to group with — released as well.");
		}
	}

	// Attempts to rejoin the given party member. Requires being within
	// maxDistance of at least one other registered party member (joined
	// or released -- being near anyone confirms you've walked back,
	// even if that "anyone" happens to be another straggler). On
	// success, sweeps every OTHER released member and brings back
	// anyone now within range of a joined member, repeating until
	// nothing more comes back -- so one R press regroups everyone
	// standing nearby instead of needing one press per character.
	public void Rejoin(int partyIndex, float maxDistance)
	{
		if (partyIndex < 0 || partyIndex >= MaxPartySize) return;
		if (!_party[partyIndex].HasValue) return;
		if (!_released[partyIndex]) return;
		if (_party[partyIndex].Value.Character is not Node3D self) return;

		if (!IsWithinRangeOfAnyOther(partyIndex, self, maxDistance))
		{
			GD.Print("Too far from the rest of the party to regroup — move closer first.");
			return;
		}

		_released[partyIndex] = false;
		GD.Print(NameOf(partyIndex) + " rejoined the party.");

		bool changed = true;
		while (changed)
		{
			changed = false;
			for (int j = 0; j < MaxPartySize; j++)
			{
				if (!_released[j]) continue;
				if (!_party[j].HasValue) continue;
				if (_party[j].Value.Character is not Node3D other) continue;

				if (IsWithinRangeOfAnyOther(j, other, maxDistance))
				{
					_released[j] = false;
					GD.Print(NameOf(j) + " rejoined the party.");
					changed = true;
				}
			}
		}
	}

	private bool IsWithinRangeOfAnyOther(int partyIndex, Node3D character, float maxDistance)
	{
		bool foundOther = false;

		for (int i = 0; i < MaxPartySize; i++)
		{
			if (i == partyIndex) continue;
			if (!_party[i].HasValue) continue;
			if (_party[i].Value.Character is not Node3D other) continue;

			foundOther = true;

			if (character.GlobalPosition.DistanceTo(other.GlobalPosition) <= maxDistance)
			{
				return true;
			}
		}

		return !foundOther;
	}

	private string NameOf(int partyIndex)
	{
		if (!_party[partyIndex].HasValue) return "Party member " + partyIndex;
		return _party[partyIndex].Value.Character.Name;
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
