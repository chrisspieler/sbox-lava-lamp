using System;

public class Orbiter : Component
{
	[Property] public float Distance { get; set; } = 10f;
	[Property, Range( -1, 1 )] public float ClockwiseFactor { get; set; } = 1;
	[Property, Range(0.1f, 500f)] public float RPM { get; set; } = 30f;
	[Property] public float Period => 60f / RPM;

	protected override void OnUpdate()
	{
		var elapsed = MathX.UnsignedMod( Time.Now, Period ) * ClockwiseFactor;
		var progress = elapsed / Period;
		var yaw = progress * 360f;
		var rotation = Rotation.FromYaw( yaw );
		var normal = Vector3.Forward * rotation;
		LocalPosition = normal * Distance;
	}
}
