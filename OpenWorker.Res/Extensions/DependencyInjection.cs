using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Res.Attributes;

namespace OpenWorker.Res.Extensions;

public static class DependencyInjection
{
    public static void AddRes(this IServiceCollection services)
    {
        var types = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(e => e.GetTypes())
            .Where(e => e.GetCustomAttribute<BinaryResourceTableAttribute>() is not null);

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<BinaryResourceTableAttribute>();
            Debug.Assert(attribute is not null);

            var path = Path.Join("res", $"{attribute.Table}.res");
            var buffer = File.ReadAllBytes(path);

            using var stream = new MemoryStream(buffer);
            using var reader = new BinaryReader(stream);

            var count = reader.ReadInt32();
            var array = Array.CreateInstance(type, count);

            foreach (var index in Enumerable.Range(0, count))
            {
                var value = Activator.CreateInstance(type, reader);
                Debug.Assert(value is not null);

                array.SetValue(value, index);
            }

            var collectionType = typeof(ReadOnlyCollection<>).MakeGenericType(type);

            var values = Activator.CreateInstance(collectionType, array);
            Debug.Assert(values is not null);

            services.AddSingleton(collectionType, values);
        }

        stopWatch.Stop();
        Debug.WriteLine($"Res loaded in {stopWatch.ElapsedMilliseconds} ms");
    }
}