using Godot;
using System.Collections.Generic;

public partial class Npc : CharacterBody3D
{
	[Export]
	public int MaxHealth = 3;
	[Export]
	public PackedScene KeyPickupScene;
	[Export]
	public PackedScene LootPileScene;
	private int _currentHealth;

	public override void _Ready()
	{
		_currentHealth = MaxHealth;
	}

	public void TakeDamage(int amount)
	{
		_currentHealth -= amount;
		GD.Print("NPC took damage, health is now " + _currentHealth);
		if (_currentHealth <= 0)
		{
			Die();
		}
	}

	private void Die()
	{
	GD.Print("NPC died");

	if (LootPileScene != null)
	{
		var lootPile = LootPileScene.Instantiate<LootPile>();
		lootPile.Items = new List<string> { "Key", "Bones" };
		GetParent().AddChild(lootPile);
		lootPile.GlobalPosition = GlobalPosition;      // <-- now set AFTER it's in the tree
	}

	QueueFree();
	}
}
