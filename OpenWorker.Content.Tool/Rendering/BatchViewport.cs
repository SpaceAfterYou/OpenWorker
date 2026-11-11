using System.Numerics;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using OpenWorker.Content.Tool.Models;
using Silk.NET.OpenGL;

namespace OpenWorker.Content.Tool.Rendering;

/// <summary>
/// Draws the ground rectangles of a <c>*.vbatch</c>: each box from its top-left, bottom-right and yaw. Game Z is up;
/// the viewport remaps that to OpenGL Y-up.
/// </summary>
public sealed class BatchViewport : OpenGlControlBase
{
    public static readonly StyledProperty<IReadOnlyList<BatchBoxDraw>?> BoxesProperty =
        AvaloniaProperty.Register<BatchViewport, IReadOnlyList<BatchBoxDraw>?>(nameof(Boxes));

    private GL? _gl;
    private uint _program;
    private uint _vao;
    private uint _vbo;
    private int _mvpLocation;
    private int _vertexCount;
    private float _yaw = 0.7f;
    private float _pitch = 0.55f;
    private float _distance = 40f;
    private Vector3 _target = Vector3.Zero;
    private bool _wheelHooked;
    private bool _meshDirty = true;

    public IReadOnlyList<BatchBoxDraw>? Boxes
    {
        get => GetValue(BoxesProperty);
        set => SetValue(BoxesProperty, value);
    }

