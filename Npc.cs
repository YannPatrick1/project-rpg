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
	private bool _isDead = false;
	private Vector3 _spawnPosition;
	private Timer _respawnTimer;
	private CollisionShape3D _collisionShape;
	private MeshInstance3D _meshInstance;

	public override void _Ready()
	{
		_currentHealth = MaxHealth;
		_spawnPosition = GlobalPosition;

		_respawnTimer = GetNode<Timer>("RespawnTimer");
		_respawnTimer.Timeout += OnRespawnTimerTimeout;

		_collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
		_meshInstance = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
	}

	public void TakeDamage(int amount)
	{
		if (_isDead) return;
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
		_isDead = true;

		if (LootPileScene != null)
		{
			var newItems = new List<string>();
			newItems.Add("Bones");

			if (GD.Randf() < 0.20f)
			{
				newItems.Add("Key");
			}

			int coinAmount = GD.RandRange(1, 3);
			for (int i = 0; i < coinAmount; i++)
			{
				newItems.Add("Coins");
			}

			LootPile existingPile = FindNearbyLootPile(GlobalPosition, 1.5f);

			if (existingPile != null)
			{
				foreach (string item in newItems)
				{
					existingPile.Items.Add(item);
				}
				GD.Print("Merged new loot into existing pile");
			}
			else
			{
				var lootPile = LootPileScene.Instantiate<LootPile>();
				lootPile.Items = newItems;
				GetParent().AddChild(lootPile);
				lootPile.GlobalPosition = GlobalPosition;
			}
		}

		Visible = false;
		_collisionShape.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
		_respawnTimer.Start();
	}

	// Looks for an existing loot pile within "radius" units of "position".
	// Used so loot from a new death merges into a pile that's already there
	// instead of spawning an overlapping duplicate.
	private LootPile FindNearbyLootPile(Vector3 position, float radius)
	{
		foreach (Node node in GetTree().GetNodesInGroup("loot_piles"))
		{
			if (node is LootPile pile && pile.GlobalPosition.DistanceTo(position) <= radius)
			{
				return pile;
			}
		}
		return null;
	}

	private void OnRespawnTimerTimeout()
	{
		GD.Print("NPC respawned");
		_currentHealth = MaxHealth;
		_isDead = false;
		GlobalPosition = _spawnPosition;
		Visible = true;
		_collisionShape.SetDeferred(CollisionShape3D.PropertyName.Disabled, false);
	}
}
