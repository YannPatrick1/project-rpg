using Godot;
using System.Collections.Generic;

public partial class Chest : StaticBody3D, ILootable, IExaminable
{
	[Export] public string RequiredKeyName = "Key";
	[Export] public bool IsOpen = false;

	// Dull Sword is a 100% guaranteed drop -- it's just always in the list,
	// no randomness involved (unlike the NPC's chance-based Key drop).
	public List<string> Items { get; set; } = new List<string> { "Dull Sword" };

	private MeshInstance3D _mesh;

	public override void _Ready()
	{
		_mesh = GetNode<MeshInstance3D>("MeshInstance3D");
	}

	public void Open()
	{
		IsOpen = true;
		var mat = new StandardMaterial3D();
		mat.AlbedoColor = new Color(1f, 0.84f, 0f); // gold, just so open state is visually obvious
		_mesh.MaterialOverride = mat;
	}

	public void RemoveItem(string itemName)
	{
		Items.Remove(itemName);
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
