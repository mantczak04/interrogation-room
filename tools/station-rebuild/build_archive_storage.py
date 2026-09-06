"""Detailed archive and supply shelves.

Run in background Blender. Writes DetailedArchiveRack and two DetailedSupplyRack variants.
"""
import bpy
import math
from pathlib import Path

OUT = Path(__file__).resolve().parents[2] / 'Assets/Art/Environment/StationRebuild'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
materials = {}
for key in ['Paint', 'Steel', 'Rubber', 'Timber', 'Paper', 'Plastic', 'Ivory', 'Cardboard', 'Blue']:
    materials[key] = bpy.data.materials.new(key)

def finish(o, key, bevel=.003):
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(materials[key])
    if bevel:
        m=o.modifiers.new('Rounded manufactured edge','BEVEL'); m.width=bevel; m.segments=2
        bpy.ops.object.modifier_apply(modifier=m.name)
        m=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
        bpy.ops.object.modifier_apply(modifier=m.name)
    return o

def box(x,y,z,w,h,d,key,bevel=.003):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(-x,-z,y))
    o=bpy.context.object; o.dimensions=(w,d,h)
    return finish(o,key,min(bevel,min(w,h,d)*.2))

def tube(points,r=.01,key='Steel'):
    c=bpy.data.curves.new('Bent tube','CURVE'); c.dimensions='3D'; c.bevel_depth=r; c.bevel_resolution=1; c.resolution_u=3
    s=c.splines.new('BEZIER'); s.bezier_points.add(len(points)-1)
    for p,(x,y,z) in zip(s.bezier_points,points):
        p.co=(-x,-z,y);p.handle_left_type='AUTO';p.handle_right_type='AUTO'
    o=bpy.data.objects.new('Bent tube',c);bpy.context.collection.objects.link(o)
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.convert(target='MESH');o.data.materials.append(materials[key])

def disk(x,y,z,r,depth,key):
    bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=depth,location=(-x,-z,y))
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

# Shared bolted shelving envelope: 1.64 x .49 m, 2.10 m high.
def frame():
    for x in [-.795,.795]:
        for z in [-.225,.225]:
            box(x,1.045,z,.033,2.07,.025,'Paint',.003)
            box(x,.013,z,.07,.022,.06,'Rubber')
            for y in [.12,.62,1.12,1.62,2.08]:
                o=disk(x,y,z-.014,.005,.003,'Steel');o.rotation_euler[0]=math.pi/2
    for y in [.12,.62,1.12,1.62,2.08]:
        box(0,y,0,1.64,.028,.49,'Paint')
        box(0,y-.027,-.242,1.59,.034,.008,'Steel')
    tube([(-.76,.16,.23),(.76,2.04,.23)],.008,'Steel')
    tube([(.76,.16,.23),(-.76,2.04,.23)],.008,'Steel')

def label(x,y,z,w=.085):
    box(x,y,z,w,.042,.002,'Paper',.0005)
    for i in range(3):box(x,y+.011-i*.009,z-.0015,w*.65,.0017,.0006,'Plastic',0)

def carton(x,y,w=.32,h=.26,d=.35):
    box(x,y+h/2,0,w,h,d,'Cardboard',.007)
    box(x,y+h-.007,0,w+.007,.023,d+.007,'Cardboard',.003)
    box(x,y+h+.006,0,.043,.002,d+.006,'Paper',.0005)
    label(x,y+h*.55,-d/2-.001,min(.11,w*.6))
    box(x,y+h*.8,-d/2-.002,.068,.022,.002,'Rubber',.007)

frame()
for row,y in enumerate([.135,.635,1.135,1.635]):
    if row in [0,2]:
        for i,x in enumerate([-.62,-.49,-.36,-.23,-.10,.03]):
            h=.34+(i%3)*.017
            key=['Blue','Paint','Ivory'][i%3]
            box(x,y+h/2,0,.105,h,.35,key,.003)
            box(x,y+h/2,.005,.084,h-.018,.319,'Paper',.001)
            box(x,y+h/2,-.173,.109,h,.012,key,.002)
            label(x,y+h*.65,-.181,.065)
            o=disk(x,y+.065,-.183,.012,.003,'Steel');o.rotation_euler[0]=math.pi/2
            o=disk(x,y+.065,-.185,.007,.003,'Rubber');o.rotation_euler[0]=math.pi/2
        carton(.48,y,.43,.31)
    else:
        for x in [-.55,-.05]:carton(x,y,.43,.28)
        for i in range(4):
            box(.5,y+.014+i*.03,0,.38,.027,.31,'Paper',.002)
            box(.5,y+.029+i*.03,0,.395,.003,.32,'Blue' if i%2 else 'Cardboard',.001)
export('DetailedArchiveRack')

for variant in [0,1]:
    frame()
    for row,y in enumerate([.135,.635,1.135,1.635]):
        if (row+variant)%3==0:
            for x in [-.5,.02,.53]:carton(x,y,.43,.28+.03*(row%2))
        elif (row+variant)%3==1:
            for x in [-.5,0,.5]:
                box(x,y+.13,0,.40,.25,.36,'Blue',.006)
                box(x,y+.255,0,.42,.016,.38,'Paint')
                for dx in [-.15,.15]:box(x+dx,y+.12,-.182,.015,.20,.006,'Paint')
                label(x,y+.18,-.185,.10)
                box(x,y+.085,-.187,.09,.024,.004,'Rubber',.005)
        else:
            for x in [-.60,-.36,-.12]:
                disk(x,y+.12,0,.085,.23,'Ivory')
                disk(x,y+.24,0,.087,.014,'Steel')
                tube([(x-.045,y+.245,0),(x-.04,y+.29,0),(x+.04,y+.29,0),(x+.045,y+.245,0)],.003,'Steel')
                label(x,y+.13,-.085,.09)
            carton(.43,y,.47,.25)
    export('DetailedSupplyRack'+str(variant))