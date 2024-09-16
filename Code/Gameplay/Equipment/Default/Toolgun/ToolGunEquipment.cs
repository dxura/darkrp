namespace Dxura.Darkrp.UI;

using Sandbox;
public class ToolGunEquipment : InputWeaponComponent
{

    [Property] public List<Tool> Tools { get; set; } = new();

    private Tool? CurrentTool = null!;

    protected override void OnInputUpdate()
    {
        if (Input.Down("attack1"))
        {
           
            // Is there a better way to do this?
            var currentPlayer = PlayerState.Local!.Player!;

            if (CurrentTool != null)
            {
                CurrentTool.UseTool(currentPlayer);
            }
        }
    }

    public void SetCurrentTool(string toolName)
    {
        CurrentTool = Tools.Find(tool => tool.Name == toolName);
    }
}