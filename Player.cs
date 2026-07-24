using Godot;
using System;

public partial class Player : CharacterBody3D
{
	private SpringArm3D _springArm;
	private PopupMenu _lootMenu;
	private ILootable _activeLoot;
	private InventoryUI _inventoryUI;

	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;

	[Export] public float MouseSensitivity = 0.15f;
	[Export] public float ZoomSpeed = 0.5f;
	[Export] public float MinZoom = 2.0f;
	[Export] public float MaxZoom = 12.0f;
	[Export] public float MinPitch = -60f;
	[Export] public float MaxPitch = -5f;

	private float _cameraYaw = 0f;
	private float _cameraPitch = -20f;

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

		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
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

		_springArm.Rotation = new Vector3(Mathf.DegToRad(_cameraPitch), Mathf.DegToRad(_cameraYaw) - Rotation.Y, 0);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (Input.IsActionJustPressed("attack"))
		{
			var npc = GetNodeOrNull<Npc>("/root/World/NPC");
			if (npc != null)
			{
				float distance = GlobalPosition.DistanceTo(npc.GlobalPosition);
				if (distance < 3.0f)
				{
					npc.TakeDamage(1);
				}
			}
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
		string selectedItem = _inventoryUI.GetSelectedItem();

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
			UseItemOn(selectedItem, collider);
			_inventoryUI.ClearSelection();
			return;
		}

		if (!isRightClick && collider is KeyPickup keyPickup)
		{
			float distance = GlobalPosition.DistanceTo(keyPickup.GlobalPosition);
			if (distance < 3.0f)
			{
				keyPickup.PickUp();
			}
			else
			{
				GD.Print("Too far away to pick that up.");
			}
		}
		else if (!isRightClick && collider is LootPile lootPile && lootPile.Items.Count > 0)
		{
			float distance = GlobalPosition.DistanceTo(lootPile.GlobalPosition);
			if (distance < 3.0f)
			{
				string topItem = lootPile.Items[0];
				var inventory = GetNode<Inventory>("/root/World/PlayerInventory");
				inventory.AddItem(topItem);
				GD.Print("Looted: ", topItem);
				lootPile.RemoveItem(topItem);
			}
			else
			{
				GD.Print("Too far away to pick that up.");
			}
		}
		else if (isRightClick && collider is LootPile lootPile2)
		{
			OpenLootMenu(lootPile2, mousePos);
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
			if (!isRightClick)
			{
				GD.Print("I could loot this chest with the right key!");
			}
			return;
		}

		if (chest.Items.Count == 0) return;

		if (isRightClick)
		{
			OpenLootMenu(chest, mousePos);
			return;
		}

		float distance = GlobalPosition.DistanceTo(chest.GlobalPosition);
		if (distance < 3.0f)
		{
			string topItem = chest.Items[0];
			var inventory = GetNode<Inventory>("/root/World/PlayerInventory");
			inventory.AddItem(topItem);
			GD.Print("Looted: ", topItem);
			chest.RemoveItem(topItem);
		}
		else
		{
			GD.Print("Too far away to pick that up.");
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

	private void UseItemOn(string itemName, GodotObject target)
	{
		if (target is Chest chest)
		{
			float distance = GlobalPosition.DistanceTo(chest.GlobalPosition);
			if (distance >= 3.0f)
			{
				GD.Print("Too far away to use that here.");
				return;
			}

			if (chest.IsOpen)
			{
				PrintDefaultUseMessage();
				return;
			}

			if (chest.RequiredKeyName == itemName)
			{
				chest.Open();
				var inventory = GetNode<Inventory>("/root/World/PlayerInventory");
				inventory.RemoveItem(itemName);
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

	private void OpenLootMenu(ILootable lootable, Vector2 mousePos)
	{
		_activeLoot = lootable;
		_lootMenu.Clear();
		foreach (string item in lootable.Items)
		{
			_lootMenu.AddItem(item);
		}
		_lootMenu.Position = (Vector2I)mousePos;
		_lootMenu.Popup();
	}

	private void OnLootMenuIndexPressed(long index)
	{
		if (_activeLoot == null) return;

		string itemName = _lootMenu.GetItemText((int)index);
		var inventory = GetNode<Inventory>("/root/World/PlayerInventory");
		inventory.AddItem(itemName);
		GD.Print("Looted: ", itemName);

		_activeLoot.RemoveItem(itemName);
		_activeLoot = null;
	}
}
