using Godot;

public partial class KeyPickup : Area3D
{
	public void PickUp()
	{
		var inventory = GetNode<Inventory>("/root/World/PlayerInventory");
		bool added = inventory.AddItem("Key");

		if (!added)
		{
			// Inventory full — leave the key on the ground.
			return;
		}

		GD.Print("Added Key to slot 0");
		QueueFree();
	}
}
