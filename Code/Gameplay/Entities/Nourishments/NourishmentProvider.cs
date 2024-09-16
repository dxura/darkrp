using Sandbox;
namespace Dxura.Darkrp;

public static class NourishmentProvider
{
  public static HashSet<NourishmentResource> Nourishments = new();

  static NourishmentProvider()
  {
    Log.Info("Loading nourishments...");

    Nourishments.Clear();

    foreach ( var nourishmentResource in ResourceLibrary.GetAll<NourishmentResource>() )
    {
      Log.Info( $"Loading nourishment {nourishmentResource.Name}" );
      Nourishments.Add( nourishmentResource );
    }
  }

  public static IEnumerable<NourishmentResource> GetOrderedNourishmentTypes()
  {
    return Nourishments.OrderBy( o => o.Name);
  }

  public static IEnumerable<NourishmentResource> GetNourishmentByType(NourishmentTypes type)
  {
    return Nourishments.Where( o => o.NourishmentType == type );
  }
}