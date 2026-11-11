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
/// Draws the ground rectangles of a <c>*.vbatch</c> with a grid and RGB axes. Right-drag orbits, WASD moves the
/// look target, the wheel zooms. Game Z is up; the viewport remaps that to OpenGL Y-up.
/// </summary>
public sealed class BatchViewport : UserControl
{
    public static readonly StyledProperty<IReadOnlyList<BatchBoxDraw>?> BoxesProperty =
        AvaloniaProperty.Register<BatchViewport, IReadOnlyList<BatchBoxDraw>?>(nameof(Boxes));

    private readonly BatchGlSurface _surface = new() { IsHitTestVisible = false };
    private readonly Border _overlay;
    private TopLevel? _topLevel;
    private bool _pointerInside;
    private bool _orbiting;
    private Point _lastPointer;

    public IReadOnlyList<BatchBoxDraw>? Boxes
    {
        get => GetValue(BoxesProperty);
        set => SetValue(BoxesProperty, value);
    }

    static BatchViewport()
    {
        BoxesProperty.Changed.AddClassHandler<BatchViewport>((viewport, args) =>
        {
            viewport._surface.Boxes = args.NewValue as IReadOnlyList<BatchBoxDraw>;
        });
    }

    public BatchViewport()
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
        _overlay.PointerCaptureLost += (_, _) => _orbiting = false;
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

        _orbiting = true;
        _lastPointer = e.GetPosition(_overlay);
        e.Pointer.Capture(_overlay);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_orbiting)
        {
            return;
        }

        var position = e.GetPosition(_overlay);
        var dx = (float)(position.X - _lastPointer.X);
        var dy = (float)(position.Y - _lastPointer.Y);
        _lastPointer = position;
        _surface.Orbit(dx, dy);
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_orbiting)
        {
            return;
        }

        _orbiting = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _surface.Zoom(e.Delta.Y > 0 ? 0.9f : 1.1f);
        e.Handled = true;
    }

    private bool IsInputActive() =>
        _pointerInside || _orbiting || _overlay.IsFocused || IsFocused;

    private bool SetMoveKey(KeyEventArgs e, bool down)
    {
        var direction = e.PhysicalKey switch
        {
            PhysicalKey.W => BatchGlSurface.MoveDirection.Forward,
            PhysicalKey.S => BatchGlSurface.MoveDirection.Back,
            PhysicalKey.A => BatchGlSurface.MoveDirection.Left,
            PhysicalKey.D => BatchGlSurface.MoveDirection.Right,
            _ => e.Key switch
            {
                Key.W => BatchGlSurface.MoveDirection.Forward,
                Key.S => BatchGlSurface.MoveDirection.Back,
                Key.A => BatchGlSurface.MoveDirection.Left,
                Key.D => BatchGlSurface.MoveDirection.Right,
                _ => BatchGlSurface.MoveDirection.None
            }
        };

        if (direction == BatchGlSurface.MoveDirection.None)
        {
            return false;
        }

        _surface.SetMove(direction, down);
        return true;
    }

    private void ClearMovement() => _surface.ClearMove();

    private sealed class BatchGlSurface : OpenGlControlBase
    {
        private const float AxisLengthFactor = 0.08f;
        private const int GridLines = 20;

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
        private Vector3 _gridOrigin = Vector3.Zero;
        private float _gridHalfExtent = 10f;
        private float _gridStep = 1f;
        private bool _meshDirty = true;
        private IReadOnlyList<BatchBoxDraw>? _boxes;
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

        public IReadOnlyList<BatchBoxDraw>? Boxes
        {
            get => _boxes;
            set
            {
                _boxes = value;
                _meshDirty = true;
                FrameCamera();
                RequestNextFrameRendering();
            }
        }

        public void Orbit(float dx, float dy)
        {
            _yaw += dx * 0.005f;
            _pitch = Math.Clamp(_pitch + dy * 0.005f, 0.05f, MathF.PI / 2f - 0.05f);
        }

        public void Zoom(float factor) =>
            _distance = Math.Clamp(_distance * factor, 10f, 400_000f);

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

            if (_vertexCount == 0)
            {
                RequestNextFrameRendering();
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

        private void ApplyMovement()
        {
            if (!_moveForward && !_moveBack && !_moveLeft && !_moveRight)
            {
                return;
            }

            var forward = new Vector3(MathF.Cos(_yaw), 0f, MathF.Sin(_yaw));
            var right = new Vector3(-MathF.Sin(_yaw), 0f, MathF.Cos(_yaw));
            var step = _distance * 0.02f;
            var delta = Vector3.Zero;

            if (_moveForward)
            {
                delta += forward;
            }

            if (_moveBack)
            {
                delta -= forward;
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

            _target += Vector3.Normalize(delta) * step;
        }

        private void UploadMesh(GL silk)
        {
            var vertices = BuildLineMesh(_boxes, _gridOrigin, _gridHalfExtent, _gridStep);
            _vertexCount = vertices.Length / 6;

            silk.BindVertexArray(_vao);
            silk.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            silk.BufferData<float>(
                BufferTargetARB.ArrayBuffer,
                vertices.Length == 0 ? [0f] : vertices,
                BufferUsageARB.DynamicDraw);

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
            var boxes = _boxes;
            if (boxes is null || boxes.Count == 0)
            {
                _target = Vector3.Zero;
                _gridOrigin = Vector3.Zero;
                _distance = 40f;
                _gridHalfExtent = 10f;
                _gridStep = 1f;
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
            _gridOrigin = _target;
            var extent = max - min;
            var radius = MathF.Max(extent.X, MathF.Max(extent.Y, extent.Z)) * 0.6f;
            _distance = Math.Clamp(radius / MathF.Tan(MathF.PI / 8f), 20f, 200_000f);

            var ground = MathF.Max(extent.X, extent.Z);
            _gridHalfExtent = Math.Max(10f, ground * 0.55f);
            _gridStep = NiceStep(_gridHalfExtent / GridLines);
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

        private Vector3 CameraPosition()
        {
            var cosPitch = MathF.Cos(_pitch);
            return _target + new Vector3(
                _distance * MathF.Cos(_yaw) * cosPitch,
                _distance * MathF.Sin(_pitch),
                _distance * MathF.Sin(_yaw) * cosPitch);
        }

        private static float[] BuildLineMesh(
            IReadOnlyList<BatchBoxDraw>? boxes,
            Vector3 gridOrigin,
            float gridHalfExtent,
            float gridStep)
        {
            var lines = new List<float>(8192);

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
            AddLine(gridOrigin, gridOrigin + new Vector3(axis, 0f, 0f), new Vector3(0.9f, 0.2f, 0.2f));
            AddLine(gridOrigin, gridOrigin + new Vector3(0f, axis, 0f), new Vector3(0.2f, 0.85f, 0.3f));
            AddLine(gridOrigin, gridOrigin + new Vector3(0f, 0f, axis), new Vector3(0.25f, 0.45f, 0.95f));

            if (boxes is null)
            {
                return [.. lines];
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
}
