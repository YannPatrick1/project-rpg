using Godot;

public partial class KeyPickup : Area3D
{
	public void PickUp()
	{
		var inventory = GetNode<Inventory>("/root/World/PlayerInventory");
		inventory.AddItem("Key");
		GD.Print("Added Key to slot 0");
		QueueFree();
	}
}
