"""A small industrial indicator housing for the existing world-state visuals."""
import bpy
import math
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
original = bpy.context.window.scene
scene = bpy.data.scenes.new('Status indicator authoring')
bpy.context.window.scene = scene
for name in ['Housing', 'Lens']:
    if name not in bpy.data.materials:
        bpy.data.materials.new(name)

def cylinder(radius, depth, z, material, vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
        location=(0,z,0), rotation=(math.pi/2,0,0))
    obj = bpy.context.object
    obj.data.materials.append(bpy.data.materials[material])
    bevel=obj.modifiers.new('Machined edge','BEVEL');bevel.width=.0015;bevel.segments=3
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    for face in obj.data.polygons: face.use_smooth=True
    return obj

cylinder(.052,.020,0,'Housing')
cylinder(.043,.009,-.014,'Housing')
cylinder(.037,.014,-.024,'Lens')
for i in range(4):
    bolt=cylinder(.004,.005,-.013,'Housing',12)
    bolt.location.x=math.cos(i*math.pi/2)*.045
    bolt.location.z=math.sin(i*math.pi/2)*.045
for name in ['Housing','Lens']:
    objects=[o for o in scene.objects if o.data.materials[0].name==name]
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects: obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    if len(objects)>1: bpy.ops.object.join()
    bpy.context.object.name='StatusIndicator_'+name
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/Art/Environment/StationRebuild/StatusIndicator.fbx'),
    use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
for obj in list(scene.objects): bpy.data.objects.remove(obj,do_unlink=True)
bpy.context.window.scene=original
bpy.data.scenes.remove(scene)
