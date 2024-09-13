namespace Dxura.Darkrp;

[Title( "Text Screen" )]
[Category( "Entities" )]
public sealed class TextScreen : BaseEntity, IDescription
{
    [Category( "Description" )] public string Text { get; set; } = null!;
}

