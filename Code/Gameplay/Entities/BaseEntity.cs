namespace Dxura.Darkrp;

/// <summary>
/// Represents a generic base component that provides common functionality 
/// for various types of interactable entities such as dropped money, printers, food, etc.
/// </summary>
public class BaseEntity : Component, IUse, IDamageListener
{
	/// <summary>
	/// Gets or sets the name of the entity.
	/// </summary>
	[Property] public string EntityName { get; set; } = "Base Entity";

	/// <summary>
	/// Gets or sets the owner of the entity.
	/// </summary>
	 public PlayerState? Owner { get; set; }

	/// <summary>
	/// Gets or sets whether the entity can be picked up by players.
	/// </summary>
	[Property] public bool CanBePickedUp { get; set; } = true;
	
	/// <summary>
	/// Health component (If we have one)
	/// </summary>
	[Property]
	public virtual HealthComponent? HealthComponent { get; set; }

	/// <summary>
	/// Called when the component is first created and added to a GameObject.
	/// Initializes the component and sets up necessary physics.
	/// </summary>
	protected override void OnStart()
	{
		base.OnStart();
		Log.Info( $"{EntityName} has been initialized." );
		SetupPhysics();

		// Ensure the entity has the interact tag to be recognized by the InteractionSystem
		GameObject.Tags.Add( "Interactable" );
	}

	/// <summary>
	/// Called when the entity's health reaches zero. Disables the component.
	/// </summary>
	[Broadcast]
	protected void OnDestroyed()
	{
		Log.Info( $"{EntityName} has been destroyed." );
		Destroy();
	}

	public void OnDamaged( DamageInfo damageInfo )
	{
		if ( HealthComponent is { Health: <= 0 } )
		{
			OnDestroyed();
		}
	}

	/// <summary>
	/// Sets up physics properties for the entity, such as colliders and collision groups.
	/// </summary>
	private void SetupPhysics()
	{
		// Setup physics, if necessary.
	}

	/// <summary>
	/// Spawns a new "entity" as a component on a GameObject.
	/// </summary>
	/// <param name="gameObject">The GameObject to add the entity to.</param>
	/// <param name="owner">The player who owns this entity.</param>
	public static void SpawnEntity( GameObject gameObject, PlayerState owner )
	{
		var entity = gameObject.Components.GetOrCreate<BaseEntity>();
		entity.Owner = owner;
		Log.Info( $"Spawned entity {entity.EntityName} on GameObject {gameObject} for player {owner}." );
	}

	public UseResult CanUse( Player player )
	{
		return true;
	}

	public virtual void OnUse( Player player ) {}
}
