using Godot;
using System.Collections.Generic;

public partial class LootPile : Area3D, ILootable
{
	public List<string> Items { get; set; } = new List<string>();

	public override void _Ready()
	{
		AddToGroup("loot_piles");
	}

	public void RemoveItem(string itemName)
	{
		Items.Remove(itemName);

		if (Items.Count == 0)
		{
			QueueFree();
		}
	}
}
