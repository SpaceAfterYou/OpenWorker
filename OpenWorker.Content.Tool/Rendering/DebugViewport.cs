using System.Numerics;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Silk.NET.OpenGL;

namespace OpenWorker.Content.Tool.Rendering;

/// <summary>
/// OpenGL viewport hosted in Avalonia. Draws a ground grid and RGB axes as a Silk.NET smoke test.
/// </summary>
public sealed class DebugViewport : OpenGlControlBase
{
    private const float GridHalfExtent = 10f;
    private const float GridStep = 1f;
    private const float AxisLength = 2f;

    private GL? _gl;
    private uint _program;
    private uint _vao;
    private uint _vbo;
    private int _mvpLocation;
    private int _vertexCount;
    private float _yaw = 0.6f;
    private float _pitch = 0.45f;
    private float _distance = 18f;
    private bool _wheelHooked;

    protected override void OnOpenGlInit(GlInterface gl)
    {
        base.OnOpenGlInit(gl);

        _gl = GL.GetApi(gl.GetProcAddress);
        _program = CreateProgram(_gl, IsOpenGles);
        _mvpLocation = _gl.GetUniformLocation(_program, "uMvp");

        var vertices = BuildLineMesh();
        _vertexCount = vertices.Length / 6;

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);

        const uint stride = 6 * sizeof(float);
        _gl.EnableVertexAttribArray(0);
        unsafe
        {
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
        }

        _gl.BindVertexArray(0);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

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

        var width = Math.Max(1, (int)Bounds.Width);
        var height = Math.Max(1, (int)Bounds.Height);

        silk.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)fb);
        silk.Viewport(0, 0, (uint)width, (uint)height);
        silk.Enable(EnableCap.DepthTest);
        silk.ClearColor(0.12f, 0.13f, 0.16f, 1f);
        silk.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var aspect = width / (float)height;
        var view = Matrix4x4.CreateLookAt(CameraPosition(), Vector3.Zero, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4f,
            aspect,
            0.1f,
            200f);
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

        _yaw += 0.004f;
        RequestNextFrameRendering();
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
            _distance = Math.Clamp(_distance - (float)e.Delta.Y, 4f, 80f);
            e.Handled = true;
        };
    }

    private Vector3 CameraPosition()
    {
        var cosPitch = MathF.Cos(_pitch);
        return new Vector3(
            _distance * MathF.Cos(_yaw) * cosPitch,
            _distance * MathF.Sin(_pitch),
            _distance * MathF.Sin(_yaw) * cosPitch);
    }

    private static float[] BuildLineMesh()
    {
        var lines = new List<float>(512);

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

        var gridColor = new Vector3(0.35f, 0.37f, 0.42f);
        for (var i = -GridHalfExtent; i <= GridHalfExtent + 0.01f; i += GridStep)
        {
            AddLine(new Vector3(i, 0f, -GridHalfExtent), new Vector3(i, 0f, GridHalfExtent), gridColor);
            AddLine(new Vector3(-GridHalfExtent, 0f, i), new Vector3(GridHalfExtent, 0f, i), gridColor);
        }

        AddLine(Vector3.Zero, new Vector3(AxisLength, 0f, 0f), new Vector3(0.9f, 0.2f, 0.2f));
        AddLine(Vector3.Zero, new Vector3(0f, AxisLength, 0f), new Vector3(0.2f, 0.85f, 0.3f));
        AddLine(Vector3.Zero, new Vector3(0f, 0f, AxisLength), new Vector3(0.25f, 0.45f, 0.95f));

        return [.. lines];
    }

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
