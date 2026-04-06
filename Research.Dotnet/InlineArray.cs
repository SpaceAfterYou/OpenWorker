using System.Diagnostics;

namespace Research.Dotnet;

[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer
{
    private int _element0;
}

public struct TestBufferArray()
{
    [DebuggerDisplay("{Buffer.Length}")]
    public Buffer Buffer = default;
}

public record TestComponent
{
    public void Test()
    {
        var buffer = new Buffer();
        
        for (var i = 0; i < 10; i++)
        {
            buffer[i] = i;
        }

        var testBuffer = new TestBufferArray();

        for (var i = 0; i < 10; i++)
        {
            Console.WriteLine(testBuffer.Buffer[i]);
        }
    }
}