using Godot;

// A Resource listing which NPC scenes can appear in a battle. Drag one
// or more Npc-scene PackedScenes into PossibleEnemies in the Inspector.
// The same scene can appear more than once in a single battle -- each
// spawn slot picks independently and with replacement.
[GlobalClass]
public partial class EncounterTable : Resource
{
	[Export] public Godot.Collections.Array<PackedScene> PossibleEnemies = new();
}
