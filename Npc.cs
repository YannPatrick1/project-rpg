using Godot;

public partial class Npc : CharacterBody3D
{
	[Export]
	public int MaxHealth = 3;

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
		QueueFree();
	}
}
