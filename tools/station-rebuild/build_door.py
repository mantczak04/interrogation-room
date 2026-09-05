"""Build a reusable panelled door in the open station Blender document."""
import bpy
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Art/Environment/StationRebuild'
station_scene = bpy.context.scene
old = bpy.data.scenes.get('DoorAsset')
if old:
    bpy.data.scenes.remove(old)
scene = bpy.data.scenes.new('DoorAsset')
bpy.context.window.scene = scene

def box(name,x,y,z,w,h,d,material):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(-x,-z,y))
    o=bpy.context.object
    o.name=name
    o.dimensions=(w,d,h)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(bpy.data.materials[material])
    bevel=o.modifiers.new('Soft manufactured edges','BEVEL')
    bevel.width=.008
    bevel.segments=3
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return o

box('Panel',0,0,0,1.32,1.98,.036,'Oak')
for x in [-.69,.69]:
    box('Stile',x,0,0,.1,2.14,.065,'Oak')
for y,height in [(-1.02,.1),(.15,.14),(1.02,.1)]:
    box('Rail',0,y,0,1.32,height,.065,'Oak')
for side in [-1,1]:
    box('LatchPlate',.54,.0,side*.039,.11,.24,.015,'Metal')
    box('HandleNeck',.54,.025,side*.073,.03,.04,.07,'Brass')
    box('Lever',.45,.025,side*.11,.23,.035,.04,'Brass')
    box('KickPlate',0,-.87,side*.039,1.22,.18,.012,'Metal')

for material in ['Oak','Metal','Brass']:
    bpy.ops.object.select_all(action='DESELECT')
    objects=[o for o in scene.objects if o.type=='MESH' and o.data.materials[0].name==material]
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.join()
    o=bpy.context.object
    o.name='Door_'+material
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    # UVs follow door width and height for readable vertical wood grain.
    uv=o.data.uv_layers.active
    for loop in o.data.loops:
        v=o.data.vertices[loop.vertex_index].co
        uv.data[loop.index].uv=(v.x,v.z)
bpy.ops.export_scene.fbx(filepath=str(OUT/'DoorLeaf.fbx'),object_types={'MESH'},
    axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False)
bpy.context.window.scene=station_scene
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/StationRebuild/StationRebuild.blend'))
print('DOOR_EXPORT complete')
