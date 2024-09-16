namespace Dxdua.Darkrp;

using Sandbox;
public class ToolGunEquipment : InputWeaponComponent
{

    [Property] public List<Tool> Tools { get; set; } = new();

    protected override void OnInputUpdate()
    {
        if (Input.Down("attack1"))
        {
            var selectedTool = GetSelectedTool("Text Screen");
            

            // Is there a better way to do this?
            var currentPlayer = PlayerState.Local!.Player!;

            if (selectedTool != null)
            {
                selectedTool.UseTool(currentPlayer);
            }
        }
    }


    private Tool? GetSelectedTool(string toolName)
    {
        return Tools.Find(tool => tool.Name == toolName);
    }
}