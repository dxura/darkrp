using Dxura.Darkrp;
using Dxura.Darkrp;

namespace Dxura.Darkrp;

public partial class Player
{
	/// <summary>
	/// What effect should we spawn when a player gets headshot?
	/// </summary>
	[Property]
	[Group( "Effects" )]
	public GameObject HeadshotEffect { get; set; }

	/// <summary>
	/// What effect should we spawn when a player gets headshot while wearing a helmet?
	/// </summary>
	[Property]
	[Group( "Effects" )]
	public GameObject HeadshotWithHelmetEffect { get; set; }

	/// <summary>
	/// What effect should we spawn when we hit a player?
	/// </summary>
	[Property]
	[Group( "Effects" )]
	public GameObject BloodEffect { get; set; }

	/// <summary>
	/// What sound should we play when a player gets headshot?
	/// </summary>
	[Property]
	[Group( "Effects" )]
	public SoundEvent? HeadshotSound { get; set; }

	/// <summary>
	/// What sound should we play when a player gets headshot?
	/// </summary>
	[Property]
	[Group( "Effects" )]
	public SoundEvent HeadshotWithHelmetSound { get; set; } = null!;

	/// <summary>
	/// What sound should we play when we hit a player?
	/// </summary>
	[Property]
	[Group( "Effects" )]
	public SoundEvent? BloodImpactSound { get; set; }

	private bool IsOutlineVisible()
	{
		if ( !HealthComponent.IsValid() )
		{
			return false;
		}

		if ( HealthComponent.State != LifeState.Alive )
		{
			return false;
		}

		if ( HealthComponent.IsGodMode )
		{
			return true;
		}

		var playerState = PlayerState.Local;
		if ( playerState.IsValid() && playerState.Player.IsValid() &&
		     playerState.Player.HealthComponent.State == LifeState.Dead )
		{
			if ( playerState.GetLastKiller() == this )
			{
				return true;
			}
		}

		var viewer = PlayerState.Local;
		if ( viewer.IsValid() )
		{
			return Job == viewer.Job;
		}

		return false;
	}

	private void UpdateOutline()
	{
		// Somehow this can happen?
		if ( !PlayerState.Local.IsValid() )
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

		if ( PlayerState.Local.GetLastKiller() == this )
		{
			Outline.ObscuredColor = Color.Red;
		}
		else
		{
			Outline.ObscuredColor = PlayerState.Local.Job == Job
				? PlayerState.PlayerColor
				: Color.Transparent;
		}
	}
}
