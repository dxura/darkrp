namespace Dxura.Darkrp;

/// <summary>
/// A component that manages the interaction and networking of prop entities, including cloud models.
/// </summary>
public sealed class PropHelper : Component
{
	/// <summary>
	/// The prop associated with this helper.
	/// </summary>
	private Prop _prop = null!;

	/// <summary>
	/// The unique identifier of the creator of this prop.
	/// </summary>
	[Sync] public Guid CreatorId { get; set; } = Guid.Empty;

	/// <summary>
	/// The identifier of the cloud model associated with this prop. When changed, the model is initialized.
	/// </summary>
	[Sync] string CloudModel { get; set; } = "";
	
	/// <summary>
	/// Called when the component starts. Initializes the prop and its related components.
	/// </summary>
	protected override void OnStart()
	{
		_prop = Components.Get<Prop>();

		InitCloudModel();
	}
	
	/// <summary>
	/// Initializes the cloud model for the prop by fetching and loading it from the cloud.
	/// </summary>
	async void InitCloudModel()
	{
		if (string.IsNullOrWhiteSpace(CloudModel)) return;

		var package = await Package.Fetch(CloudModel, false);
		await package.MountAsync();

		var model = Model.Load(package.GetMeta("PrimaryAsset", ""));
		if (model is null) return;

		if (_prop.IsValid())
		{
			_prop.Enabled = false;
			_prop.Model = model;
			_prop.Enabled = true;
		}
	}
	

	/// <summary>
	/// Sets the cloud model for this prop and initializes it.
	/// </summary>
	/// <param name="cloudModel">The identifier of the cloud model to set.</param>
	[Broadcast]
	public void SetCloudModel(string cloudModel)
	{
		CloudModel = cloudModel;
		InitCloudModel();
	}
}
