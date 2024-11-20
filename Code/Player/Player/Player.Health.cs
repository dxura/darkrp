using Dxura.Darkrp.UI;
using Sandbox.Events;

namespace Dxura.Darkrp;

public partial class Player : IGameEventHandler<DamageGivenEvent>, IGameEventHandler<DamageTakenEvent>
{
	private RealTimeSince _timeSinceDamageTaken = 1;
	
	/// <summary>
	/// Called when YOU inflict damage on something
	/// </summary>
	void IGameEventHandler<DamageGivenEvent>.OnGameEvent( DamageGivenEvent eventArgs )
	{
		// Did we cause this damage?
		if ( !IsProxy )
		{
			Crosshair.Instance?.Trigger( eventArgs.DamageInfo );
		}
	}

	/// <summary>
	/// Called when YOU take damage from something
	/// </summary>
	void IGameEventHandler<DamageTakenEvent>.OnGameEvent( DamageTakenEvent eventArgs )
	{
		_timeSinceDamageTaken = 0;
		var damageInfo = eventArgs.DamageInfo;

		var attacker = GameUtils.GetPlayerFromComponent( eventArgs.DamageInfo.Attacker );
		var victim = GameUtils.GetPlayerFromComponent( eventArgs.DamageInfo.Victim );

		var position = eventArgs.DamageInfo.Position;
		var force = damageInfo.Force.IsNearZeroLength ? Random.Shared.VectorInSphere() : damageInfo.Force;

		AnimationHelper.ProceduralHitReaction( damageInfo.Damage / 100f, force );

		if ( !damageInfo.Attacker.IsValid() )
		{
			return;
		}

		// Is this the local player?
		if ( !IsProxy )
		{
			DamageIndicator.Current?.OnHit( position );
		}

		_timeUntilAccelerationRecovered = Global.TakeDamageAccelerationDampenTime;
		_accelerationAddedScale = Global.TakeDamageAccelerationOffset;

		if ( attacker != victim)
		{
			DamageTakenPosition = position;
			DamageTakenForce = force.Normal * damageInfo.Damage * Game.Random.Float( 5f, 20f );
		}

		// Headshot effects
		if ( damageInfo.Hitbox.HasFlag( HitboxTags.Head ) )
		{
			// Non-local viewer
			if ( IsProxy )
			{
				var go = damageInfo.HasHelmet
					? HeadshotWithHelmetEffect?.Clone( position )
					: HeadshotEffect?.Clone( position );
			}

			var headshotSound = damageInfo.HasHelmet ? HeadshotWithHelmetSound : HeadshotSound;
			if ( headshotSound is not null )
			{
				var handle = Sound.Play( headshotSound, position );
				if ( handle.IsValid )
				{
					handle.ListenLocal = attacker is { IsProxy: false } ||  victim is { IsProxy: false };
				}
			}
		}
		else
		{
			if ( BloodEffect.IsValid() )
			{
				BloodEffect?.Clone( new CloneConfig()
				{
					StartEnabled = true,
					Transform = new Transform( position ),
					Name = $"Blood effect from ({GameObject})"
				} );
			}

			if ( BloodImpactSound is not null )
			{
				var snd = Sound.Play( BloodImpactSound, position );

				if ( snd != null )
				{
					snd.ListenLocal = !IsProxy;
				}
			}
		}
	}
}
