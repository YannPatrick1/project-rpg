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
			var lootPile = LootPileScene.Instantiate<LootPile>();
			lootPile.Items = new List<string>();

			// Bones always drop.
			lootPile.Items.Add("Bones");

			// Key: 20% chance.
			if (GD.Randf() < 0.20f)
			{
				lootPile.Items.Add("Key");
			}

			// Coins: 100% chance, even 1/3 chance each of 1, 2, or 3.
			int coinAmount = GD.RandRange(1, 3);
			for (int i = 0; i < coinAmount; i++)
			{
				lootPile.Items.Add("Coins");
			}

			GetParent().AddChild(lootPile);
			lootPile.GlobalPosition = GlobalPosition;
		}

		// Instead of QueueFree(), "hide and disable" so we can respawn later.
		Visible = false;
		_collisionShape.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
		_respawnTimer.Start();
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
