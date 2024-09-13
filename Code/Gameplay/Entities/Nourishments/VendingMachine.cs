namespace Dxura.Darkrp;
using Sandbox;
using Dxura.Darkrp.UI;

[Title( "Vending Machine" )]
[Category( "Entities" )]
public sealed class VendingMachine: BaseEntity, IDescription
{
    [Property] public GameObject NourishmentItemPrefab { get; set; } = null!;
    [Property] public NourishmentTypes NourishmentType { get; set; }

    [Property] public SoundEvent? BuySound { get; set; }

    public void OnItemBought( Player player, NourishmentResource item )
    {
        GameObject itemEntity = NourishmentItemPrefab.Clone(this.Transform.Position);

        itemEntity.Transform.Position = this.Transform.Position + (Vector3.Up * Math.Min(10, itemEntity.GetBounds().Size.z)) + Vector3.Right * Math.Min(25, itemEntity.GetBounds().Size.y);
        itemEntity.Components.Get<NourishmentItem>().CurrentNourishmentResource = item;

        itemEntity.NetworkSpawn();

        if (BuySound != null)
        {
            Sound.Play(BuySound);
        }

        player.PlayerState.SetBalance( player.PlayerState.Balance - item.Price );

        Toast.Instance.Show( $"You bought {item.Name} for ${item.Price}", ToastType.Generic );
    }
}