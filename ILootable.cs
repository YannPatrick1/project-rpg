using System.Collections.Generic;

public interface ILootable
{
	List<string> Items { get; }
	void RemoveItem(string itemName);
}
