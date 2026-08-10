// Shared across NPCs and equipment. Kept in its own file so both can
// reference the same enum without duplicating it (see project lessons
// learned: shared enums belong in their own file).
public enum ElementType
{
	None,
	Fire,
	Water,
	Earth,
	Air
}
