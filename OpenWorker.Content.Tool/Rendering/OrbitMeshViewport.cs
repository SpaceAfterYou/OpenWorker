using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using OpenWorker.Content.Tool.Models;
using Silk.NET.OpenGL;

namespace OpenWorker.Content.Tool.Rendering;

/// <summary>
/// Draws an <see cref="OrbitScene"/> with the same camera as the batch viewport: right-drag orbits around a
/// pivot kept in front of the camera, Shift+right-drag pans, WASD flies along the look, the wheel zooms.
/// </summary>
public sealed class OrbitMeshViewport : UserControl
{
    public static readonly StyledProperty<OrbitScene?> SceneProperty =
        AvaloniaProperty.Register<OrbitMeshViewport, OrbitScene?>(nameof(Scene));

    private readonly OrbitGlSurface _surface = new() { IsHitTestVisible = false };
    private readonly Border _overlay;
    private TopLevel? _topLevel;
    private bool _pointerInside;
    private bool _orbiting;
    private bool _panning;
    private Point _lastPointer;

    public OrbitScene? Scene
    {
        get => GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    static OrbitMeshViewport()
    {
        SceneProperty.Changed.AddClassHandler<OrbitMeshViewport>((viewport, args) =>
        {
            viewport._surface.Scene = args.NewValue as OrbitScene;
        });
    }

    public OrbitMeshViewport()
    {
        Focusable = true;
        _overlay = new Border
        {
            Background = Brushes.Transparent,
            Focusable = true,
            IsHitTestVisible = true
        };

        Content = new Grid
        {
            Children =
            {
                _surface,
                _overlay
            }
        };

        _overlay.PointerEntered += (_, _) =>
        {
            _pointerInside = true;
            _overlay.Focus();
        };
        _overlay.PointerExited += (_, _) =>
        {
            _pointerInside = false;
            ClearMovement();
        };
        _overlay.PointerPressed += OnPointerPressed;
        _overlay.PointerMoved += OnPointerMoved;
        _overlay.PointerReleased += OnPointerReleased;
        _overlay.PointerCaptureLost += (_, _) =>
        {
            _orbiting = false;
            _panning = false;
        };
        _overlay.PointerWheelChanged += OnPointerWheelChanged;
        _overlay.KeyDown += OnKeyDown;
        _overlay.KeyUp += OnKeyUp;
        _overlay.LostFocus += (_, _) => ClearMovement();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        DetachTopLevel();
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is null)
        {
            return;
        }

        _topLevel.AddHandler(KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Tunnel);
        _topLevel.AddHandler(KeyUpEvent, OnTopLevelKeyUp, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachTopLevel();
        ClearMovement();
        base.OnDetachedFromVisualTree(e);
    }

    private void DetachTopLevel()
    {
        if (_topLevel is null)
        {
            return;
        }

        _topLevel.RemoveHandler(KeyDownEvent, OnTopLevelKeyDown);
        _topLevel.RemoveHandler(KeyUpEvent, OnTopLevelKeyUp);
        _topLevel = null;
    }

    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsInputActive())
        {
            return;
        }

