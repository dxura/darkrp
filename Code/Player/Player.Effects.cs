using Dxura.Darkrp;
using Dxura.Darkrp;
using Sandbox.Events;

namespace Dxura.Darkrp;

public partial class Player : IGameEventHandler<JobChangedEvent>
{
	/// <summary>
	/// What effect should we spawn when a player gets headshot?
	/// </summary>
	[Property]
	[Feature( "Effects" )]
	private GameObject? HeadshotEffect { get; set; }

	/// <summary>
	/// What effect should we spawn when a player gets headshot while wearing a helmet?
	/// </summary>
	[Property]
	[Feature( "Effects" )]
	private GameObject? HeadshotWithHelmetEffect { get; set; }

	/// <summary>
	/// What effect should we spawn when we hit a player?
	/// </summary>
	[Property]
	[Feature( "Effects" )]
	private GameObject? BloodEffect { get; set; }

	/// <summary>
	/// What sound should we play when a player gets headshot?
	/// </summary>
	[Property]
	[Feature( "Effects" ), Group("Sounds")]
	private SoundEvent? HeadshotSound { get; set; }

	/// <summary>
	/// What sound should we play when a player gets headshot?
	/// </summary>
	[Property]
	[Feature( "Effects" ), Group("Sounds")]
	private SoundEvent HeadshotWithHelmetSound { get; set; } = null!;

	/// <summary>
	/// What sound should we play when we hit a player?
	/// </summary>
	[Property]
	[Feature( "Effects" ), Group("Sounds")]
	private SoundEvent? BloodImpactSound { get; set; }
	
	/// <summary>
	/// What sound should we play when we change jobs?
	/// </summary>
	[Property]
	[Feature( "Effects" ), Group("Sounds")]
	private SoundEvent? JobChangedSound { get; set; }
	
	[Property, Feature( "Effects" ), Group("Sounds")]
	public SoundEvent? LandSound { get; set; }


	private bool IsOutlineVisible()
	{
		var localPlayer = Player.Local;
		if ( localPlayer.IsValid() &&
		     HealthComponent.State == LifeState.Dead )
		{
			if ( localPlayer.GetLastKiller() == this )
			{
				return true;
			}
		}
		
		return false;
	}

	private void OnFixedUpdateEffects()
	{
		// Somehow this can happen?
		if ( !Player.Local.IsValid() )
		{
			return;
		}

		if ( !IsOutlineVisible() )
		{
			Outline.Enabled = false;
			return;
		}

		Outline.Enabled = true;
		Outline.Width = 0.2f;
		Outline.Color = Color.Transparent;
		Outline.InsideColor = HealthComponent.IsGodMode ? Color.White.WithAlpha( 0.1f ) : Color.Transparent;

		if ( Local.GetLastKiller() == this )
		{
			Outline.ObscuredColor = Color.Red;
		}
		else
		{

			// TODO
			// Outline.ObscuredColor = Local.Job == Job && player != null
			// 	? player.PlayerColor
			// 	: Color.Transparent;
		}
	}
}
