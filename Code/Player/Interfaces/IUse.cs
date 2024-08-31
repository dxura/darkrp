using Dxura.Darkrp;

namespace Dxura.Darkrp;

public record UseResult
{
	public bool CanUse { get; set; } = false;
	public string Reason { get; set; }

	// How on god's green earth is this legal
	public static implicit operator UseResult( bool boolean )
	{
		return new UseResult { CanUse = boolean };
	}

	public static implicit operator UseResult( string reason )
	{
		return new UseResult { CanUse = false, Reason = reason };
	}
}

public interface IUse : IValid
{
	public UseResult CanUse( Player player );
	public void OnUse( Player player );
}
