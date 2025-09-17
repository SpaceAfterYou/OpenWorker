using OpenWorker.Havok.Attributes;

namespace OpenWorker.Havok;

[HavokSerializeClass("hkcdStaticTreeDynamicStoragehkcdStaticTreeCodec3Axis6", 0)]
public class hkcdStaticTreeDynamicStoragehkcdStaticTreeCodec3Axis6;

[HavokSerializeClass("hkcdStaticTreeCodec3Axis", 0)]
public class hkcdStaticTreeCodec3Axis;

[HavokSerializeClass("hkcdStaticTreeCodec3Axis6", 0)]
public class hkcdStaticTreeCodec3Axis6 : hkcdStaticTreeCodec3Axis;

[HavokSerializeClass("hkcdStaticTreeDynamicStorage6", 0)]
public class hkcdStaticTreeDynamicStorage6 : hkcdStaticTreeDynamicStoragehkcdStaticTreeCodec3Axis6;

[HavokSerializeClass("hkcdStaticTreeTreehkcdStaticTreeDynamicStorage6", 0)]
public class hkcdStaticTreeTreehkcdStaticTreeDynamicStorage6 : hkcdStaticTreeDynamicStorage6;

[HavokSerializeClass("hkAabb", 0)]
public class hkAabb;

[HavokSerializeClass("hkcdStaticTreeDefaultTreeStorage6", 0)]
public class hkcdStaticTreeDefaultTreeStorage6 : hkcdStaticTreeTreehkcdStaticTreeDynamicStorage6;

[HavokSerializeClass("hkaiStaticTree", 14)]
public class hkaiStaticTree : HavokReferencedObject;

[HavokSerializeClass("hkaiStreamingSet", 6)]
public class hkaiStreamingSet;

[HavokSerializeClass("hkaiNavMeshEdge", 8)]
public class hkaiNavMeshEdge;

[HavokSerializeClass("hkaiNavMeshFace", 0)]
public class hkaiNavMeshFace;

[HavokSerializeClass("hkaiStreamingSetVolumeConnection", 0)]
public class hkaiStreamingSetVolumeConnection;

[HavokSerializeClass("hkaiStreamingSetGraphConnection", 0)]
public class hkaiStreamingSetGraphConnection;

[HavokSerializeClass("hkaiStreamingSetNavMeshConnection", 0)]
public class hkaiStreamingSetNavMeshConnection;
