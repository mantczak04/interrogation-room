"""Workshop bench and stocked maintenance racks.

Run in background Blender. Writes only WorkshopDetailBench and WorkshopSupplyRack A/B.
"""
import bpy
import math
from pathlib import Path

OUT = Path(__file__).resolve().parents[2] / 'Assets/Art/Environment/StationRebuild'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
materials = {}
for key in ['Paint', 'Steel', 'Rubber', 'Timber', 'Paper', 'Cardboard', 'Red']:
    materials[key] = bpy.data.materials.new(key)

def finish(o, key, bevel=.003):
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(materials[key])
    if bevel:
        m=o.modifiers.new('Rounded manufactured edge','BEVEL'); m.width=bevel; m.segments=3
        bpy.ops.object.modifier_apply(modifier=m.name)
        m=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
        bpy.ops.object.modifier_apply(modifier=m.name)
    return o

def box(x,y,z,w,h,d,key,bevel=.003):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(-x,-z,y))
    o=bpy.context.object; o.dimensions=(w,d,h)
    return finish(o,key,min(bevel,min(w,h,d)*.2))

def tube(points,r=.01,key='Steel'):
    c=bpy.data.curves.new('Bent tube','CURVE'); c.dimensions='3D'; c.bevel_depth=r; c.bevel_resolution=2
    s=c.splines.new('BEZIER'); s.bezier_points.add(len(points)-1)
    for p,(x,y,z) in zip(s.bezier_points,points):
        p.co=(-x,-z,y);p.handle_left_type='AUTO';p.handle_right_type='AUTO'
    o=bpy.data.objects.new('Bent tube',c);bpy.context.collection.objects.link(o)
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.convert(target='MESH');o.data.materials.append(materials[key])

def disk(x,y,z,r,depth,key):
    bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=r,depth=depth,location=(-x,-z,y))
    return finish(bpy.context.object,key,.001)

def export(name):
    groups={}
    for o in bpy.context.scene.objects:groups.setdefault(o.data.materials[0].name,[]).append(o)
    for key,objects in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for o in objects:o.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        if len(objects)>1:bpy.ops.object.join()
        o=bpy.context.object;o.name=name+'_'+key
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
        if key == 'Timber':
            uv=o.data.uv_layers.active
            for p in o.data.polygons:
                axis=max(range(3),key=lambda i:abs(p.normal[i]))
                axes=[i for i in range(3) if i!=axis]
                for li in p.loop_indices:
                    v=o.data.vertices[o.data.loops[li].vertex_index].co
                    uv.data[li].uv=(v[axes[0]],v[axes[1]])
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},
        axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False)
    print(name,sum(len(p.vertices)-2 for o in bpy.context.scene.objects for p in o.data.polygons),'triangles')
    bpy.ops.object.delete(use_global=False)

# A welded frame, inset drawer cabinet and laminated worktop at the existing height.
for x in [-.97,.97]:
    for z in [-.28,.28]:
        box(x,.455,z,.05,.89,.05,'Paint')
        disk(x,.02,z,.04,.035,'Rubber')
        box(x,.77,z,.10,.025,.09,'Steel')
for z in [-.28,.28]:box(0,.835,z,2,.07,.045,'Paint')
box(0,.91,0,2.2,.085,.76,'Timber',.01)
for z in [-.367,.367]:box(0,.91,z,2.19,.082,.014,'Steel')
box(.64,.465,.015,.63,.76,.57,'Paint',.008)
for y,h in [(.74,.16),(.53,.22),(.275,.25)]:
    box(.64,y,-.283,.59,h,.018,'Rubber')
    box(.64,y,-.299,.578,h-.012,.018,'Paint')
    tube([(.48,y+.02,-.312),(.48,y+.02,-.347),(.80,y+.02,-.347),(.80,y+.02,-.312)],.008)
    box(.64,y-.035,-.312,.10,.026,.004,'Paper',.001)
box(-.38,.20,.02,1.15,.035,.56,'Paint')
# Backboard, hooks and varied hand tools rather than cloned hammers.
box(0,1.455,.308,2.14,.88,.035,'Paint')
for x in [-1.045,1.045]:box(x,1.455,.278,.025,.88,.027,'Steel')
for j in range(10):
    for i in range(25):box(-.96+i*.08,1.09+j*.08,.288,.008,.008,.002,'Rubber',0)
