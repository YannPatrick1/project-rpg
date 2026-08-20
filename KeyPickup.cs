using Godot;

public partial class KeyPickup : Area3D, IExaminable
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

	public string GetExamineText()
	{
		return "A small iron key, glinting on the ground. Someone's going to miss this.";
	}
}
