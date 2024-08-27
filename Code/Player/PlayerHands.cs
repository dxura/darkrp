using System;

namespace Dxura.Darkrp.Player;

[Group( "Player" )]
[Title( "Player Hands" )]
public class PlayerHands : Component
{
	[Property]
	private float HoldScrollStep { get; set; } = 0.3f; // How much scrolling will increase/decrease hold distance

	[Property]
	private float HoldSmoothing { get; set; } =
		0.075f; // How smooth moving object is (e.g. max distance it can travel in one fixedUpdate)

	[Property] private float GrabRange { get; set; } = 120f; // How far away can you grab something
	
	[Property] private CameraComponent Camera { get; set; } = null!;
	[Property] public bool Debug { get; set; } = false;
	
	private const float HoldDistance = 70f;

	private GameObject? _held;
	private PhysicsBody? _heldBody;

	private float _heldDistance = HoldDistance;
	private Rotation _heldRotation = Rotation.Identity;

	private const float GridSize = 8f;
	
	private const float DeltaActionTime = 0.20f;
	private float _lastAction;

	private bool _rotating;
	
	private bool _shouldRelease;

	public bool IsHolding() =>  _held != null;

	protected override void OnStart()
	{
		if ( IsProxy ) Enabled = false;
	}

	protected override void OnUpdate()
	{
		var currentTime = RealTime.Now;

		if ( !(currentTime - _lastAction > DeltaActionTime) )
		{
			return;
		}

		if ( _shouldRelease || Input.Released( "attack1" ))
		{
			Release();
			_rotating = false;
			_shouldRelease = false;
			_lastAction = currentTime;
		}

		if ( IsHolding() )
		{
			_rotating = Input.Down( "run" );
		}
		else
		{
			if ( Input.Down( "attack1" ) )
			{
				AttemptGrab();
				_lastAction = currentTime;
			}
			
			_rotating = false;
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( _held != null )
		{
			var holdPosition = Camera.Transform.Position + Camera.Transform.World.Forward * _heldDistance;
			var heldDistance = Vector3.DistanceBetween( _held.Transform.Position, holdPosition );

			// Release (If too far)
			if (heldDistance > 1.5f * GrabRange )
			{
				_shouldRelease = true;
			}

			UpdateHeldPosition(holdPosition);
		}

	}

	private void UpdateHeldPosition( Vector3 holdPosition )
	{
		// Update _held position either via rigid body or transform depending on type
		if (_heldBody != null)
		{
			var velocity = _heldBody.Velocity;
			Vector3.SmoothDamp( _heldBody.Position, holdPosition, ref velocity, HoldSmoothing, Time.Delta );
			_heldBody.Velocity = velocity;

			var angularVelocity = _heldBody.AngularVelocity;
			Rotation.SmoothDamp( _heldBody.Rotation, _heldRotation, ref angularVelocity, HoldSmoothing, Time.Delta );
			_heldBody.AngularVelocity = angularVelocity;
		}
		else if(_held != null)
		{
			var snappedPosition = new Vector3(
				MathF.Round(holdPosition.x / GridSize) * GridSize,
				MathF.Round(holdPosition.y / GridSize) * GridSize,
				MathF.Round(holdPosition.z / GridSize) * GridSize
			);
			
			_held.Transform.Position = snappedPosition;
			_held.Transform.Rotation = _heldRotation;
		}
	}

	private void AttemptGrab()
	{
		// Starting position of the line (camera position)
		var start = Camera.Transform.Position;

		// Direction of the line (the direction the camera is facing)
		var direction = Camera.Transform.World.Forward;

		// Calculate the end position based on direction and interact range
		var end = start + direction * GrabRange;

		// Perform a line trace (raycast) to detect objects in the line of sight ( raycast ignore the player )
		var tr = Scene.Trace.Ray( start, end )
			.UseHitboxes()
			.IgnoreGameObject( GameObject )
			.WithTag( "grab" )
			.Run();

		// Check if the hit object has the "interact" tag and handle the interaction
		if ( tr.Hit && tr.GameObject.IsValid() )
		{
			Grab(tr.GameObject, tr.Body);
		}
	}

	private void Grab( GameObject target, PhysicsBody? targetBody )
	{
		target.Network.TakeOwnership();
		
		_held = target;

		var rb = target.Components.Get<Rigidbody>();
		if (rb  != null)
		{
			_heldBody = rb.PhysicsBody;
		}
		
		var boundsExtents = _held.GetBounds().Extents;
		_heldDistance = HoldDistance + Math.Max(Math.Max(boundsExtents.x, boundsExtents.y), boundsExtents.z);
		
		_heldRotation = target.Transform.Rotation;
	}

	private void Release()
	{
		// Clear references
		_held = null;
		_heldBody = null;
	}
}