for i,x in enumerate([-.83,-.58,-.29,.01,.29,.57,.83]):
    tube([(x,1.77,.28),(x,1.77,.25),(x,1.735,.235)],.005)
    if i in [0,1]:
        box(x,1.47,.245,.035,.34,.035,'Timber',.008)
        box(x,1.65,.24,.15,.048,.05,'Steel',.008)
    elif i in [2,3]:
        box(x,1.39,.24,.037,.14,.034,'Red',.01)
        box(x,1.55,.24,.011,.20,.012,'Steel')
        box(x,1.655,.24,.022,.025,.008,'Steel')
    elif i in [4,5]:
        box(x,1.51,.24,.027,.25,.025,'Steel')
        for side in [-1,1]:box(x+side*.028,1.66,.24,.019,.08,.025,'Steel')
        box(x,1.625,.24,.07,.022,.025,'Steel')
    else:
        tube([(x-.05,1.36,.24),(x-.02,1.52,.24),(x+.04,1.66,.24)],.012,'Red')
        tube([(x+.05,1.36,.24),(x+.02,1.52,.24),(x-.04,1.66,.24)],.012,'Steel')
# Small vice mounted at the free left front corner, with screw and sliding handle.
box(-.82,.979,-.17,.24,.052,.20,'Paint',.008)
box(-.82,1.035,-.17,.17,.07,.14,'Paint',.01)
for z in [-.24,-.11]:box(-.82,1.075,z,.19,.055,.027,'Steel')
tube([(-.82,1.026,-.20),(-.82,1.026,-.37)],.013)
tube([(-.82,.95,-.37),(-.82,1.13,-.37)],.008)
for x in [-.90,-.74]:disk(x,1.008,-.17,.012,.01,'Steel')
export('WorkshopDetailBench')

for variant in range(2):
    # Match the old rack footprint and shelf heights to retain collision and clearance.
    for x in [-.79,.79]:
        for z in [-.22,.22]:
            box(x,1.04,z,.035,2.08,.035,'Paint')
            box(x,.027,z,.08,.035,.07,'Steel')
    for y in [.12,.62,1.12,1.62,2.08]:
        box(0,y,0,1.64,.027,.49,'Paint')
        box(0,y-.025,-.237,1.62,.05,.018,'Paint')
    tube([(-.76,.15,.235),(.76,2.04,.235)],.008)
    tube([(.76,.15,.235),(-.76,2.04,.235)],.008)
    # Bottom cartons; seams and shipping labels give the contents a readable scale.
    for i in range(2):
        x=-.42+i*.77
        box(x,.335,0,.61,.40,.38,'Cardboard',.006)
        box(x,.539,0,.10,.006,.38,'Paper',.001)
        box(x,.36,-.192,.17,.09,.003,'Paper',0)
        for j in range(3):box(x,.34+j*.018,-.195,.11,.003,.001,'Rubber',0)
    # Open parts bins with visible fasteners, different fill patterns on the two racks.
    for i in range(3-variant):
        x=-.55+i*.48
        box(x,.655,0,.40,.025,.36,'Red' if variant else 'Paint')
        box(x,.765,.16,.40,.22,.02,'Paint')
        for dx in [-.19,.19]:box(x+dx,.765,0,.02,.22,.34,'Paint')
        box(x,.705,-.17,.40,.11,.02,'Paint')
        box(x,.722,-.184,.13,.036,.003,'Paper',0)
        for j in range(5):disk(x-.12+j*.058,.70,-.02,.022,.055,'Steel')
    # Cans and spools share one shelf, avoiding repeated archive-folder silhouettes.
    for i in range(3+variant):
        x=-.56+i*.25
        disk(x,1.295,.015,.075,.31,'Paint' if i%2 else 'Red')
        disk(x,1.458,.015,.08,.015,'Steel')
        box(x,1.30,-.061,.08,.1,.004,'Paper',0)
    for i in range(2):
        x=-.45+i*.72
        disk(x,1.775,0,.14,.26,'Rubber')
        for y in [1.655,1.905]:disk(x,y,0,.17,.018,'Timber')
        disk(x,1.919,0,.033,.012,'Rubber')
    export('WorkshopSupplyRack'+('A' if variant==0 else 'B'))