    static BatchViewport()
    {
        BoxesProperty.Changed.AddClassHandler<BatchViewport>((viewport, _) =>
        {
            viewport._meshDirty = true;
            viewport.FrameCamera();
            viewport.RequestNextFrameRendering();
        });
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        base.OnOpenGlInit(gl);

        _gl = GL.GetApi(gl.GetProcAddress);
        _program = CreateProgram(_gl, IsOpenGles);
        _mvpLocation = _gl.GetUniformLocation(_program, "uMvp");

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _meshDirty = true;

        EnsureWheelInput();
        RequestNextFrameRendering();
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        if (_gl is not null)
        {
            if (_vbo != 0)
            {
                _gl.DeleteBuffer(_vbo);
                _vbo = 0;
            }

            if (_vao != 0)
            {
                _gl.DeleteVertexArray(_vao);
                _vao = 0;
            }

            if (_program != 0)
            {
                _gl.DeleteProgram(_program);
                _program = 0;
            }

            _gl.Dispose();
            _gl = null;
        }

        base.OnOpenGlDeinit(gl);
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        var silk = _gl;
        if (silk is null || _program == 0)
        {
            return;
        }

        if (_meshDirty)
        {
            UploadMesh(silk);
            _meshDirty = false;
        }

        var width = Math.Max(1, (int)Bounds.Width);
        var height = Math.Max(1, (int)Bounds.Height);

        silk.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)fb);
        silk.Viewport(0, 0, (uint)width, (uint)height);
        silk.Enable(EnableCap.DepthTest);
        silk.ClearColor(0.12f, 0.13f, 0.16f, 1f);
        silk.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if (_vertexCount == 0)
        {
            return;
        }

        var aspect = width / (float)height;
        var view = Matrix4x4.CreateLookAt(CameraPosition(), _target, Vector3.UnitY);
        var far = Math.Max(200f, _distance * 8f);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, aspect, 1f, far);
        var mvp = view * projection;

        silk.UseProgram(_program);
        unsafe
        {
            silk.UniformMatrix4(_mvpLocation, 1, false, (float*)&mvp);
        }

        silk.BindVertexArray(_vao);
        silk.DrawArrays(PrimitiveType.Lines, 0, (uint)_vertexCount);
        silk.BindVertexArray(0);
        silk.UseProgram(0);

        RequestNextFrameRendering();
    }

    private void UploadMesh(GL silk)
    {
        var vertices = BuildLineMesh(Boxes);
        _vertexCount = vertices.Length / 6;

        silk.BindVertexArray(_vao);
        silk.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        silk.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices.Length == 0 ? [0f] : vertices, BufferUsageARB.DynamicDraw);

        const uint stride = 6 * sizeof(float);
        silk.EnableVertexAttribArray(0);
        unsafe
        {
            silk.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
            silk.EnableVertexAttribArray(1);
            silk.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
        }

        silk.BindVertexArray(0);
        silk.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }

    private void FrameCamera()
    {
        var boxes = Boxes;
        if (boxes is null || boxes.Count == 0)
        {
            _target = Vector3.Zero;
            _distance = 40f;
            return;
        }

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var box in boxes)
        {
            foreach (var corner in CornersGl(box))
            {
                min = Vector3.Min(min, corner);
                max = Vector3.Max(max, corner);
            }
        }

        _target = (min + max) * 0.5f;
        var extent = max - min;
        var radius = MathF.Max(extent.X, MathF.Max(extent.Y, extent.Z)) * 0.6f;
        _distance = Math.Clamp(radius / MathF.Tan(MathF.PI / 8f), 20f, 200_000f);
    }

    private Vector3 CameraPosition()
    {
        var cosPitch = MathF.Cos(_pitch);
        return _target + new Vector3(
            _distance * MathF.Cos(_yaw) * cosPitch,
            _distance * MathF.Sin(_pitch),
            _distance * MathF.Sin(_yaw) * cosPitch);
    }

    private void EnsureWheelInput()
    {
        if (_wheelHooked)
        {
            return;
        }

        _wheelHooked = true;
        PointerWheelChanged += (_, e) =>
        {
            var factor = e.Delta.Y > 0 ? 0.9f : 1.1f;
            _distance = Math.Clamp(_distance * factor, 10f, 400_000f);
            e.Handled = true;
        };
    }

    private static float[] BuildLineMesh(IReadOnlyList<BatchBoxDraw>? boxes)
    {
        var lines = new List<float>(4096);

        void AddLine(Vector3 a, Vector3 b, Vector3 color)
        {
            lines.Add(a.X);
            lines.Add(a.Y);
            lines.Add(a.Z);
            lines.Add(color.X);
            lines.Add(color.Y);
            lines.Add(color.Z);
            lines.Add(b.X);
            lines.Add(b.Y);
            lines.Add(b.Z);
            lines.Add(color.X);
            lines.Add(color.Y);
            lines.Add(color.Z);
        }

        if (boxes is null)
        {
            return [];
        }

        foreach (var box in boxes)
        {
            var corners = CornersGl(box);
            AddLine(corners[0], corners[1], box.Color);
            AddLine(corners[1], corners[2], box.Color);
            AddLine(corners[2], corners[3], box.Color);
            AddLine(corners[3], corners[0], box.Color);
        }

        return [.. lines];
    }

    /// <summary>
    /// Ground rectangle in OpenGL space: game XY is the floor, game Z is height (mapped to GL Y).
    /// </summary>
    private static Vector3[] CornersGl(BatchBoxDraw box)
    {
        var minX = MathF.Min(box.TopLeft.X, box.BottomRight.X);
        var maxX = MathF.Max(box.TopLeft.X, box.BottomRight.X);
        var minY = MathF.Min(box.TopLeft.Y, box.BottomRight.Y);
        var maxY = MathF.Max(box.TopLeft.Y, box.BottomRight.Y);
        var height = (box.TopLeft.Z + box.BottomRight.Z) * 0.5f;

        var centerX = (minX + maxX) * 0.5f;
        var centerY = (minY + maxY) * 0.5f;
        var radians = box.RotationDegrees * (MathF.PI / 180f);
        var (sin, cos) = MathF.SinCos(radians);

        Vector3 Rotate(float x, float y)
        {
            var dx = x - centerX;
            var dy = y - centerY;
            var rx = centerX + dx * cos - dy * sin;
            var ry = centerY + dx * sin + dy * cos;
            return ToGl(new Vector3(rx, ry, height));
        }

        return
        [
            Rotate(minX, minY),
            Rotate(maxX, minY),
            Rotate(maxX, maxY),
            Rotate(minX, maxY)
        ];
    }

    private static Vector3 ToGl(Vector3 game) => new(game.X, game.Z, game.Y);

    private bool IsOpenGles => GlVersion.Type == GlProfileType.OpenGLES;

    private static uint CreateProgram(GL gl, bool openGles)
    {
        var version = openGles ? "300 es" : "330 core";
        var precision = openGles ? "precision mediump float;\n" : string.Empty;

        var vertexSource =
            "#version " + version + "\n" +
            precision +
            """
            layout (location = 0) in vec3 aPos;
            layout (location = 1) in vec3 aColor;
            uniform mat4 uMvp;
            out vec3 vColor;
            void main()
            {
                vColor = aColor;
                gl_Position = uMvp * vec4(aPos, 1.0);
            }
            """;

        var fragmentSource =
            "#version " + version + "\n" +
            precision +
            """
            in vec3 vColor;
            out vec4 FragColor;
            void main()
            {
                FragColor = vec4(vColor, 1.0);
            }
            """;

        var vertex = CompileShader(gl, ShaderType.VertexShader, vertexSource);
        var fragment = CompileShader(gl, ShaderType.FragmentShader, fragmentSource);
        var program = gl.CreateProgram();
        gl.AttachShader(program, vertex);
        gl.AttachShader(program, fragment);
        gl.LinkProgram(program);
        gl.GetProgram(program, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            var log = gl.GetProgramInfoLog(program);
            gl.DeleteProgram(program);
            gl.DeleteShader(vertex);
            gl.DeleteShader(fragment);
            throw new InvalidOperationException($"Shader link failed: {log}");
        }

        gl.DetachShader(program, vertex);
        gl.DetachShader(program, fragment);
        gl.DeleteShader(vertex);
        gl.DeleteShader(fragment);
        return program;
    }

    private static uint CompileShader(GL gl, ShaderType type, string source)
    {
        var shader = gl.CreateShader(type);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        gl.GetShader(shader, GLEnum.CompileStatus, out var status);
        if (status == 0)
        {
            var log = gl.GetShaderInfoLog(shader);
            gl.DeleteShader(shader);
            throw new InvalidOperationException($"Shader compile failed ({type}): {log}");
        }

        return shader;
    }
}
