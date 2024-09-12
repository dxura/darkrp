namespace Dxura.Darkrp;
using Sandbox;

[Title( "Nourishment" )]
[Category( "Entities" )]
public sealed class NourishmentItem: BaseEntity, IDescription
{
  [Property] private GameObject ParentGameObject = null!;
  [Property] public NourishmentResource CurrentNourishmentResource { get; set; } = null!;
  [Property] private ModelCollider ModelCollider = null!;
  [Property] private ModelRenderer ModelRenderer = null!;
  [Property] private SoundEvent? UseSound = null!;

  protected override void OnStart()
  {
    base.OnStart();

    ModelCollider.Model = CurrentNourishmentResource.Model;
    ModelRenderer.Model = CurrentNourishmentResource.Model;

    //if (CurrentNourishmentResource.ModelColor != null)
    //{
    //    Log.Info(CurrentNourishmentResource.ModelColor);
    //    ModelRenderer.Tint = CurrentNourishmentResource.ModelColor;
    //}

    UseSound = CurrentNourishmentResource.UseSound;

    if (CurrentNourishmentResource.ModelMaterial != null)
    {
      ModelRenderer.MaterialOverride = CurrentNourishmentResource.ModelMaterial;
    }
  }

  public override void OnUse( Player player )
  {
    ParentGameObject.Destroy();
    if (UseSound != null)
    {
      Sound.Play( UseSound );
    }
    player.HealthComponent.Health = Math.Min( player.HealthComponent.Health + CurrentNourishmentResource.HealAmount, 100 );
  }
}