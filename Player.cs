using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody3D
{
	[Export] public int PartyIndex = 0;

	private SpringArm3D _springArm;
	private Camera3D _camera;
	private PopupMenu _contextMenu;
	private List<Action> _contextMenuActions = new();
	private InventoryUI _inventoryUI;

	private Inventory _inventory;
	private Equipment _equipment;
	private PlayerStats _stats;

	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;
	private const float InteractRange = 3.0f;

	[Export] public float MouseSensitivity = 0.15f;
	[Export] public float ZoomSpeed = 0.5f;
	[Export] public float MinZoom = 2.0f;
	[Export] public float MaxZoom = 12.0f;
	[Export] public float MinPitch = -60f;
	[Export] public float MaxPitch = -5f;

	[Export] public double AttackIntervalSeconds = 1.2;

	[Export] public float FollowDistance = 2.5f;
	[Export] public float FollowStopDistance = 1.2f;
	[Export] public float MaxRegroupDistance = 8f;

	private float _cameraYaw = 0f;
	private float _cameraPitch = -20f;

	private Npc _attackTarget = null;
	private double _attackCooldown = 0;

	// Dormant = hidden, non-colliding, and frozen. Set by PartyManager.
	private bool _isDormant = false;
	private uint _defaultCollisionLayer;
	private uint _defaultCollisionMask;

	public bool IsActiveCharacter =>
		PartyManager.Instance != null && PartyManager.Instance.IsActiveIndex(PartyIndex);

	public override void _Ready()
	{
		_defaultCollisionLayer = CollisionLayer;
		_defaultCollisionMask = CollisionMask;

		_springArm = GetNode<SpringArm3D>("SpringArm3D");
		_camera = _springArm.GetNode<Camera3D>("Camera3D");
		_contextMenu = GetNode<PopupMenu>("/root/World/LootMenu");
		_contextMenu.IndexPressed += OnContextMenuIndexPressed;
		_inventoryUI = GetNode<InventoryUI>("/root/World/InventoryUI");

		_inventory = GetNode<Inventory>("PlayerInventory");
		_equipment = GetNode<Equipment>("PlayerEquipment");
		_stats = GetNode<PlayerStats>("PlayerStats");

		PartyManager.Instance.RegisterPartyMember(PartyIndex, this, _inventory, _equipment, _stats);
		PartyManager.Instance.ActiveCharacterChanged += OnActiveCharacterChanged;

		// Everyone except the starting active character begins hidden.
		if (!IsActiveCharacter)
		{
			PartyManager.Instance.SetDormant(PartyIndex, true);
		}

		UpdateCameraState();
	}

	// Called by PartyManager.SetDormant. Hides the character, turns its
	// collision off (layer/mask 0 = collides with nothing and nothing
	// collides with it), and stops it from processing movement.
	public void ApplyDormant(bool dormant)
	{
		_isDormant = dormant;
		Visible = !dormant;
		CollisionLayer = dormant ? 0 : _defaultCollisionLayer;
		CollisionMask = dormant ? 0 : _defaultCollisionMask;

		if (dormant)
		{
			Velocity = Vector3.Zero;
			_attackTarget = null;
		}
	}

	private void OnActiveCharacterChanged()
	{
		UpdateCameraState();
	}

	private void UpdateCameraState()
	{
		_camera.Current = IsActiveCharacter;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDormant) return;

		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		if (!IsActiveCharacter)
		{
			if (PartyManager.Instance.IsReleased(PartyIndex))
			{
				velocity.X = 0;
				velocity.Z = 0;
			}
			else
			{
				ProcessFollow(ref velocity);
			}

			Velocity = velocity;
			MoveAndSlide();
			return;
		}

		ProcessCombat(delta);

		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 rawDirection = new Vector3(inputDir.X, 0, inputDir.Y);
		Vector3 direction = rawDirection.Rotated(Vector3.Up, Mathf.DegToRad(_cameraYaw)).Normalized();

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
			LookAt(GlobalPosition + direction, Vector3.Up);
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

	// Walks this (inactive, non-released) character toward a point
	// behind and to the side of whoever's currently active -- unless
	// that active character is itself released, in which case they've
	// left the group and there's nothing to follow.
	private void ProcessFollow(ref Vector3 velocity)
	{
		int leaderIndex = PartyManager.Instance.GetActiveIndex();

		if (PartyManager.Instance.IsReleased(leaderIndex))
		{
			velocity.X = 0;
			velocity.Z = 0;
			return;
		}

		var leader = PartyManager.Instance.GetActiveCharacter() as Node3D;

		if (leader == null || leader == this)
		{
			velocity.X = 0;
			velocity.Z = 0;
			return;
		}

		Vector3 behind = leader.GlobalTransform.Basis.Z.Normalized() * FollowDistance;
		float sideSign = (PartyIndex % 2 == 0) ? -1f : 1f;
		Vector3 side = leader.GlobalTransform.Basis.X.Normalized() * (sideSign * 1.5f);
		Vector3 followPoint = leader.GlobalPosition + behind + side;

		Vector3 toTarget = followPoint - GlobalPosition;
		toTarget.Y = 0;
		float distance = toTarget.Length();

		if (distance < FollowStopDistance)
		{
			velocity.X = 0;
			velocity.Z = 0;
		}
		else
		{
			Vector3 moveDir = toTarget.Normalized();
			velocity.X = moveDir.X * Speed;
			velocity.Z = moveDir.Z * Speed;
			LookAt(GlobalPosition + moveDir, Vector3.Up);
		}
	}

	private void ToggleReleased()
	{
		if (PartyManager.Instance == null) return;

		if (PartyManager.Instance.IsReleased(PartyIndex))
		{
			PartyManager.Instance.Rejoin(PartyIndex, MaxRegroupDistance);
		}
		else
		{
			PartyManager.Instance.Release(PartyIndex);
		}
	}

	private void UpdateCameraRotation()
	{
		_springArm.Rotation = new Vector3(Mathf.DegToRad(_cameraPitch), Mathf.DegToRad(_cameraYaw) - Rotation.Y, 0);
	}

	// With direct control there's no auto-walk: click an NPC to target it,
	// and the auto-attack fires on its cooldown whenever you're in range.
	private void ProcessCombat(double delta)
	{
		if (_attackTarget == null) return;

		if (!IsInstanceValid(_attackTarget) || !_attackTarget.Visible)
		{
			_attackTarget = null;
			return;
		}

		if (!IsInRange(_attackTarget)) return;

		_attackCooldown -= delta;
		if (_attackCooldown <= 0)
		{
			_attackTarget.TakeDamage(1);
			_attackCooldown = AttackIntervalSeconds;
		}
	}

	private bool IsInRange(Node3D target)
	{
		return target != null && GlobalPosition.DistanceTo(target.GlobalPosition) <= InteractRange;
	}

	private bool CheckReach(Node3D target, string verb = "pick that up")
	{
		if (IsInRange(target)) return true;
		GD.Print("Too far away to " + verb + ".");
		return false;
	}

	private void StartAttacking(Npc npc)
	{
		_attackTarget = npc;
		_attackCooldown = 0;

		if (!IsInRange(npc))
		{
			GD.Print("Get closer to attack.");
		}
	}

	// Called by BattleManager right before teleporting this character into
	// or out of an arena, so no leftover attack state carries over
	// across the teleport.
	public void CancelMovementAndActions()
	{
		_attackTarget = null;
		Velocity = Vector3.Zero;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsActiveCharacter) return;

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.R)
		{
			ToggleReleased();
		}

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
		GD.Print("Clicked on: ", collider, " | Right click: ", isRightClick);

		if (selectedItem != null)
		{
			UseItemOn(selectedItem, selectedIndex, collider);
			_inventoryUI.ClearSelection();
			return;
		}

		if (!isRightClick && collider is KeyPickup keyPickup)
		{
			TryPickUpKey(keyPickup);
		}
		else if (isRightClick && collider is KeyPickup keyPickupMenu)
		{
			OpenKeyPickupContextMenu(keyPickupMenu, mousePos);
		}
		else if (!isRightClick && collider is LootPile lootPile && lootPile.Items.Count > 0)
		{
			GrabItem(lootPile, lootPile.Items[0]);
		}
		else if (isRightClick && collider is LootPile lootPile2)
		{
			OpenLootContextMenu(lootPile2, mousePos);
		}
		else if (!isRightClick && collider is Npc npc)
		{
			StartAttacking(npc);
		}
		else if (isRightClick && collider is Npc npcMenu)
		{
			OpenNpcContextMenu(npcMenu, mousePos);
		}
		else if (collider is Chest chest)
		{
			HandleChestClick(chest, isRightClick, mousePos);
		}
	}

	private void HandleChestClick(Chest chest, bool isRightClick, Vector2 mousePos)
	{
		if (!chest.IsOpen)
		{
			if (isRightClick)
			{
				OpenChestClosedContextMenu(chest, mousePos);
			}
			else
			{
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
			GD.Print("There's nothing left to loot here.");
			return;
		}

		GrabItem(chest, chest.Items[0]);
	}

	private void TryPickUpKey(KeyPickup keyPickup)
	{
		if (!IsInstanceValid(keyPickup)) return;
		if (!CheckReach(keyPickup)) return;
		keyPickup.PickUp();
	}

	private void GrabItem(ILootable lootable, string itemName)
	{
		if (!CheckReach(lootable as Node3D)) return;

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
			if (!CheckReach(chest, "use that here")) return;

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
		}
		else
		{
			PrintDefaultUseMessage();
		}
	}

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

	private void LootStackableItem(ILootable lootable, string itemName)
	{
		if (!CheckReach(lootable as Node3D)) return;

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

	private void LootSingleItem(ILootable lootable, string itemName)
	{
		if (!CheckReach(lootable as Node3D)) return;

		bool added = _inventory.AddItem(itemName, 1);
		if (!added) return;

		GD.Print("Looted: " + ItemDatabase.GetSingleInstanceName(itemName));
		lootable.RemoveItem(itemName);
	}

	private void OpenChestClosedContextMenu(Chest chest, Vector2 mousePos)
	{
		_contextMenu.Clear();
		_contextMenuActions.Clear();

		AddContextMenuOption("Open", () => TryOpenChestWithInventoryKey(chest));
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

	private void TryOpenChestWithInventoryKey(Chest chest)
	{
		if (!IsInstanceValid(chest) || chest.IsOpen) return;
		if (!CheckReach(chest, "open that")) return;

		int keySlot = _inventory.FindSlotIndex(chest.RequiredKeyName);

		if (keySlot < 0)
		{
			GD.Print("I could loot this chest with the right key!");
			return;
		}

		chest.Open();
		_inventory.RemoveItemAt(keySlot);
		GD.Print("The chest opened and magically consumed the key!");
	}

	private void OpenKeyPickupContextMenu(KeyPickup keyPickup, Vector2 mousePos)
	{
		_contextMenu.Clear();
		_contextMenuActions.Clear();

		AddContextMenuOption("Take", () => TryPickUpKey(keyPickup));
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
