using System.Numerics;

namespace OpenWorker.Content.Tool.Vision;

/// <summary>
/// Object serializers for Vision zone archives. Only static-mesh fields are kept;
/// other classes are consumed so the stream stays aligned.
/// </summary>
internal static class VArchiveSerializers
{
    public static Dictionary<string, Action<VArchiveReader, ArchiveObject>> Build()
    {
        var map = new Dictionary<string, Action<VArchiveReader, ArchiveObject>>(StringComparer.Ordinal)
        {
            ["VisStaticMeshInstance_cl"] = SerializeStaticMeshInstance,
            ["VisLightSource_cl"] = SerializeLightSource,
            ["VCoronaComponent"] = SerializeCoronaComponent,
            ["VStaticMeshAlphaController"] = SerializeStaticMeshAlphaController,
            ["VolumetricCone_cl"] = SerializeVolumetricCone,
            ["VisParticleEffect_cl"] = SerializeParticleEffect,
            ["VisVisibilityObjectAABox_cl"] = SerializeVisibilityObject,
            ["VSurfaceTextureSetSerializationProxy"] = SerializeSurfaceTextureSetProxy,
            ["VProjectedWallmark"] = SerializeProjectedWallmark,
            ["VisVisibilityZoneProxy_cl"] = SerializeVisibilityZoneProxy,
            ["VisVisibilityZone_cl"] = SerializeVisibilityZone,
            ["VShadowMapComponentSpotDirectional"] = SerializeShadowMapSpot,
            ["VModelSerializationProxy"] = SerializeModelProxy,
            ["VSequenceSetSerializationProxy"] = SerializeSequenceSetProxy,
            ["VTextureSerializationProxy"] = SerializeTextureProxy,
        };

        return map;
    }

    public static HashSet<string> Object3dClasses { get; } = new(StringComparer.Ordinal)
    {
        "VisObject3D_cl",
        "VisLightSource_cl",
        "VolumetricCone_cl",
        "VisParticleEffect_cl",
        "VisVisibilityObjectAABox_cl",
        "VProjectedWallmark",
        "ParticleGroupBase_cl",
    };

    private static void SerializeStaticMeshInstance(VArchiveReader ar, ArchiveObject obj)
    {
        var mesh = (StaticMeshArchiveObject)obj;
        SerializeTypedEngineObject(ar, obj);

        var version = ar.ReadUInt8();
        mesh.Instance.Path = ar.ReadStringBinary();

        if (version < 3)
        {
            mesh.UniqueId = ar.ReadInt64();
        }

        mesh.Instance.Matrix = ar.ReadMat4();
        _ = ar.ReadBBoxVis();
        _ = ar.ReadVec4();

        if (ar.LoadingVersion >= 25)
        {
            mesh.ObjectKey = ar.ReadVString();
        }

        if (version >= 9)
        {
            _ = ar.ReadInt32(); // visibility level
        }

        if (version >= 1)
        {
            _ = version < 5 ? ar.ReadUInt16() : ar.ReadUInt32(); // light mask
        }

        if (version >= 9)
        {
            _ = version >= 10 ? ar.ReadUInt32() : ar.ReadUInt8(); // collision behavior
        }

        if (version >= 10)
        {
            _ = ar.ReadUInt8(); // physics hint
        }

        var geometryCount = ar.ReadInt32();
        var masks = new List<uint>(geometryCount);
        for (var i = 0; i < geometryCount; i++)
        {
            masks.Add(SerializeStaticGeometryInstance(ar));
        }

        mesh.Instance.SetGeometryMasks(masks);

        if (version >= 4)
        {
            if (version < 8)
            {
                if (ar.ReadBool())
                {
                    throw new InvalidDataException("Inline surface texture set is not supported.");
                }
            }
            else
            {
                _ = ar.ReadProxyObject();
            }
        }
    }

