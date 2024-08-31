using GameSystems.Jobs;

namespace Dxura.Darkrp;

public partial class Player
{
	/// <summary>
	/// The player state ID
	/// </summary>
	[HostSync]
	public PlayerState PlayerState { get; set; } = null!;

	/// <summary>
	/// The position this player last spawned at.
	/// </summary>
	[HostSync]
	public Vector3 SpawnPosition { get; set; }

	/// <summary>
	/// The rotation this player last spawned at.
	/// </summary>
	[HostSync]
	public Rotation SpawnRotation { get; set; }

	/// <summary>
	/// The tags of the last spawn point of this pawn.
	/// </summary>
	[HostSync]
	public NetList<string> SpawnPointTags { get; private set; } = new();

	/// <summary>
	/// What job does this pawn belong to?
	/// </summary>
	public JobResource Job => PlayerState.Job;

	/// <summary>
	/// Who's the owner?
	/// </summary>
	[Sync]
	public ulong SteamId { get; set; }
	
	/// <summary>
	/// What are we called?
	/// </summary>
	public string DisplayName => PlayerState.IsValid() ? PlayerState.DisplayName : "Invalid Player";
}
