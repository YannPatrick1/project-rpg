using Godot;
using System.Collections.Generic;

public partial class Chest : StaticBody3D, ILootable
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
}