    private static uint SerializeStaticGeometryInstance(VArchiveReader ar)
    {
        var version = ar.ReadUInt8();
        uint visibleMask = 0;

        if (version < 8)
        {
            _ = ar.ReadFloat();
            if (version >= 4)
            {
                _ = ar.ReadFloat();
                _ = ar.ReadVec3();
                _ = ar.ReadInt32();
            }
        }
        else
        {
            visibleMask = SerializeVisibilityData(ar);
        }

        _ = ar.ReadInt32(); // geometry type

        if (version < 8)
        {
            _ = ar.ReadBBoxVis();
        }

        _ = ar.ReadUInt32(); // light mask

        if (version < 8)
        {
            _ = ar.ReadUInt32();
            _ = ar.ReadUInt32();
        }

        _ = ar.ReadBool();
        _ = ar.ReadInt32();

        if (version != 6)
        {
            _ = ar.ReadInt64();
        }

        _ = ar.ReadBool();

        if (version < 3)
        {
            if (version < 2)
            {
                _ = ar.ReadObject();
            }
            else
            {
                ReadZoneExchange(ar);
            }
        }
        else
        {
            var zoneCount = ar.ReadUInt16();
            for (var i = 0; i < zoneCount; i++)
            {
                ReadZoneExchange(ar);
            }
        }

        _ = ar.ReadVec4();
        for (var i = 0; i < 4; i++)
        {
            _ = ar.ReadStringBinary();
        }

        if (version >= 1)
        {
            var shadowCount = ar.ReadInt16();
            for (var i = 0; i < shadowCount; i++)
            {
                var smVersion = ar.ReadUInt8();
                var byId = smVersion >= 1 && ar.ReadBool();
                if (byId)
                {
                    _ = ar.ReadInt64();
                }
                else
                {
                    _ = ar.ReadObject();
                }

                _ = ar.ReadVec4();
                _ = ar.ReadStringBinary();
            }
        }

        if (version >= 5)
        {
            _ = ar.ReadObject();
        }

        return visibleMask;
    }

    private static uint SerializeVisibilityData(VArchiveReader ar)
    {
        _ = ar.ReadUInt8();
        _ = ar.ReadBBoxX();
        _ = ar.ReadVec3();
        var visibleMask = ar.ReadUInt32();
        _ = ar.ReadFloat();
        _ = ar.ReadInt32();
        _ = ar.ReadFloat();
        return visibleMask;
    }

    private static void ReadZoneExchange(VArchiveReader ar)
    {
        if (ar.ReadBool())
        {
            _ = ar.ReadProxyObject();
        }
        else
        {
            _ = ar.ReadObject();
        }
    }

    private static int SerializeTypedEngineObject(VArchiveReader ar, ArchiveObject obj)
    {
        if (ar.LoadingVersion < 28)
        {
            return -1;
        }

        var localVersion = ar.ReadUInt8();
        if (localVersion >= 1)
        {
            SerializeComponents(ar, obj);
        }

        if (localVersion >= 2)
        {
            obj.UniqueId = ar.ReadInt64();
        }

        return localVersion;
    }

