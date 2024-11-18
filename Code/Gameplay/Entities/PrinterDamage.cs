using Microsoft.VisualBasic;
using Sandbox;

namespace Dxura.Darkrp;

public sealed class PrinterDamage : Component,Component.ITriggerListener,IDamageListener,IRespawnable
{
	
	[Property] public GameObject Printer { get;set;}
	[Property] public HealthComponent HealthComponent { get;set;}
	[Property] public GameObject Fire { get; set; }
	[Property] public GameObject Explosion { get; set; }
	
	[Property] public float ExplosionDamage { get; set;} = 50.0f;
	private float PrinterMidLife = 30f;
	private GameObject _printer;

	public void OnKill(DamageInfo damageInfo){
		Explosion.Clone(Transform.Position);
	}

	public void OnDamaged (DamageInfo damageInfo){
		if(HealthComponent.Health < PrinterMidLife && !_printer.IsValid()){
			_printer = Fire.Clone( GameObject, Vector3.Zero, Rotation.Identity, Vector3.One );
		}
	}

	protected void PrinterExplosionDamage(){
		var player = Player.Local;
		if(HealthComponent.Health <= 2){
			player.HealthComponent.TakeDamage(new DamageInfo(player as Component,ExplosionDamage,Hitbox:HitboxTags.None,Flags:DamageFlags.Explosion));
			Explosion.Clone(Transform.Position);
			OnDestroy();
		}
	}
    protected override void OnDestroy()
    {
        if ( _printer.IsValid() )
		{
			_printer.Destroy();
		}
		GameObject.Destroy();
    }

    public void OnTriggerEnter(Collider other)
	{
		PrinterExplosionDamage();
	}	

	public void OnTriggerExit(Collider other)
	{
		
	}
	
	protected override void OnUpdate()
	{

	}
}
