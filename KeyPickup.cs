using Godot;

public partial class KeyPickup : Area3D
{
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player)
		{
			var inventory = GetNodeOrNull<Inventory>("/root/World/PlayerInventory");
			if (inventory != null)
			{
				inventory.AddItem("Key");
			}
			QueueFree();
		}
	}
}
