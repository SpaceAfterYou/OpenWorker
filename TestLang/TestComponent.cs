using System.Diagnostics;

namespace TestLang;

[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer
{
    private int _element0;
}

public struct TestBufferArray
{
    [DebuggerDisplay("{Buffer.Length}")]
    public Buffer Buffer;
}

public record TestComponent
{
    public int A;

    public void Test()
    {
        var buffer = new Buffer();
        for (int i = 0; i < 10; i++)
        {
            buffer[i] = i;
        }
        
        var testBuffer = new TestBufferArray();
        
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine(buffer[i]);
        }

        return;
    }
}