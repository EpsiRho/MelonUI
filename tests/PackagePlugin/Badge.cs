namespace PackagePlugin;

public partial class Badge : global::MelonUI.Base.UIElement
{
    [global::MelonUI.Attributes.BindingAttribute]
    private global::System.String _caption = "Plugin caption";
    protected override void RenderContent(global::MelonUI.Base.ConsoleBuffer buffer) =>
        buffer.WriteString(0, 0, Caption, Foreground, Background);
}
