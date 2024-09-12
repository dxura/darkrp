namespace Dxura.Darkrp;
using Sandbox;

[Title( "Vending Machine" )]
[Category( "Entities" )]
public sealed class VendingMachine: BaseEntity, IDescription
{
    [Property] public GameObject NourishmentItemPrefab { get; set; } = null!;
    [Property] public NourishmentTypes NourishmentType { get; set; }

    [Property] public SoundEvent? BuySound { get; set; }

    public void OnItemBought( Player player, NourishmentResource item )
    {
        GameObject itemEntity = NourishmentItemPrefab.Clone(this.Transform.Position + (Vector3.Right * 10) + (Vector3.Backward * 7) + (Vector3.Up * 10));
        itemEntity.Transform.Rotation *= new Angles(90,0,0);
        itemEntity.Components.Get<NourishmentItem>().CurrentNourishmentResource = item;

        itemEntity.NetworkSpawn();

        if (BuySound != null)
        {
            Sound.Play(BuySound);
        }

        // TODO: handle player money
    }
}