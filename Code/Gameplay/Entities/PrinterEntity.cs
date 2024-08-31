namespace Dxura.Darkrp;

public class PrinterEntityConfiguration {
	public Color Color { get; set; }
	public Material Material { get; set; }
	
	public float Price { get; set; }
	
	/// <summary>
	/// The timer for the printer to generate money in seconds
	/// </summary>
	public float Timer { get; set; }
}

[Title( "Printer" )]
[Category( "Entities" )]
public sealed class PrinterEntity : BaseEntity
{
	[Property] public GameObject PrinterFan { get; set; } = null!;
	[Property] private float PrinterFanSpeed { get; set; } = 1000f;

	[Property] private ModelRenderer ModelRenderer { get; set; } = null!;
	
	// Define the different types of printers
	public enum PrinterType { Bronze, Silver, Gold, Diamond };

	[Property] private readonly Dictionary<PrinterType, PrinterEntityConfiguration> _printerConfig = new();

	// Printer Timer Setup
	[Property, Sync] public int PrinterCurrentMoney { get; set; } = 0;
	[Property] private int PrinterTimerMoney { get; set; } = 25;
	[Property] private int PrinterMaxMoney { get; set; } = 8000;

	private TimeSince _lastUsed = 0; // Set the timer
	private PrinterType _currentPrinterType; // Store the current printer type
	
	public override void OnUse( Player player )
	{
		Log.Info( "Interacting with printer" );

		if ( PrinterCurrentMoney <= 0 || player.PlayerState == null )
		{
			return;
		}

		player.PlayerState?.GiveCash(  PrinterCurrentMoney );
			
		ResetPrinterMoney();
			
		Sound.Play( "audio/money.sound" );
	}
	
	protected override void OnFixedUpdate()
	{
		// Determine the timer based on the printer type
		var printerTimer = GetPrinterTimer();
		
		// If the timer has passed, add money
		if ( _lastUsed >= printerTimer )
		{
			if ( PrinterCurrentMoney < PrinterMaxMoney )
			{
				PrinterCurrentMoney += PrinterTimerMoney; // Add money to the printer
			}

			_lastUsed = 0; // Reset the timer
		}

		SpinFan();
	}

	private void SpinFan()
	{
		// Calculate the rotation amount based on PrinterFanSpeed and Time.Delta
		var rotationAmount = PrinterFanSpeed * Time.Delta;

		// Apply the rotation relative to the GameObject's current rotation
		PrinterFan.Transform.Rotation *= Rotation.FromAxis(Vector3.Left, -rotationAmount);
	}

	// Method to set the current printer type and update its color
	public void SetPrinterType( PrinterType type )
	{
		_currentPrinterType = type;
		
		// Automatically update the color when the printer type is set
		UpdatePrinterColor(); 
	}

	[Broadcast]
	private void ResetPrinterMoney()
	{
		PrinterCurrentMoney = 0;
	}
	
	// Method to get the correct timer based on the printer type
	private float GetPrinterTimer()
	{
		if ( _printerConfig.TryGetValue( _currentPrinterType, out var config ) )
		{
			return config.Timer;
		}
		
		return 60f; // Default timer, in case something goes wrong
	}

	// Method to update the printer color based on the printer type
	private void UpdatePrinterColor()
	{
		var newColor =
			// Default color, in case something goes wrong
			!_printerConfig.TryGetValue( _currentPrinterType, out var config ) ? Color.White : config.Color;

		ModelRenderer.Tint = newColor;
		ModelRenderer.MaterialOverride = config?.Material;
	}
}
