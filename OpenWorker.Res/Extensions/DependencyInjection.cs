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

            var path = attribute.PathIn();

            if (path is null)
            {
                services.AddSingleton(typeof(ReadOnlyCollection<>).MakeGenericType(type), BinaryResourceTables.EmptyCollection(type));
                continue;
            }

            var array = BinaryResourceTables.ReadArray(type, path);
            var values = BinaryResourceTables.WrapCollection(type, array);

            services.AddSingleton(typeof(ReadOnlyCollection<>).MakeGenericType(type), values);
        }

        stopWatch.Stop();
        Debug.WriteLine($"Res loaded in {stopWatch.ElapsedMilliseconds} ms");
    }
}
