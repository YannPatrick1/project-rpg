using Godot;
using System.Collections.Generic;

public partial class Chest : StaticBody3D, ILootable, IExaminable
{
	[Export] public string RequiredKeyName = "Key";
	[Export] public bool IsOpen = false;
	[Export] public float ResetSeconds = 30f;

	// Dull Sword is a 100% guaranteed drop -- it's just always in the list,
	// no randomness involved (unlike the NPC's chance-based Key drop).
	public List<string> Items { get; set; } = new List<string> { "Dull Sword", "Ring of Recall" };

	// Snapshot of the chest's starting loot, taken once at _Ready. Items
	// itself gets emptied out as the player loots it, so this is what the
	// reset timer restocks from.
	private List<string> _originalItems;

	private MeshInstance3D _mesh;
	private Material _closedMaterial;
	private Timer _resetTimer;

	public override void _Ready()
	{
		_mesh = GetNode<MeshInstance3D>("MeshInstance3D");
		_closedMaterial = _mesh.MaterialOverride; // whatever it looked like before Open() ever ran
		_originalItems = new List<string>(Items);

		_resetTimer = GetNode<Timer>("ResetTimer");
		_resetTimer.OneShot = true;
		_resetTimer.Timeout += OnResetTimerTimeout;
	}

	public void Open()
	{
		IsOpen = true;
		var mat = new StandardMaterial3D();
		mat.AlbedoColor = new Color(1f, 0.84f, 0f); // gold, just so open state is visually obvious
		_mesh.MaterialOverride = mat;

		_resetTimer.Start(ResetSeconds);
	}

	public void RemoveItem(string itemName)
	{
		Items.Remove(itemName);
	}

	// Fires ResetSeconds after Open() was called -- relocks the chest and
	// restocks its original loot, regardless of how much the player took.
	private void OnResetTimerTimeout()
	{
		IsOpen = false;
		Items = new List<string>(_originalItems);
		_mesh.MaterialOverride = _closedMaterial;
		GD.Print(NameOrFallback() + " reset and relocked.");
	}

	private string NameOrFallback()
	{
		return string.IsNullOrEmpty(Name) ? "The chest" : Name;
	}

	// Three distinct states, each with its own line: closed, open with
	// loot still inside, and open-but-empty (looted out). Order of checks
	// matters -- IsOpen must be checked before Items.Count.
	public string GetExamineText()
	{
		if (!IsOpen)
		{
			return "A sturdy wooden chest, locked tight. Whatever's inside will have to wait for the right key.";
		}

		if (Items.Count > 0)
		{
			return "An open chest, its lid thrown back. Something's still glinting inside.";
		}

		return "An open chest, picked clean. Nothing left in here but dust and disappointment.";
	}
}
