using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody3D
{
	private SpringArm3D _springArm;
	private PopupMenu _lootMenu;
	private ILootable _activeLoot;
	private List<string> _lootMenuItemNames = new();
	private InventoryUI _inventoryUI;

	public const float Speed = 5.0f;
	private const float InteractRange = 3.0f;

	[Export] public float MouseSensitivity = 0.15f;
	[Export] public float ZoomSpeed = 0.5f;
	[Export] public float MinZoom = 2.0f;
	[Export] public float MaxZoom = 12.0f;
	[Export] public float MinPitch = -60f;
	[Export] public float MaxPitch = -5f;

	// Assign the click_indicator.tscn scene here in the Inspector.
	[Export] public PackedScene ClickIndicatorScene;

	// TODO: once weapons exist, replace this flat value with a per-weapon
	// attack speed lookup instead of a fixed export field.
	[Export] public double AttackIntervalSeconds = 1.2;

	private static readonly Color WalkIndicatorColor = new Color(1f, 0.85f, 0f);   // yellow
	private static readonly Color InteractIndicatorColor = new Color(0.85f, 0.1f, 0.1f); // red

	private float _cameraYaw = 0f;
	private float _cameraPitch = -20f;

	// --- Click-to-move state ---
	private Vector3? _moveTarget = null;
	private const float ArrivalDistance = 0.2f;

	// Stuck detection: periodically check how far we've actually moved.
	// If it's basically nothing (walked into a wall/obstacle), we cancel
	// the move target as if we'd arrived, rather than staying stuck forever.
	private const double StuckCheckInterval = 0.3;
	private const float StuckDistanceThreshold = 0.05f;
	private double _stuckCheckTimer = 0;
	private Vector3 _stuckCheckPosition;

	// --- Walk-then-interact state (one-shot actions: open chest, grab loot, etc.) ---
	// Set alongside _moveTarget. Checked every physics frame while moving so
	// the action fires as soon as we're in range, not only once we'd
	// otherwise "arrive" exactly at the target's position.
	private Action _pendingAction = null;
	private Vector3 _pendingActionPosition;
	private float _pendingActionRange;

	// --- Combat state (recurring auto-attack, not a one-shot pending action) ---
	private Npc _attackTarget = null;
	private double _attackCooldown = 0;

	public override void _Ready()
	{
		_springArm = GetNode<SpringArm3D>("SpringArm3D");
		_lootMenu = GetNode<PopupMenu>("/root/World/LootMenu");
		_lootMenu.IndexPressed += OnLootMenuIndexPressed;
		_inventoryUI = GetNode<InventoryUI>("/root/World/InventoryUI");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		ProcessCombat(delta);

		if (_moveTarget.HasValue)
		{
			if (_pendingAction != null)
			{
				float actionDistance = GlobalPosition.DistanceTo(_pendingActionPosition);
				if (actionDistance <= _pendingActionRange)
				{
					velocity.X = 0;
					velocity.Z = 0;
					Velocity = velocity;
					MoveAndSlide();

					var action = _pendingAction;
					_pendingAction = null;
					_moveTarget = null;

					action.Invoke();

					UpdateCameraRotation();
					return;
				}
			}

			Vector3 toTarget = _moveTarget.Value - GlobalPosition;
			toTarget.Y = 0;
			float distance = toTarget.Length();

			if (distance < ArrivalDistance)
			{
				// Arrived.
				velocity.X = 0;
				velocity.Z = 0;
				_moveTarget = null;
				_pendingAction = null;
			}
			else
			{
				Vector3 moveDir = toTarget.Normalized();
				velocity.X = moveDir.X * Speed;
				velocity.Z = moveDir.Z * Speed;
				LookAt(GlobalPosition + moveDir, Vector3.Up);

				CheckIfStuck(delta);
			}
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();

		UpdateCameraRotation();
	}

	private void UpdateCameraRotation()
	{
		_springArm.Rotation = new Vector3(Mathf.DegToRad(_cameraPitch), Mathf.DegToRad(_cameraYaw) - Rotation.Y, 0);
	}

	// Handles walking toward an attack target and auto-attacking on a timer
	// once in range. Runs every physics frame whenever _attackTarget is set.
	private void ProcessCombat(double delta)
	{
		if (_attackTarget == null) return;

		if (!IsInstanceValid(_attackTarget) || !_attackTarget.Visible)
		{
			// Target died (Npc.Die() hides it) or was otherwise removed.
			_attackTarget = null;
			return;
		}

		float distance = GlobalPosition.DistanceTo(_attackTarget.GlobalPosition);

		if (distance > InteractRange)
		{
			// Not in range yet (or anymore) — keep walking toward it.
			_moveTarget = _attackTarget.GlobalPosition;
			return;
		}

		// In range: stop moving, face the target, tick the attack timer.
		_moveTarget = null;
		Vector3 lookTarget = new Vector3(_attackTarget.GlobalPosition.X, GlobalPosition.Y, _attackTarget.GlobalPosition.Z);
		if (lookTarget != GlobalPosition)
		{
			LookAt(lookTarget, Vector3.Up);
		}

		_attackCooldown -= delta;
		if (_attackCooldown <= 0)
		{
			_attackTarget.TakeDamage(1);
			_attackCooldown = AttackIntervalSeconds;
		}
	}

	// Called every physics frame while walking toward a click-move target.
	// Every StuckCheckInterval seconds, compares current position to where
	// we were at the last check. If we've barely moved, something's
	// blocking us (wall, chest, etc.) — cancel the target instead of
	// grinding against it forever.
	private void CheckIfStuck(double delta)
	{
		_stuckCheckTimer += delta;

		if (_stuckCheckTimer >= StuckCheckInterval)
		{
			float movedDistance = GlobalPosition.DistanceTo(_stuckCheckPosition);

			if (movedDistance < StuckDistanceThreshold)
			{
				GD.Print("Stuck on something — stopping here.");
				_moveTarget = null;
				_pendingAction = null;
			}

			_stuckCheckPosition = GlobalPosition;
			_stuckCheckTimer = 0;
		}
	}

	private void ResetStuckCheck()
	{
		_stuckCheckPosition = GlobalPosition;
		_stuckCheckTimer = 0;
	}

	private void SpawnClickIndicator(Vector3 worldPosition, Color color)
	{
		if (ClickIndicatorScene == null) return;

		var indicator = ClickIndicatorScene.Instantiate<ClickIndicator>();
		GetParent().AddChild(indicator);
		indicator.Play(worldPosition, color);
	}

	private void StartAttacking(Npc npc)
	{
		_pendingAction = null;
		_attackTarget = npc;
		_attackCooldown = 0; // attack immediately once in range
		_moveTarget = npc.GlobalPosition;
		ResetStuckCheck();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion && Input.IsMouseButtonPressed(MouseButton.Middle))
		{
			_cameraYaw -= mouseMotion.Relative.X * MouseSensitivity;
			_cameraPitch = Mathf.Clamp(_cameraPitch - mouseMotion.Relative.Y * MouseSensitivity, MinPitch, MaxPitch);
		}

		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
		{
			if (mouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				_springArm.SpringLength = Mathf.Clamp(_springArm.SpringLength - ZoomSpeed, MinZoom, MaxZoom);
			}
			else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				_springArm.SpringLength = Mathf.Clamp(_springArm.SpringLength + ZoomSpeed, MinZoom, MaxZoom);
			}
			else if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				HandleClick(mouseButton.Position, false);
			}
			else if (mouseButton.ButtonIndex == MouseButton.Right)
			{
				HandleClick(mouseButton.Position, true);
			}
		}
	}

	private void HandleClick(Vector2 mousePos, bool isRightClick)
	{
		// Any new click cancels whatever the player was previously doing
		// (auto-attacking, walking to loot, etc.) — the branches below set
		// these back if the new click starts a new one of these.
		_attackTarget = null;
		_pendingAction = null;

		string selectedItem = _inventoryUI.GetSelectedItem();
		int selectedIndex = _inventoryUI.GetSelectedIndex();

		var camera = _springArm.GetNode<Camera3D>("Camera3D");
		Vector3 rayOrigin = camera.ProjectRayOrigin(mousePos);
		Vector3 rayDirection = camera.ProjectRayNormal(mousePos);
		Vector3 rayEnd = rayOrigin + rayDirection * 1000f;

		var spaceState = GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
		query.CollideWithAreas = true;
		query.CollideWithBodies = true;

		var result = spaceState.IntersectRay(query);

		if (result.Count == 0)
		{
			GD.Print("Clicked on nothing.");
			if (selectedItem != null)
			{
				PrintDefaultUseMessage();
				_inventoryUI.ClearSelection();
			}
			return;
		}

		var collider = result["collider"].AsGodotObject();
		Vector3 hitPosition = (Vector3)result["position"];
		GD.Print("Clicked on: ", collider, " | Right click: ", isRightClick);

		if (selectedItem != null)
		{
			SpawnClickIndicator(hitPosition, InteractIndicatorColor);
			UseItemOn(selectedItem, selectedIndex, collider);
			_inventoryUI.ClearSelection();
			return;
		}

		if (!isRightClick && collider is KeyPickup keyPickup)
		{
			SpawnClickIndicator(hitPosition, InteractIndicatorColor);

			Vector3 targetPos = keyPickup.GlobalPosition;
			_moveTarget = targetPos;
			_pendingActionPosition = targetPos;
			_pendingActionRange = InteractRange;
			_pendingAction = () =>
			{
				if (!IsInstanceValid(keyPickup)) return;
				keyPickup.PickUp();
			};
			ResetStuckCheck();
		}
		else if (!isRightClick && collider is LootPile lootPile && lootPile.Items.Count > 0)
		{
			SpawnClickIndicator(hitPosition, InteractIndicatorColor);

			Vector3 targetPos = lootPile.GlobalPosition;
			_moveTarget = targetPos;
			_pendingActionPosition = targetPos;
			_pendingActionRange = InteractRange;
			_pendingAction = () =>
			{
				if (!IsInstanceValid(lootPile) || lootPile.Items.Count == 0) return;
				string topItem = lootPile.Items[0];
				GrabItem(lootPile, topItem);
			};
			ResetStuckCheck();
		}
		else if (isRightClick && collider is LootPile lootPile2)
		{
			// Right-click context menu opens immediately, no walking first —
			// matches OSRS right-click-menu convention.
			OpenLootMenu(lootPile2, mousePos);
		}
		else if (!isRightClick && collider is Npc npc)
		{
			SpawnClickIndicator(hitPosition, InteractIndicatorColor);
			StartAttacking(npc);
		}
		else if (collider is Chest chest)
		{
			HandleChestClick(chest, isRightClick, mousePos, hitPosition);
		}
		else if (!isRightClick)
		{
			// Nothing interactive was clicked — treat it as a plain move command.
			SpawnClickIndicator(hitPosition, WalkIndicatorColor);
			_moveTarget = hitPosition;
			ResetStuckCheck();
		}
	}

	private void HandleChestClick(Chest chest, bool isRightClick, Vector2 mousePos, Vector3 hitPosition)
	{
		if (!chest.IsOpen)
		{
			if (!isRightClick)
			{
				SpawnClickIndicator(hitPosition, InteractIndicatorColor);
				GD.Print("I could loot this chest with the right key!");
			}
			return;
		}

		if (chest.Items.Count == 0) return;

		if (isRightClick)
		{
			// Right-click context menu opens immediately, no walking first.
			OpenLootMenu(chest, mousePos);
			return;
		}

		SpawnClickIndicator(hitPosition, InteractIndicatorColor);

		Vector3 targetPos = chest.GlobalPosition;
		_moveTarget = targetPos;
		_pendingActionPosition = targetPos;
		_pendingActionRange = InteractRange;
		_pendingAction = () =>
		{
			if (!IsInstanceValid(chest) || chest.Items.Count == 0) return;
			string topItem = chest.Items[0];
			GrabItem(chest, topItem);
		};
		ResetStuckCheck();
	}

	// Grabs ALL matching entries of a stacked item in one click (e.g. all 3
	// "Coins" entries at once), or just the single entry for non-stackable items.
	private void GrabItem(ILootable lootable, string itemName)
	{
		int count = 0;
		foreach (string item in lootable.Items)
		{
			if (item == itemName) count++;
		}

		var inventory = GetNode<Inventory>("/root/World/PlayerInventory");
		bool added = inventory.AddItem(itemName, count);

		if (!added)
		{
			// Inventory full and this isn't an existing stack — leave it where it is.
			return;
		}

		GD.Print("Looted: " + ItemDatabase.GetDisplayText(itemName, count));

		for (int i = 0; i < count; i++)
		{
			lootable.RemoveItem(itemName);
		}
	}

	// Any item name starting with "Key" counts as a key-type item.
	// Future keys (Key2, Key3, ...) are automatically covered by this check.
	private bool IsKeyItem(string itemName)
	{
		return itemName != null && itemName.StartsWith("Key");
	}

	// Single source of truth for the generic "used something on something
	// it doesn't interact with" message. Change the wording here only.
	private void PrintDefaultUseMessage()
	{
		GD.Print("Nothing noteworthy happened");
	}

	// Walks the player to the target first; the actual key/chest logic only
	// runs once within InteractRange (see the pending-action check in
	// _PhysicsProcess).
	private void UseItemOn(string itemName, int itemIndex, GodotObject target)
	{
		if (target is Chest chest)
		{
			Vector3 targetPos = chest.GlobalPosition;
			_moveTarget = targetPos;
			_pendingActionPosition = targetPos;
			_pendingActionRange = InteractRange;
			_pendingAction = () =>
			{
				if (!IsInstanceValid(chest)) return;

				if (chest.IsOpen)
				{
					PrintDefaultUseMessage();
					return;
				}

				if (chest.RequiredKeyName == itemName)
				{
					chest.Open();
					var inventory = GetNode<Inventory>("/root/World/PlayerInventory");
					inventory.RemoveItemAt(itemIndex);
					GD.Print("The chest opened and magically consumed the key!");
				}
				else if (IsKeyItem(itemName))
				{
					GD.Print("This key doesn't work here");
				}
				else
				{
					PrintDefaultUseMessage();
				}
			};
			ResetStuckCheck();
		}
		else
		{
			PrintDefaultUseMessage();
		}
	}

	// Groups duplicate entries (e.g. three "Coins") into a single menu row
	// with a combined display like "3 Gold Coins", instead of listing each
	// one separately.
	private void OpenLootMenu(ILootable lootable, Vector2 mousePos)
	{
		_activeLoot = lootable;
		_lootMenu.Clear();
		_lootMenuItemNames.Clear();

		var seen = new HashSet<string>();
		foreach (string item in lootable.Items)
		{
			if (seen.Contains(item)) continue;
			seen.Add(item);

			int count = 0;
			foreach (string i in lootable.Items)
			{
				if (i == item) count++;
			}

			_lootMenu.AddItem(ItemDatabase.GetDisplayText(item, count));
			_lootMenuItemNames.Add(item);
		}

		_lootMenu.Position = (Vector2I)mousePos;
		_lootMenu.Popup();
	}

	private void OnLootMenuIndexPressed(long index)
	{
		if (_activeLoot == null) return;

		string itemName = _lootMenuItemNames[(int)index];

		int count = 0;
		foreach (string item in _activeLoot.Items)
		{
			if (item == itemName) count++;
		}

		var inventory = GetNode<Inventory>("/root/World/PlayerInventory");
		bool added = inventory.AddItem(itemName, count);

		if (!added)
		{
			_activeLoot = null;
			return;
		}

		GD.Print("Looted: " + ItemDatabase.GetDisplayText(itemName, count));

		for (int i = 0; i < count; i++)
		{
			_activeLoot.RemoveItem(itemName);
		}

		_activeLoot = null;
	}
}
