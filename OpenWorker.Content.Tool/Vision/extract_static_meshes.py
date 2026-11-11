#!/usr/bin/env python3
"""Dump Vision static-mesh instances from a .vscene (and its .vzone sidecars).

Writes one TSV line per instance:
  collision_only\\tpath\\tm00\\tm01\\t...\\tm15

Requires OPENWORKER_VISION_ADDON = path to blender-io_soulworker (the folder that
contains io_soulworker/). bpy is stubbed; only the archive parsers are used.
"""
from __future__ import annotations

import os
import sys
import types
from pathlib import Path


def _stub_modules() -> None:
    bpy = types.ModuleType("bpy")
    bpy.types = types.SimpleNamespace(
        ArmatureModifier=object,
        Collection=object,
        Context=object,
        Object=object,
        Material=object,
        Mesh=object,
        ShaderNodeBsdfPrincipled=object,
        ShaderNodeTexImage=object,
        VertexGroup=object,
    )
    sys.modules["bpy"] = bpy
    sys.modules["bpy.types"] = bpy.types

    class Vector(tuple):
        def __new__(cls, xyz=(0.0, 0.0, 0.0)):
            values = tuple(xyz)
            if len(values) >= 3:
                values = values[:3]
            return tuple.__new__(cls, values)

        def __init__(self, xyz=(0.0, 0.0, 0.0)) -> None:
            pass

        @property
        def x(self):
            return self[0]

        @property
        def y(self):
            return self[1]

        @property
        def z(self):
            return self[2]

    class Matrix:
        def __init__(self, rows=None) -> None:
            if rows is None:
                self.rows = [
                    (1.0, 0.0, 0.0, 0.0),
                    (0.0, 1.0, 0.0, 0.0),
                    (0.0, 0.0, 1.0, 0.0),
                    (0.0, 0.0, 0.0, 1.0),
                ]
            else:
                self.rows = [tuple(float(v) for v in row) for row in rows]

        @staticmethod
        def Identity(_n: int) -> "Matrix":
            return Matrix()

        def __getitem__(self, index: int):
            return self.rows[index]

    mathutils = types.ModuleType("mathutils")
    mathutils.Vector = Vector
    mathutils.Matrix = Matrix
    sys.modules["mathutils"] = mathutils


def _load_addon(addon_root: Path) -> None:
    package = types.ModuleType("io_soulworker")
    package.__path__ = [str(addon_root / "io_soulworker")]
    sys.modules["io_soulworker"] = package
    sys.path.insert(0, str(addon_root))


def _zone_files(scene_path: Path) -> list[Path]:
    data_dir = scene_path.with_suffix(scene_path.suffix + "_data")
    if not data_dir.is_dir():
        return []
    return sorted(path for path in data_dir.glob("*.vzone") if path.is_file())


def _emit(instance) -> None:
    collision = 1 if instance.is_collision_only else 0
    path = instance.path.replace("/", "\\")
    floats: list[str] = []
    for row in range(4):
        for col in range(4):
            floats.append(f"{float(instance.matrix[row][col]):.9g}")
    print(f"{collision}\t{path}\t" + "\t".join(floats))


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: extract_static_meshes.py <scene.vscene>", file=sys.stderr)
        return 2

    scene_path = Path(sys.argv[1])
    if not scene_path.is_file():
        print(f"missing scene: {scene_path}", file=sys.stderr)
        return 1

    addon = os.environ.get("OPENWORKER_VISION_ADDON", "").strip()
    if not addon:
        print("OPENWORKER_VISION_ADDON is not set", file=sys.stderr)
        return 1

    addon_root = Path(addon)
    if not (addon_root / "io_soulworker").is_dir():
        print(f"addon not found: {addon_root}", file=sys.stderr)
        return 1

    _stub_modules()
    _load_addon(addon_root)

    from io_soulworker.core.varchive.shapes import read_zone_file

    for zone_path in _zone_files(scene_path):
        try:
            parsed = read_zone_file(zone_path.read_bytes())
        except Exception as exc:  # noqa: BLE001 - surface to the tool
            print(f"failed {zone_path.name}: {exc}", file=sys.stderr)
            continue

        for instance in parsed.static_meshes:
            if not instance.path:
                continue
            _emit(instance)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
