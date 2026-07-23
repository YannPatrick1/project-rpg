using Godot;
using System;

public partial class Player : CharacterBody3D
{
	private SpringArm3D _springArm;

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
	
	private PopupMenu _lootMenu;
	private LootPile _activeLootPile;

	public override void _Ready()
	{
		_springArm = GetNode<SpringArm3D>("SpringArm3D");
		_lootMenu = GetNode<PopupMenu>("/root/World/LootMenu");
		_lootMenu.IndexPressed += OnLootMenuIndexPressed;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;
		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}
		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}
		// Get input, then rotate it to match camera facing (camera-relative movement)
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

		// Compensate SpringArm's rotation so the camera stays fixed in world space,
		// regardless of which way the Player node is currently facing.
		_springArm.Rotation = new Vector3(Mathf.DegToRad(_cameraPitch), Mathf.DegToRad(_cameraYaw) - Rotation.Y, 0);
	}

	public override void _Input(InputEvent @event)
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

		// Camera orbit: middle mouse button held + drag
		if (@event is InputEventMouseMotion mouseMotion && Input.IsMouseButtonPressed(MouseButton.Middle))
		{
			_cameraYaw -= mouseMotion.Relative.X * MouseSensitivity;
			_cameraPitch = Mathf.Clamp(_cameraPitch - mouseMotion.Relative.Y * MouseSensitivity, MinPitch, MaxPitch);
		}

		// Mouse button actions: zoom, left click, right click
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
		var camera = _springArm.GetNode<Camera3D>("Camera3D");
		Vector3 rayOrigin = camera.ProjectRayOrigin(mousePos);
		Vector3 rayDirection = camera.ProjectRayNormal(mousePos);
		Vector3 rayEnd = rayOrigin + rayDirection * 1000f;

		var spaceState = GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
		query.CollideWithAreas = true;
		query.CollideWithBodies = true;

		var result = spaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			var collider = result["collider"].AsGodotObject();
			GD.Print("Clicked on: ", collider, " | Right click: ", isRightClick);
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
				_activeLootPile = lootPile2;
				_lootMenu.Clear();
				foreach (string item in lootPile2.Items)
				{
					_lootMenu.AddItem(item);
				}
				_lootMenu.Position = (Vector2I)mousePos;
				_lootMenu.Popup();
			}
		}                                       // <-- ADDED: closes "if (result.Count > 0)"
		else
		{
			GD.Print("Clicked on nothing.");
		}
	}
	private void OnLootMenuIndexPressed(long index)
	{
		if (_activeLootPile == null) return;

		string itemName = _lootMenu.GetItemText((int)index);
		var inventory = GetNode<Inventory>("/root/World/PlayerInventory");
		inventory.AddItem(itemName);
		GD.Print("Looted: ", itemName);

		_activeLootPile.RemoveItem(itemName);
		_activeLootPile = null;
	}
}
