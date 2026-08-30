using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody3D
{
	private SpringArm3D _springArm;
	private PopupMenu _contextMenu;
	private List<Action> _contextMenuActions = new();
	private InventoryUI _inventoryUI;

	private Inventory _inventory;
	private Equipment _equipment;
	private PlayerStats _stats;

	public const float Speed = 5.0f;
	private const float InteractRange = 3.0f;

	[Export] public float MouseSensitivity = 0.15f;
	[Export] public float ZoomSpeed = 0.5f;
	[Export] public float MinZoom = 2.0f;
	[Export] public float MaxZoom = 12.0f;
	[Export] public float MinPitch = -60f;
	[Export] public float MaxPitch = -5f;

	[Export] public PackedScene ClickIndicatorScene;

	[Export] public double AttackIntervalSeconds = 1.2;

	private static readonly Color WalkIndicatorColor = new Color(1f, 0.85f, 0f);
	private static readonly Color InteractIndicatorColor = new Color(0.85f, 0.1f, 0.1f);

	private float _cameraYaw = 0f;
	private float _cameraPitch = -20f;

	private Vector3? _moveTarget = null;
	private const float ArrivalDistance = 0.2f;

	private const double StuckCheckInterval = 0.3;
	private const float StuckDistanceThreshold = 0.05f;
	private double _stuckCheckTimer = 0;
	private Vector3 _stuckCheckPosition;

	private Action _pendingAction = null;
	private Vector3 _pendingActionPosition;
	private float _pendingActionRange;

	private Npc _attackTarget = null;
	private double _attackCooldown = 0;

	public override void _Ready()
	{
		_springArm = GetNode<SpringArm3D>("SpringArm3D");
		_contextMenu = GetNode<PopupMenu>("/root/World/LootMenu");
		_contextMenu.IndexPressed += OnContextMenuIndexPressed;
		_inventoryUI = GetNode<InventoryUI>("/root/World/InventoryUI");

		// This character's own nodes, now nested under Player instead of
		// World. Registering with PartyManager is what lets UI and world
		// objects (KeyPickup, etc.) reach "whichever character is active"
		// without hardcoding a path to this specific Player node.
		_inventory = GetNode<Inventory>("PlayerInventory");
		_equipment = GetNode<Equipment>("PlayerEquipment");
		_stats = GetNode<PlayerStats>("PlayerStats");

		PartyManager.Instance.RegisterActiveCharacter(this, _inventory, _equipment, _stats);
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

	private void ProcessCombat(double delta)
	{
		if (_attackTarget == null) return;

		if (!IsInstanceValid(_attackTarget) || !_attackTarget.Visible)
		{
			_attackTarget = null;
			return;
		}

		float distance = GlobalPosition.DistanceTo(_attackTarget.GlobalPosition);

		if (distance > InteractRange)
		{
			_moveTarget = _attackTarget.GlobalPosition;
			return;
		}

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
		_attackCooldown = 0;
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
			WalkToAndPickUpKey(keyPickup);
		}
		else if (isRightClick && collider is KeyPickup keyPickupMenu)
		{
			OpenKeyPickupContextMenu(keyPickupMenu, mousePos);
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
			OpenLootContextMenu(lootPile2, mousePos);
		}
		else if (!isRightClick && collider is Npc npc)
		{
			SpawnClickIndicator(hitPosition, InteractIndicatorColor);
			StartAttacking(npc);
		}
		else if (isRightClick && collider is Npc npcMenu)
		{
			OpenNpcContextMenu(npcMenu, mousePos);
		}
		else if (collider is Chest chest)
		{
			HandleChestClick(chest, isRightClick, mousePos, hitPosition);
		}
		else if (!isRightClick)
		{
			SpawnClickIndicator(hitPosition, WalkIndicatorColor);
			_moveTarget = hitPosition;
			ResetStuckCheck();
		}
	}

	private void HandleChestClick(Chest chest, bool isRightClick, Vector2 mousePos, Vector3 hitPosition)
	{
		if (!chest.IsOpen)
		{
			if (isRightClick)
			{
				OpenChestClosedContextMenu(chest, mousePos, hitPosition);
			}
			else
			{
				SpawnClickIndicator(hitPosition, InteractIndicatorColor);
				GD.Print("I could loot this chest with the right key!");
			}
			return;
		}

		if (isRightClick)
		{
			OpenChestOpenContextMenu(chest, mousePos);
			return;
		}

		if (chest.Items.Count == 0)
		{
			SpawnClickIndicator(hitPosition, InteractIndicatorColor);
			GD.Print("There's nothing left to loot here.");
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

	private void WalkToAndPickUpKey(KeyPickup keyPickup)
	{
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

	// Grabs ALL matching entries for a STACKABLE item in one click (e.g.
	// all 3 "Coins" entries at once, merged into one inventory stack). For
	// a NON-stackable item, only grabs the single top instance -- clicking
	// a pile with 2 Bones picks up just 1 Bone per click, each landing in
	// its own inventory slot, matching how non-stackable items never merge.
	private void GrabItem(ILootable lootable, string itemName)
	{
		if (ItemDatabase.IsStackable(itemName))
		{
			int count = 0;
			foreach (string item in lootable.Items)
			{
				if (item == itemName) count++;
			}

			bool added = _inventory.AddItem(itemName, count);
			if (!added) return;

			GD.Print("Looted: " + ItemDatabase.GetDisplayText(itemName, count));

			for (int i = 0; i < count; i++)
			{
				lootable.RemoveItem(itemName);
			}
		}
		else
		{
			bool added = _inventory.AddItem(itemName, 1);
			if (!added) return;

			GD.Print("Looted: " + ItemDatabase.GetSingleInstanceName(itemName));
			lootable.RemoveItem(itemName);
		}
	}

	private bool IsKeyItem(string itemName)
	{
		return itemName != null && itemName.StartsWith("Key");
	}

	private void PrintDefaultUseMessage()
	{
		GD.Print("Nothing noteworthy happened");
	}

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
					_inventory.RemoveItemAt(itemIndex);
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

	// --- Generic right-click context menu helpers ---

	private void AddContextMenuOption(string label, Action action)
	{
		_contextMenu.AddItem(label);
		_contextMenuActions.Add(action);
	}

	private void OpenContextMenu(Vector2 mousePos)
	{
		_contextMenu.Position = (Vector2I)mousePos;
		_contextMenu.Popup();
	}

	private void OnContextMenuIndexPressed(long index)
	{
		if (index < 0 || index >= _contextMenuActions.Count) return;
		var action = _contextMenuActions[(int)index];
		_contextMenuActions.Clear();
		action?.Invoke();
	}

	// STACKABLE items (Coins) get ONE row per name, combined into a count
	// ("3 Gold Coins"). NON-stackable items (Bones, etc.) get ONE row PER
	// INSTANCE -- two Bones from two separate kills show as two distinct
	// "Bone" rows, not merged into a single counted line. Adds "Examine"
	// last if the lootable implements IExaminable.
	private void OpenLootContextMenu(ILootable lootable, Vector2 mousePos)
	{
		_contextMenu.Clear();
		_contextMenuActions.Clear();

		var seenStackable = new HashSet<string>();

		foreach (string item in lootable.Items)
		{
			if (ItemDatabase.IsStackable(item))
			{
				if (seenStackable.Contains(item)) continue;
				seenStackable.Add(item);

				int count = 0;
				foreach (string i in lootable.Items)
				{
					if (i == item) count++;
				}

				string itemName = item;
				AddContextMenuOption(ItemDatabase.GetDisplayText(item, count), () => LootStackableItem(lootable, itemName));
			}
			else
			{
				string itemName = item;
				AddContextMenuOption(ItemDatabase.GetSingleInstanceName(item), () => LootSingleItem(lootable, itemName));
			}
		}

		if (lootable is IExaminable examinable)
		{
			AddContextMenuOption("Examine", () => GD.Print(examinable.GetExamineText()));
		}

		OpenContextMenu(mousePos);
	}

	// Loots every remaining instance of a stackable item in one action.
	private void LootStackableItem(ILootable lootable, string itemName)
	{
		int count = 0;
		foreach (string item in lootable.Items)
		{
			if (item == itemName) count++;
		}

		bool added = _inventory.AddItem(itemName, count);
		if (!added) return;

		GD.Print("Looted: " + ItemDatabase.GetDisplayText(itemName, count));

		for (int i = 0; i < count; i++)
		{
			lootable.RemoveItem(itemName);
		}
	}

	// Loots exactly ONE instance of a non-stackable item -- used when the
	// player clicks one specific "Bone" row out of possibly several.
	private void LootSingleItem(ILootable lootable, string itemName)
	{
		bool added = _inventory.AddItem(itemName, 1);
		if (!added) return;

		GD.Print("Looted: " + ItemDatabase.GetSingleInstanceName(itemName));
		lootable.RemoveItem(itemName);
	}

	private void OpenChestClosedContextMenu(Chest chest, Vector2 mousePos, Vector3 hitPosition)
	{
		_contextMenu.Clear();
		_contextMenuActions.Clear();

		AddContextMenuOption("Open", () => TryOpenChestWithInventoryKey(chest, hitPosition));
		AddContextMenuOption("Examine", () => GD.Print(chest.GetExamineText()));

		OpenContextMenu(mousePos);
	}

	private void OpenChestOpenContextMenu(Chest chest, Vector2 mousePos)
	{
		if (chest.Items.Count > 0)
		{
			OpenLootContextMenu(chest, mousePos);
			return;
		}

		_contextMenu.Clear();
		_contextMenuActions.Clear();
		AddContextMenuOption("Examine", () => GD.Print(chest.GetExamineText()));
		OpenContextMenu(mousePos);
	}

	private void TryOpenChestWithInventoryKey(Chest chest, Vector3 hitPosition)
	{
		SpawnClickIndicator(hitPosition, InteractIndicatorColor);

		int keySlot = _inventory.FindSlotIndex(chest.RequiredKeyName);

		if (keySlot < 0)
		{
			GD.Print("I could loot this chest with the right key!");
			return;
		}

		Vector3 targetPos = chest.GlobalPosition;
		_moveTarget = targetPos;
		_pendingActionPosition = targetPos;
		_pendingActionRange = InteractRange;
		_pendingAction = () =>
		{
			if (!IsInstanceValid(chest) || chest.IsOpen) return;

			int currentKeySlot = _inventory.FindSlotIndex(chest.RequiredKeyName);
			if (currentKeySlot < 0)
			{
				GD.Print("I could loot this chest with the right key!");
				return;
			}

			chest.Open();
			_inventory.RemoveItemAt(currentKeySlot);
			GD.Print("The chest opened and magically consumed the key!");
		};
		ResetStuckCheck();
	}

	private void OpenKeyPickupContextMenu(KeyPickup keyPickup, Vector2 mousePos)
	{
		_contextMenu.Clear();
		_contextMenuActions.Clear();

		AddContextMenuOption("Take", () => WalkToAndPickUpKey(keyPickup));
		AddContextMenuOption("Examine", () => GD.Print(keyPickup.GetExamineText()));

		OpenContextMenu(mousePos);
	}

	private void OpenNpcContextMenu(Npc npc, Vector2 mousePos)
	{
		_contextMenu.Clear();
		_contextMenuActions.Clear();

		AddContextMenuOption("Attack", () => StartAttacking(npc));
		AddContextMenuOption("Examine", () => GD.Print(npc.GetExamineText()));

		OpenContextMenu(mousePos);
	}
}