        if (SetMoveKey(e, true))
        {
            e.Handled = true;
        }
    }

    private void OnTopLevelKeyUp(object? sender, KeyEventArgs e)
    {
        if (SetMoveKey(e, false))
        {
            e.Handled = true;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (SetMoveKey(e, true))
        {
            e.Handled = true;
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (SetMoveKey(e, false))
        {
            e.Handled = true;
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _overlay.Focus();
        var point = e.GetCurrentPoint(_overlay);
        if (point.Properties.PointerUpdateKind != PointerUpdateKind.RightButtonPressed
            && !point.Properties.IsRightButtonPressed)
        {
            return;
        }

        var pan = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        _panning = pan;
        _orbiting = !pan;
        if (_orbiting)
        {
            _surface.CaptureOrbitPivot();
        }

        _lastPointer = e.GetPosition(_overlay);
        e.Pointer.Capture(_overlay);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_orbiting && !_panning)
        {
            return;
        }

        var position = e.GetPosition(_overlay);
        var dx = (float)(position.X - _lastPointer.X);
        var dy = (float)(position.Y - _lastPointer.Y);
        _lastPointer = position;

        if (_panning)
        {
            _surface.Pan(dx, dy);
        }
        else
        {
            _surface.Orbit(dx, dy);
        }

        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_orbiting && !_panning)
        {
            return;
        }

        _orbiting = false;
        _panning = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _surface.Zoom(e.Delta.Y > 0 ? 0.9f : 1.1f);
        e.Handled = true;
    }

    private bool IsInputActive() =>
        _pointerInside || _orbiting || _panning || _overlay.IsFocused || IsFocused;

    private bool SetMoveKey(KeyEventArgs e, bool down)
    {
        var direction = e.PhysicalKey switch
        {
            PhysicalKey.W => OrbitGlSurface.MoveDirection.Forward,
            PhysicalKey.S => OrbitGlSurface.MoveDirection.Back,
            PhysicalKey.A => OrbitGlSurface.MoveDirection.Left,
            PhysicalKey.D => OrbitGlSurface.MoveDirection.Right,
            _ => e.Key switch
            {
                Key.W => OrbitGlSurface.MoveDirection.Forward,
                Key.S => OrbitGlSurface.MoveDirection.Back,
                Key.A => OrbitGlSurface.MoveDirection.Left,
                Key.D => OrbitGlSurface.MoveDirection.Right,
                _ => OrbitGlSurface.MoveDirection.None
            }
        };

        if (direction == OrbitGlSurface.MoveDirection.None)
        {
            return false;
        }

        _surface.SetMove(direction, down);
        return true;
    }

    private void ClearMovement() => _surface.ClearMove();

    private sealed class OrbitGlSurface : OpenGlControlBase
    {
        private const float AxisLengthFactor = 0.08f;
        private const int GridLines = 20;
        private const int FloatsPerVertex = 7;

        private GL? _gl;
        private uint _program;
        private uint _vao;
        private uint _vbo;
        private int _mvpLocation;
        private int _faceVertexCount;
        private int _lineVertexCount;
        private float _yaw = 0.7f;
        private float _pitch = 0.55f;
        private float _distance = 40f;
        private Vector3 _eye = new(0f, 32f, 0f);
        private Vector3 _target = Vector3.Zero;
        private Vector3 _gridOrigin = Vector3.Zero;
        private float _gridHalfExtent = 10f;
        private float _gridStep = 1f;
        private bool _meshDirty = true;
        private OrbitScene? _scene;
        private bool _moveForward;
        private bool _moveBack;
        private bool _moveLeft;
        private bool _moveRight;

        public enum MoveDirection
        {
            None,
            Forward,
            Back,
            Left,
            Right
        }

        public OrbitScene? Scene
        {
            get => _scene;
            set
            {
                _scene = value;
                _meshDirty = true;
                FrameCamera(value);
                RequestNextFrameRendering();
            }
        }

        /// <summary>
        /// Places the orbit pivot in front of the camera (ground hit when possible) before a right-drag orbit.
        /// </summary>
        public void CaptureOrbitPivot() => PlaceOrbitPivotInFront();

        public void Orbit(float dx, float dy)
        {
            _yaw += dx * 0.005f;
            _pitch = Math.Clamp(_pitch + dy * 0.005f, 0.05f, MathF.PI / 2f - 0.05f);
            SyncEyeFromTarget();
        }

        /// <summary>
        /// Ground-plane pan of the camera; the orbit pivot is kept in front afterward.
        /// </summary>
        public void Pan(float dx, float dy)
        {
            var forward = new Vector3(-MathF.Cos(_yaw), 0f, -MathF.Sin(_yaw));
            var right = new Vector3(MathF.Sin(_yaw), 0f, -MathF.Cos(_yaw));
            // Grab-the-world: drag right moves the scene right (camera left).
            var scale = _distance * 0.0015f;
            _eye += right * (-dx) * scale + forward * dy * scale;
            PlaceOrbitPivotInFront();
        }

        public void Zoom(float factor)
        {
            _distance = Math.Clamp(_distance * factor, 10f, 400_000f);
            SyncEyeFromTarget();
        }

        public void SetMove(MoveDirection direction, bool down)
        {
            switch (direction)
            {
                case MoveDirection.Forward:
                    _moveForward = down;
                    break;
                case MoveDirection.Back:
                    _moveBack = down;
                    break;
                case MoveDirection.Left:
                    _moveLeft = down;
                    break;
                case MoveDirection.Right:
                    _moveRight = down;
                    break;
            }
        }

        public void ClearMove()
        {
            _moveForward = false;
            _moveBack = false;
            _moveLeft = false;
            _moveRight = false;
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

            ApplyMovement();

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

            var total = _faceVertexCount + _lineVertexCount;
            if (total == 0)
            {
                RequestNextFrameRendering();
                return;
            }

            var aspect = width / (float)height;
            var view = Matrix4x4.CreateLookAt(_eye, _target, Vector3.UnitY);
            var far = Math.Max(200f, _distance * 8f);
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, aspect, 1f, far);
            var mvp = view * projection;

            silk.UseProgram(_program);
            unsafe
            {
                silk.UniformMatrix4(_mvpLocation, 1, false, (float*)&mvp);
            }

            silk.BindVertexArray(_vao);

            if (_faceVertexCount > 0)
            {
                silk.Enable(EnableCap.Blend);
                silk.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                silk.DepthMask(false);
                silk.DrawArrays(PrimitiveType.Triangles, 0, (uint)_faceVertexCount);
                silk.DepthMask(true);
                silk.Disable(EnableCap.Blend);
            }

            if (_lineVertexCount > 0)
            {
                silk.DrawArrays(PrimitiveType.Lines, _faceVertexCount, (uint)_lineVertexCount);
            }

            silk.BindVertexArray(0);
            silk.UseProgram(0);

            RequestNextFrameRendering();
        }

        private void ApplyMovement()
        {
            if (!_moveForward && !_moveBack && !_moveLeft && !_moveRight)
            {
                return;
            }

            var look = LookDirection();
            var right = Vector3.Cross(look, Vector3.UnitY);
            if (right.LengthSquared() < 1e-8f)
            {
                right = new Vector3(MathF.Sin(_yaw), 0f, -MathF.Cos(_yaw));
            }
            else
            {
                right = Vector3.Normalize(right);
            }

            var step = _distance * 0.02f;
            var delta = Vector3.Zero;

            if (_moveForward)
            {
                delta += look;
            }

            if (_moveBack)
            {
                delta -= look;
            }

            if (_moveRight)
            {
                delta += right;
            }

            if (_moveLeft)
            {
                delta -= right;
            }

            if (delta.LengthSquared() < 1e-8f)
            {
                return;
            }

            _eye += Vector3.Normalize(delta) * step;
            PlaceOrbitPivotInFront();
        }

        private Vector3 LookDirection()
        {
            var cosPitch = MathF.Cos(_pitch);
            return Vector3.Normalize(new Vector3(
                -MathF.Cos(_yaw) * cosPitch,
                -MathF.Sin(_pitch),
                -MathF.Sin(_yaw) * cosPitch));
        }

        private void SyncEyeFromTarget() =>
            _eye = _target - LookDirection() * _distance;

        /// <summary>
        /// Keeps the orbit pivot in front of the camera: the ground under the view ray when it hits ahead,
        /// otherwise a point along the look direction at the current distance.
        /// </summary>
        private void PlaceOrbitPivotInFront()
        {
            var look = LookDirection();
            var groundY = _gridOrigin.Y;
            if (MathF.Abs(look.Y) > 1e-4f)
            {
                var distanceToGround = (groundY - _eye.Y) / look.Y;
                if (distanceToGround > 1f)
                {
                    var clamped = Math.Min(distanceToGround, Math.Max(_distance, 10f) * 20f);
                    _distance = Math.Clamp(clamped, 10f, 400_000f);
                    _target = _eye + look * _distance;
                    return;
                }
            }

            _distance = Math.Clamp(_distance, 10f, 400_000f);
            _target = _eye + look * _distance;
        }

        private void UploadMesh(GL silk)
        {
            var faces = _scene?.Faces ?? [];
            var lines = _scene?.Lines ?? [];
            if (_scene is null)
            {
                (faces, lines) = BuildEmptyGrid(_gridOrigin, _gridHalfExtent, _gridStep);
            }
            _faceVertexCount = faces.Length / FloatsPerVertex;
            _lineVertexCount = lines.Length / FloatsPerVertex;

            var combined = new float[faces.Length + lines.Length];
            faces.CopyTo(combined, 0);
            lines.CopyTo(combined, faces.Length);

            silk.BindVertexArray(_vao);
            silk.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            silk.BufferData<float>(
                BufferTargetARB.ArrayBuffer,
                combined.Length == 0 ? [0f] : combined,
                BufferUsageARB.DynamicDraw);

            const uint stride = FloatsPerVertex * sizeof(float);
            silk.EnableVertexAttribArray(0);
            unsafe
            {
                silk.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
                silk.EnableVertexAttribArray(1);
                silk.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
            }

            silk.BindVertexArray(0);
            silk.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        }

        private void FrameCamera(OrbitScene? scene)
        {
            if (scene is null)
            {
                _target = Vector3.Zero;
                _gridOrigin = Vector3.Zero;
                _distance = 40f;
                _gridHalfExtent = 10f;
                _gridStep = 1f;
                SyncEyeFromTarget();
                return;
            }

            var min = scene.Minimum;
            var max = scene.Maximum;
            _target = (min + max) * 0.5f;
            _gridOrigin = _target;

            var extent = max - min;
            var radius = MathF.Max(extent.X, MathF.Max(extent.Y, extent.Z)) * 0.6f;
            _distance = Math.Clamp(radius / MathF.Tan(MathF.PI / 8f), 5f, 200_000f);

            var ground = MathF.Max(extent.X, extent.Z);
            _gridHalfExtent = Math.Max(10f, ground * 0.55f);
            _gridStep = NiceStep(_gridHalfExtent / GridLines);
            SyncEyeFromTarget();
        }

        private static float NiceStep(float rough)
        {
            if (rough <= 0f)
            {
                return 1f;
            }

            var exponent = MathF.Floor(MathF.Log10(rough));
            var baseStep = MathF.Pow(10f, exponent);
            var fraction = rough / baseStep;
            var nice = fraction <= 1f ? 1f : fraction <= 2f ? 2f : fraction <= 5f ? 5f : 10f;
            return nice * baseStep;
        }

        private static (float[] Faces, float[] Lines) BuildEmptyGrid(
            Vector3 gridOrigin,
            float gridHalfExtent,
            float gridStep)
        {
            var lines = new List<float>(512);

            void AddVertex(Vector3 position, Vector4 color)
            {
                lines.Add(position.X);
                lines.Add(position.Y);
                lines.Add(position.Z);
                lines.Add(color.X);
                lines.Add(color.Y);
                lines.Add(color.Z);
                lines.Add(color.W);
            }

            void AddLine(Vector3 a, Vector3 b, Vector4 color)
            {
                AddVertex(a, color);
                AddVertex(b, color);
            }

            var gridColor = new Vector4(0.35f, 0.37f, 0.42f, 1f);
            var y = gridOrigin.Y;
            for (var i = -gridHalfExtent; i <= gridHalfExtent + gridStep * 0.01f; i += gridStep)
            {
                AddLine(
                    new Vector3(gridOrigin.X + i, y, gridOrigin.Z - gridHalfExtent),
                    new Vector3(gridOrigin.X + i, y, gridOrigin.Z + gridHalfExtent),
                    gridColor);
                AddLine(
                    new Vector3(gridOrigin.X - gridHalfExtent, y, gridOrigin.Z + i),
                    new Vector3(gridOrigin.X + gridHalfExtent, y, gridOrigin.Z + i),
                    gridColor);
            }

            var axis = Math.Max(gridStep * 2f, gridHalfExtent * AxisLengthFactor);
            AddLine(gridOrigin, gridOrigin + new Vector3(axis, 0f, 0f), new Vector4(0.9f, 0.2f, 0.2f, 1f));
            AddLine(gridOrigin, gridOrigin + new Vector3(0f, axis, 0f), new Vector4(0.2f, 0.85f, 0.3f, 1f));
            AddLine(gridOrigin, gridOrigin + new Vector3(0f, 0f, axis), new Vector4(0.25f, 0.45f, 0.95f, 1f));
            return ([], [.. lines]);
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
                layout (location = 1) in vec4 aColor;
                uniform mat4 uMvp;
                out vec4 vColor;
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
                in vec4 vColor;
                out vec4 FragColor;
                void main()
                {
                    FragColor = vColor;
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
}
