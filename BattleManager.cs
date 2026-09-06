using Godot;
using System.Collections.Generic;
using System.Linq;

// Autoload singleton. Rolls for random encounters as the active party
// moves, spawns enemies for an instanced battle, freezes the rest of
// the game world for its duration via GetTree().Paused, and cleans up
// once every spawned enemy is dead.
public partial class BattleManager : Node
{
	// World units the active character has to move to count as one
	// "step" for encounter-roll purposes.
	[Export] public float StepDistance = 1.0f;

	// Chance (0-1) a battle triggers on any given step.
	[Export] public float EncounterChancePerStep = 0.10f;

	// Chance (0-1) each spawn slot beyond the first spawns an enemy.
	// Slot 1 always spawns once a battle triggers at all.
	[Export] public float AdditionalEnemyChance = 0.5f;

	[Export] public int MaxEnemies = 6;

	// The pool of enemies for the current area. One global table for
	// now -- swappable per-zone later without changing this system.
	[Export] public EncounterTable EncounterTable;

	public static BattleManager Instance { get; private set; }

	private Marker3D _arenaMarker;
	private Vector3 _lastCheckedPosition;
	private bool _hasLastPosition = false;

	private bool _inBattle = false;
	private List<int> _battlePartyIndices = new();
	private Dictionary<int, Vector3> _preBattlePositions = new();
	private List<Npc> _aliveEnemies = new();
	private List<Npc> _allSpawnedEnemies = new();

	public override void _Ready()
	{
		Instance = this;
		_arenaMarker = GetNodeOrNull<Marker3D>("/root/World/BattleArenaMarker");

		if (_arenaMarker == null)
		{
			GD.PrintErr("BattleManager: no BattleArenaMarker found at /root/World/BattleArenaMarker.");
		}
	}

	public bool IsInBattle => _inBattle;

	public bool IsPartyIndexInBattle(int partyIndex)
	{
		return _battlePartyIndices.Contains(partyIndex);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_inBattle) return;
		if (_arenaMarker == null) return;
		if (PartyManager.Instance == null) return;

		var activeNode = PartyManager.Instance.GetActiveCharacter();
		if (activeNode is not Node3D active) return;

		if (!_hasLastPosition)
		{
			_lastCheckedPosition = active.GlobalPosition;
			_hasLastPosition = true;
			return;
		}

		float moved = active.GlobalPosition.DistanceTo(_lastCheckedPosition);

		// Handle covering more than one StepDistance in a single frame
		// (a big teleport, a low-framerate hiccup) by rolling once per
		// full step covered, not just once per frame regardless of
		// distance.
		while (moved >= StepDistance)
		{
			_lastCheckedPosition = _lastCheckedPosition.MoveToward(active.GlobalPosition, StepDistance);
			moved -= StepDistance;

			if (GD.Randf() < EncounterChancePerStep)
			{
				StartBattle();
				return;
			}
		}
	}

	private void StartBattle()
	{
		if (EncounterTable == null || EncounterTable.PossibleEnemies.Count == 0)
		{
			GD.Print("Encounter triggered, but no EncounterTable is assigned — skipping.");
			return;
		}

		int activeIndex = PartyManager.Instance.GetActiveIndex();

		_battlePartyIndices.Clear();
		if (PartyManager.Instance.IsReleased(activeIndex))
		{
			// Solo -- only the triggering character enters.
			_battlePartyIndices.Add(activeIndex);
		}
		else
		{
			// The whole current group (2 or 3) enters together.
			for (int i = 0; i < PartyManager.MaxPartySize; i++)
			{
				if (PartyManager.Instance.HasMember(i) && !PartyManager.Instance.IsReleased(i))
				{
					_battlePartyIndices.Add(i);
				}
			}
		}

		GD.Print("Battle starting! Party indices: " + string.Join(", ", _battlePartyIndices));

		_preBattlePositions.Clear();
		Vector3 arenaCenter = _arenaMarker.GlobalPosition;
		int slot = 0;

		foreach (int index in _battlePartyIndices)
		{
			var character = PartyManager.Instance.GetCharacter(index);
			if (character is not Node3D character3D) continue;

			// Clear any leftover move target / pending action / attack
			// target / velocity BEFORE teleporting, so nothing carries over
			// from the old-world context into the arena.
			if (character is Player player)
			{
				player.CancelMovementAndActions();
			}

			_preBattlePositions[index] = character3D.GlobalPosition;

			float angle = slot * Mathf.Pi * 2f / Mathf.Max(_battlePartyIndices.Count, 1);
			Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 1.5f;

			// Slight upward lift so the character lands cleanly on the
			// arena floor next physics frame instead of spawning flush/
			// embedded in it.
			character3D.GlobalPosition = arenaCenter + offset + Vector3.Up * 0.5f;
			slot++;

			character.ProcessMode = ProcessModeEnum.Always;
		}

		SpawnEnemies(arenaCenter);

		_inBattle = true;
		GetTree().Paused = true;
	}

	private void SpawnEnemies(Vector3 arenaCenter)
	{
		_aliveEnemies.Clear();
		_allSpawnedEnemies.Clear();

		var pool = EncounterTable.PossibleEnemies;

		int enemyCount = 1; // slot 1 is guaranteed
		for (int slot = 1; slot < MaxEnemies; slot++)
		{
			if (GD.Randf() < AdditionalEnemyChance)
			{
				enemyCount++;
			}
		}

		var worldNode = GetTree().Root.GetNode("World");

		for (int i = 0; i < enemyCount; i++)
		{
			var scene = pool[GD.RandRange(0, pool.Count - 1)];
			if (scene == null) continue;

			var npc = scene.Instantiate<Npc>();
			worldNode.AddChild(npc);

			float angle = i * Mathf.Pi * 2f / Mathf.Max(enemyCount, 1);
			Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 3.5f;
			npc.GlobalPosition = arenaCenter + offset + Vector3.Back * 3f;

			npc.ProcessMode = ProcessModeEnum.Always;
			npc.Died += () => OnEnemyDied(npc);

			_aliveEnemies.Add(npc);
			_allSpawnedEnemies.Add(npc);
		}

		GD.Print("Spawned " + enemyCount + " enem" + (enemyCount == 1 ? "y" : "ies") + " for battle.");
	}

	private void OnEnemyDied(Npc npc)
	{
		_aliveEnemies.Remove(npc);

		if (_aliveEnemies.Count == 0)
		{
			EndBattle();
		}
	}

	private void EndBattle()
	{
		GD.Print("Battle won! Returning party to the field.");

		foreach (var kvp in _preBattlePositions)
		{
			var character = PartyManager.Instance.GetCharacter(kvp.Key);
			if (character is not Node3D character3D) continue;

			// Clear any leftover move target / pending action / attack
			// target / velocity BEFORE teleporting back, so nothing carries
			// over from the arena context into the field.
			if (character is Player player)
			{
				player.CancelMovementAndActions();
			}

			character3D.GlobalPosition = kvp.Value;
			character.ProcessMode = ProcessModeEnum.Inherit;
		}

		foreach (var enemy in _allSpawnedEnemies.ToList())
		{
			if (IsInstanceValid(enemy))
			{
				enemy.QueueFree();
			}
		}

		_aliveEnemies.Clear();
		_allSpawnedEnemies.Clear();
		_battlePartyIndices.Clear();
		_preBattlePositions.Clear();

		_inBattle = false;
		_hasLastPosition = false;

		GetTree().Paused = false;
	}
}