    private static void SerializeComponents(VArchiveReader ar, ArchiveObject owner)
    {
        var version = ar.ReadUInt8();
        var count = ar.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            var mode = version >= 1 ? ar.ReadUInt8() : (byte)0;
            if (mode == 0)
            {
                var component = ar.ReadObject();
                if (component is not null)
                {
                    owner.Components.Add(component);
                }
            }
            else
            {
                _ = ar.ReadObject();
                _ = ar.ReadInt32();
                throw new InvalidDataException("Component serialize-mode 1 is not supported.");
            }
        }
    }

    private static void SerializeObjectComponentBase(VArchiveReader ar, ArchiveObject obj)
    {
        SerializeTypedEngineObject(ar, obj);
        var version = ar.ReadUInt8();
        _ = ar.ReadObject();
        _ = ar.ReadInt32();
        if (version >= 1)
        {
            if (ar.ReadBool())
            {
                _ = ar.ReadStringBinary();
            }
            else
            {
                _ = ar.ReadInt32();
            }
        }
    }

    private static void SerializeObject3d(VArchiveReader ar, ArchiveObject obj)
    {
        if (ar.LoadingVersion >= 12)
        {
            SerializeTypedEngineObject(ar, obj);
        }

        var localVersion = 0;
        if (ar.LoadingVersion >= 21)
        {
            localVersion = ar.ReadUInt8();
            if (localVersion is >= 1 and <= 5)
            {
                obj.UniqueId = ar.ReadInt64();
            }
        }

        _ = ar.ReadVisVector(); // position
        _ = ar.ReadVisVector(); // orientation
        _ = ar.ReadVisVector();
        if (localVersion >= 5)
        {
            _ = ar.ReadVisVector();
        }

        _ = ar.ReadVisVector();

        if (ar.LoadingVersion >= 25)
        {
            obj.ObjectKey = ar.ReadVString();
        }

        if (localVersion >= 7)
        {
            _ = ar.ReadInt32();
        }

        if (ar.LoadingVersion < 12)
        {
            _ = ar.ReadVec3();
        }

        if (localVersion < 1)
        {
            _ = ar.ReadInt32();
        }

        if (ar.LoadingVersion >= 7)
        {
            var flags = ar.ReadUInt32();
            if ((flags & 0x20) == 0)
            {
                _ = ar.ReadMat3();
            }
        }

        if (ar.LoadingVersion >= 10)
        {
            _ = ar.ReadObject();
        }

        if (localVersion >= 2 && ar.LoadingVersion < 28)
        {
            SerializeComponents(ar, obj);
        }

        if (localVersion >= 4 && ar.ReadBool())
        {
            _ = ar.ReadUInt8();
            _ = ar.ReadObject();
            _ = ar.ReadBool();
            _ = ar.ReadVisVector();
            _ = ar.ReadFloat();
            var zones = ar.ReadInt16();
            for (var i = 0; i < zones; i++)
            {
                ReadZoneExchange(ar);
            }
        }
    }

    private static void SerializeLightSource(VArchiveReader ar, ArchiveObject obj)
    {
        if (ar.LoadingVersion < 12)
        {
            SerializeTypedEngineObject(ar, obj);
        }
        else
        {
            SerializeObject3d(ar, obj);
        }

        var lightVersion = 0;
        if (ar.LoadingVersion >= 21)
        {
            lightVersion = ar.ReadUInt8();
        }

        if (ar.LoadingVersion < 12)
        {
            _ = ar.ReadUInt16();
            _ = ar.ReadBool();
            _ = ar.ReadVec4();
            _ = ar.ReadMat3();
            _ = ar.ReadMat3();
        }

        _ = ar.ReadFloat();
        _ = ar.ReadInt32();
        _ = ar.ReadUInt16();

        if (ar.LoadingVersion < 12)
        {
            _ = ar.ReadVec4();
            _ = ar.ReadVec4();
        }

        if (ar.LoadingVersion < 25)
        {
            obj.ObjectKey = ar.ReadVString();
        }

        _ = ar.ReadInt32();
        _ = ar.ReadBool();

        if (ar.LoadingVersion < 12)
        {
            _ = ar.ReadStringBinary();
        }

        _ = ar.ReadBool();
        _ = ar.ReadInt32();
        _ = ar.ReadColorRef();

        if (lightVersion >= 1)
        {
            _ = ar.ReadFloat();
            _ = ar.ReadFloat();
        }

        _ = ar.ReadStringBinary();

        if (lightVersion < 7)
        {
            _ = ar.ReadStringBinary();
        }

        if (lightVersion >= 2)
        {
            _ = ar.ReadUInt32();
        }

        var flags = ar.ReadInt32();
        if ((flags & 1) != 0)
        {
            _ = ar.ReadInt32();
            _ = ar.ReadInt32();
            _ = ar.ReadInt32();
            for (var i = 0; i < 7; i++)
            {
                _ = ar.ReadFloat();
            }
        }

        if ((flags & 2) != 0)
        {
            var typePacked = ar.ReadInt32();
            _ = ar.ReadInt32();
            _ = ar.ReadInt32();
            _ = ar.ReadInt32();
            _ = ar.ReadInt32();
            for (var i = 0; i < 3; i++)
            {
                _ = ar.ReadInt32();
            }

            for (var i = 0; i < 9; i++)
            {
                _ = ar.ReadInt32();
            }

            for (var i = 0; i < 6; i++)
            {
                _ = ar.ReadFloat();
            }

            if (ar.LoadingVersion >= 6)
            {
                _ = ar.ReadVString();
                _ = ar.ReadFloat();
                _ = ar.ReadFloat();
                _ = ar.ReadFloat();
                for (var i = 0; i < 3; i++)
                {
                    _ = ar.ReadUInt8();
                }
            }

            if ((typePacked >> 8) >= 1)
            {
                _ = ar.ReadBool();
            }
        }

        if (ar.LoadingVersion < 12)
        {
            _ = ar.ReadObject();
        }

        _ = ar.ReadColorRef();

        if (lightVersion < 7)
        {
            _ = ar.ReadUInt8();
            _ = ar.ReadUInt8();
        }

        _ = ar.ReadUInt16();

        if (lightVersion == 6)
        {
            _ = ar.ReadUInt16();
            _ = ar.ReadUInt8();
            _ = ar.ReadFloat();
        }

        if (ar.LoadingVersion < 11)
        {
            _ = ar.ReadUInt16();
            _ = ar.ReadUInt16();
        }
        else
        {
            _ = ar.ReadUInt32();
            _ = ar.ReadUInt32();
        }

        _ = ar.ReadUInt8();

        if (ar.LoadingVersion >= 20)
        {
            _ = ar.ReadInt32();
            _ = ar.ReadFloat();
            _ = ar.ReadStringBinary();
        }

        if (lightVersion >= 3)
        {
            _ = ar.ReadInt32();
        }

        if (lightVersion >= 4)
        {
            _ = ar.ReadUInt32();
        }

        if (lightVersion >= 5)
        {
            _ = ar.ReadFloat();
            _ = ar.ReadFloat();
        }

        if (lightVersion >= 8)
        {
            _ = ar.ReadBool();
        }
    }

    private static void SerializeCoronaComponent(VArchiveReader ar, ArchiveObject obj)
    {
        SerializeObjectComponentBase(ar, obj);
        var version = ar.ReadInt8();
        _ = ar.ReadStringBinary();
        _ = ar.ReadFloat();
        _ = ar.ReadUInt32();
        _ = ar.ReadUInt32();
        _ = ar.ReadFloat();
        _ = ar.ReadFloat();
        _ = ar.ReadFloat();
        _ = ar.ReadUInt8();
        _ = ar.ReadUInt8();
        _ = ar.ReadInt32();
        if (version >= 1)
        {
            _ = ar.ReadUInt32();
        }

        if (version >= 2)
        {
            _ = ar.ReadInt32();
        }
    }

    private static void SerializeStaticMeshAlphaController(VArchiveReader ar, ArchiveObject obj) =>
        _ = ar.ReadFloat();

    private static void SerializeVolumetricCone(VArchiveReader ar, ArchiveObject obj)
    {
        SerializeObject3d(ar, obj);
        var version = ar.ReadInt32();
        _ = ar.ReadColorRef();
        for (var i = 0; i < 5; i++)
        {
            _ = ar.ReadFloat();
        }

        _ = ar.ReadObject();
        _ = ar.ReadUInt8();
        _ = ar.ReadUInt8();
        _ = ar.ReadUInt8();
        _ = ar.ReadFloat();
        if (version >= 1)
        {
            _ = ar.ReadUInt32();
        }

        if (version >= 2)
        {
            _ = ar.ReadFloat();
        }
    }

    private static void SerializeParticleEffect(VArchiveReader ar, ArchiveObject obj)
    {
        SerializeObject3d(ar, obj);
        var version = ar.ReadInt32();
        var fileMode = version >= 1 ? ar.ReadUInt8() : (byte)0;
        if (version >= 4)
        {
            _ = ar.ReadUInt32();
        }

        if (fileMode == 0)
        {
            _ = ar.ReadStringBinary();
        }
        else if (fileMode == 1)
        {
            _ = ar.ReadObject();
        }

        if (version >= 1 && ar.LoadingVersion < 25)
        {
            _ = ar.ReadVString();
        }

        if (version >= 3)
        {
            _ = ar.ReadBool();
            _ = ar.ReadBool();
        }

        var groupCount = ar.ReadInt32();
        for (var i = 0; i < groupCount; i++)
        {
            var present = version < 2 || ar.ReadBool();
            if (present)
            {
                SerializeParticleGroup(ar);
            }
        }
    }

    private static void SerializeParticleGroup(VArchiveReader ar)
    {
        SerializeObject3d(ar, new ArchiveObject("ParticleGroupBase_cl"));
        var version = ar.ReadInt32();
        _ = ar.ReadFloat();
        if (version >= 2)
        {
            _ = ar.ReadColorRef();
        }

        if (version >= 3)
        {
            _ = ar.ReadUInt32();
        }

        if (version >= 4)
        {
            _ = ar.ReadVisVector();
        }

        if (version >= 5)
        {
            _ = ar.ReadBool();
        }

        if (version >= 6)
        {
            _ = ar.ReadBool();
        }

        if (version >= 7)
        {
            _ = ar.ReadObject();
        }

        var constraintCount = ar.ReadInt32();
        for (var i = 0; i < constraintCount; i++)
        {
            _ = ar.ReadObject();
            _ = ar.ReadInt32();
        }
    }

    private static void SerializeVisibilityObject(VArchiveReader ar, ArchiveObject obj)
    {
        SerializeObject3d(ar, obj);
        var version = ar.ReadUInt8();
        if (version >= 3)
        {
            _ = SerializeVisibilityData(ar);
        }

        _ = ar.ReadInt32();
        _ = ar.ReadBool();
        if (version >= 2)
        {
            _ = ar.ReadBool();
        }

        _ = ar.ReadBBoxVis();
        if (version < 3)
        {
            _ = ar.ReadBBoxVis();
        }

        _ = ar.ReadUInt8();
        if (version < 3)
        {
            _ = ar.ReadFloat();
        }

        _ = ar.ReadInt32();
    }

    private static void SerializeSurfaceTextureSetProxy(VArchiveReader ar, ArchiveObject obj)
    {
        var version = ar.ReadUInt8();
        var count = ar.ReadInt16();
        var asSurfaces = version >= 1 && ar.ReadBool();
        for (var i = 0; i < count; i++)
        {
            if (asSurfaces)
            {
                SerializeSurface(ar);
            }
            else
            {
                SerializeSurfaceTextures(ar);
            }
        }
    }

    private static void SerializeSurfaceTextures(VArchiveReader ar)
    {
        _ = ar.ReadUInt8();
        _ = ar.ReadVec4();
        for (var i = 0; i < 3; i++)
        {
            SerializeTextureExchange(ar);
        }

        for (var i = 0; i < 4; i++)
        {
            SerializeTextureExchange(ar);
        }

        var aux = ar.ReadInt16();
        for (var i = 0; i < aux; i++)
        {
            SerializeTextureExchange(ar);
        }
    }

    private static void SerializeTextureExchange(VArchiveReader ar)
    {
        if (ar.ReadBool())
        {
            _ = ar.ReadUInt16();
            _ = ar.ReadStringBinary();
        }
    }

    private static void SerializeSurface(VArchiveReader ar)
    {
        SerializeSurfaceTextures(ar);
        var version = ar.ReadUInt8();
        _ = ar.ReadVString();
        _ = ar.ReadInt32();
        _ = ar.ReadColorRef();
        _ = ar.ReadUInt8();
        _ = ar.ReadUInt8();
        _ = ar.ReadUInt8();
        _ = ar.ReadUInt8();
        _ = ar.ReadBool();
        if (version < 1)
        {
            _ = ar.ReadBool();
        }
        else
        {
            _ = ar.ReadUInt8();
        }

        _ = ar.ReadBool();
        _ = ar.ReadBool();
        for (var i = 0; i < 8; i++)
        {
            _ = ar.ReadFloat();
        }

        _ = ar.ReadInt32();
        _ = ar.ReadVString();
        _ = ar.ReadUInt8();
        ar.ReadEffectConfig();
        if (version >= 2)
        {
            _ = ar.ReadUInt8();
        }
    }

    private static void SerializeProjectedWallmark(VArchiveReader ar, ArchiveObject obj)
    {
        var version = ar.ReadInt32();
        for (var i = 0; i < 5; i++)
        {
            _ = ar.ReadVisVector();
        }

        _ = ar.ReadFloat();
        _ = ar.ReadStringBinary();
        _ = ar.ReadColorRef();
        _ = ar.ReadUInt32();
        _ = ar.ReadFloat();
        _ = ar.ReadFloat();
        if (version >= 1)
        {
            _ = ar.ReadUInt32();
        }

        if (version >= 2)
        {
            _ = ar.ReadBool();
        }

        if (version is >= 3 and <= 7)
        {
            _ = ar.ReadInt64();
        }

        if (version >= 4)
        {
            _ = ar.ReadFloat();
        }

        if (version >= 5)
        {
            _ = ar.ReadFloat();
        }

        if (version >= 6)
        {
            _ = ar.ReadUInt32();
        }

        if (version >= 7)
        {
            ar.ReadEffectConfig();
        }

        if (version >= 9)
        {
            _ = ar.ReadInt32();
        }
    }

    private static void SerializeVisibilityZoneProxy(VArchiveReader ar, ArchiveObject obj)
    {
        _ = ar.ReadUInt8();
        _ = ar.ReadInt64();
    }

    private static void SerializeVisibilityZone(VArchiveReader ar, ArchiveObject obj)
    {
        SerializeTypedEngineObject(ar, obj);
        var version = ar.ReadUInt8();
        if (version is >= 2 and <= 3)
        {
            _ = ar.ReadInt64();
        }

        var flags = ar.ReadUInt32();
        _ = ar.ReadUInt32();
        _ = ar.ReadBBoxVis();
        if (version >= 3)
        {
            _ = ar.ReadBBoxVis();
        }

        _ = ar.ReadColorRef();
        if ((flags & 1) != 0)
        {
            var portals = ar.ReadUInt16();
            if (portals != 0)
            {
                throw new InvalidDataException("VisPortal objects are not supported.");
            }
        }

        if ((flags & 2) != 0)
        {
            var count = ar.ReadUInt16();
            for (var i = 0; i < count; i++)
            {
                _ = ar.ReadObject();
            }
        }

        if ((flags & 0x100) != 0)
        {
            var count = ar.ReadUInt16();
            for (var i = 0; i < count; i++)
            {
                _ = ar.ReadUInt32();
            }
        }
    }

    private static void SerializeShadowMapSpot(VArchiveReader ar, ArchiveObject obj)
    {
        SerializeObjectComponentBase(ar, obj);
        var localVersion = ar.ReadUInt8();
        if (localVersion > 0)
        {
            _ = ar.ReadInt32();
            if (localVersion >= 2)
            {
                _ = ar.ReadInt32();
            }

            _ = ar.ReadUInt32();
            _ = ar.ReadInt32();
            if (localVersion < 5)
            {
                _ = ar.ReadInt32();
            }

            _ = ar.ReadInt32();
            _ = ar.ReadFloat();
            if (localVersion >= 6)
            {
                _ = ar.ReadFloat();
            }

            _ = ar.ReadInt32();
            if (localVersion < 5)
            {
                _ = ar.ReadInt32();
            }

            for (var i = 0; i < 4; i++)
            {
                _ = ar.ReadFloat();
            }

            for (var i = 0; i < 4; i++)
            {
                _ = ar.ReadFloat();
            }

            _ = ar.ReadUInt32();
            _ = ar.ReadFloat();
            _ = ar.ReadColorRef();
            _ = ar.ReadFloat();
            if (localVersion >= 3)
            {
                _ = ar.ReadInt32();
            }
        }

        _ = ar.ReadUInt32();
        if (localVersion >= 4)
        {
            _ = ar.ReadInt32();
        }

        for (var i = 0; i < 4; i++)
        {
            _ = ar.ReadFloat();
        }

        _ = ar.ReadInt32();
        _ = ar.ReadFloat();
        _ = ar.ReadFloat();
        if (localVersion >= 4)
        {
            _ = ar.ReadFloat();
            _ = ar.ReadFloat();
        }
    }

    private static void SerializeModelProxy(VArchiveReader ar, ArchiveObject obj)
    {
        _ = ar.ReadStringBinary();
        var count = ar.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            _ = ar.ReadProxyObject();
        }
    }

    private static void SerializeSequenceSetProxy(VArchiveReader ar, ArchiveObject obj) =>
        _ = ar.ReadStringBinary();

    private static void SerializeTextureProxy(VArchiveReader ar, ArchiveObject obj)
    {
        _ = ar.ReadStringBinary();
        _ = ar.ReadUInt32();
    }
}
