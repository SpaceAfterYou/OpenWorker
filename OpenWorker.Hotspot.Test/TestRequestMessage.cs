using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class TestRequestMessage
{
    public TestContext TestContext { get; set; }
    
    [TestMethod]
    public void TestMessageIntegrity()
    {
        var files = Directory.GetFiles(@"..\..\..\Datas", "*.bin", SearchOption.AllDirectories);

        var types = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(x => x.GetCustomAttribute<HotspotMessageAttribute>() is not null)
            .Where(x =>
            {
                Assert.IsNotNull(x.Namespace);
                return x.Namespace.Contains("Request");
            })
            .ToImmutableArray();
        
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);

            var ids = name
                .Split(',')
                .Select(x => x.Trim())
                .Select(x => Convert.ToByte(x, 16))
                .ToArray();

            var opcode = new MessageOpcode(ids[0], ids[1]);
            
            var type = types.FirstOrDefault(x =>
            {
                var attribute = x.GetCustomAttribute<HotspotMessageAttribute>();
                Assert.IsNotNull(attribute);
                
                return attribute.Opcode == opcode;
            });
            
            if (type is null)
            {
                Trace.WriteLine($"Missing {opcode}");
            }

            else
            {
                Trace.WriteLine($"{opcode} {type.Name}");
                
                var stream = new MemoryStream(File.ReadAllBytes(file));
                var reader = new BinaryReader(stream);
                
                // Skip header
                _ = new MessageHeader(reader);
                
                // Skip opcode
                _ = reader.ReadInt16();
                
                var instance = Activator.CreateInstance(type, reader);
                
                Assert.IsNotNull(instance, $"{opcode} {type.Name}");
                Assert.AreEqual(reader.BaseStream.Position, reader.BaseStream.Length, $"{opcode} {type.Name} | {reader.BaseStream.Position} != {reader.BaseStream.Length}");
            }
        }
    }
}