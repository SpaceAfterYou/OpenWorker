using Avalonia;
using Avalonia.Controls;
using OpenWorker.Navigation;

namespace OpenWorker.Content.Tool.Rendering;

/// <summary>
/// Shows a navigation mesh loaded from a Havok <c>*.hkt</c> tagfile through <see cref="NavMesh"/>.
/// </summary>
public sealed class NavMeshViewport : UserControl
{
    public static readonly StyledProperty<NavMesh?> MeshProperty =
        AvaloniaProperty.Register<NavMeshViewport, NavMesh?>(nameof(Mesh));

    private readonly OrbitMeshViewport _orbit = new();

    public NavMesh? Mesh
    {
        get => GetValue(MeshProperty);
        set => SetValue(MeshProperty, value);
    }

    static NavMeshViewport()
    {
        MeshProperty.Changed.AddClassHandler<NavMeshViewport>((viewport, args) =>
        {
            var mesh = args.NewValue as NavMesh;
            viewport._orbit.Scene = mesh is null ? null : NavMeshSceneBuilder.Build(mesh);
        });
    }

    public NavMeshViewport()
    {
        Content = _orbit;
    }
}
