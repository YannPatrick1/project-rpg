using Godot;

// Attached to a Label3D root. Spawned at the clicked world position, plays a
// quick grow+fade animation, then removes itself. Color is set per-call so
// the same scene can show either the "walk" or "interact" indicator.
public partial class ClickIndicator : Label3D
{
	private const float Lifetime = 0.5f;
	private const float StartScale = 0.4f;
	private const float EndScale = 1.0f;

	private double _elapsed = 0;

	public override void _Process(double delta)
	{
		_elapsed += delta;
		float t = (float)(_elapsed / Lifetime);

		if (t >= 1f)
		{
			QueueFree();
			return;
		}

		float scale = Mathf.Lerp(StartScale, EndScale, t);
		Scale = new Vector3(scale, scale, scale);

		Color c = Modulate;
		c.A = Mathf.Lerp(1f, 0f, t);
		Modulate = c;
	}

	public void Play(Vector3 worldPosition, Color color)
	{
		GlobalPosition = worldPosition;
		Modulate = color;
		Scale = new Vector3(StartScale, StartScale, StartScale);
		_elapsed = 0;
	}
}
