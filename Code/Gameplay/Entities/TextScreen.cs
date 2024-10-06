namespace Dxura.Darkrp;

[Title( "Text Screen" )]
[Category( "Entities" )]
public sealed class TextScreen : BaseEntity, IDescription, IUndoable
{
    [Category( "Description" )] public string Text { get; set; } = null!;

    public void Undo()
    {
        this.GameObject.Destroy(); 
    }
}

