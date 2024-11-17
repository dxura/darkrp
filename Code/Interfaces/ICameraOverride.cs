namespace Dxura.Darkrp;

public interface ICameraOverride
{
    bool IsActive { get; }

    void UpdateCamera();
}