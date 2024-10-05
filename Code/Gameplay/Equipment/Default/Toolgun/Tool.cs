namespace Dxura.Darkrp;

public abstract class Tool: Component, IDescription
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Icon { get; }
    public abstract void UseTool(Player Player);
    public abstract void OpenToolOptions(Player Player);
}