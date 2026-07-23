using Godot;
using System.Collections.Generic;

public partial class LootPile : Area3D
{
	public List<string> Items = new List<string>();

	public void RemoveItem(string itemName)
	{
		Items.Remove(itemName);

		if (Items.Count == 0)
		{
			QueueFree();
		}
	}
}
