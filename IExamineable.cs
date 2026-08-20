// Anything the player can right-click and choose "Examine" on. Returns the
// flavor text shown when examined. Kept in its own file per project
// convention for shared interfaces (see ILootable.cs).
public interface IExaminable
{
	string GetExamineText();
}
