"""Flatten imported prop rigs for static environment use without changing their baked UVs."""
import bpy
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'Assets/Art/Environment/StationRebuild/Scanned'
original_scene = bpy.context.window.scene
scene = bpy.data.scenes.new('Scanned asset processing')
bpy.context.window.scene = scene
for asset_id in ['drawer_cabinet', 'power_box_01', 'potted_plant_04', 'binder_notebook',
                 'metal_toolbox', 'cardboard_box_01', 'fire_alarm', 'desk_lamp_arm_01']:
    bpy.ops.import_scene.fbx(filepath=str(FOLDER / asset_id / (asset_id + '.fbx')))
    objects = list(scene.objects)
    meshes = [o for o in objects if o.type == 'MESH' and len(o.data.vertices)]
    if asset_id == 'binder_notebook':
        meshes = [o for o in meshes if 'closed' in o.name]
    for obj in meshes:
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.convert(target='MESH')
    bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    model = bpy.context.object
    model.name = asset_id + '_static'
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    vertices = [model.matrix_world @ Vector(corner) for corner in model.bound_box]
    center = Vector(((min(p.x for p in vertices)+max(p.x for p in vertices))/2,
                     (min(p.y for p in vertices)+max(p.y for p in vertices))/2,
                     min(p.z for p in vertices)))
    scene.cursor.location = center
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    model.location = (0, 0, 0)
    bpy.ops.export_scene.fbx(filepath=str(FOLDER / asset_id / (asset_id + '_static.fbx')),
        use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
        bake_space_transform=True, add_leaf_bones=False, bake_anim=False)
    print(asset_id, 'triangles', sum(len(p.vertices)-2 for p in model.data.polygons), 'size', tuple(round(v,3) for v in model.dimensions))
    for obj in list(scene.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
bpy.context.window.scene = original_scene
bpy.data.scenes.remove(scene)
