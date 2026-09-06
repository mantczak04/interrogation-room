"""Detailed repeated fixtures for the station.

Run in background Blender. Writes door, radiator, ceiling trim and briefing table FBXs.
"""
import bpy
import math
from pathlib import Path

OUT = Path(__file__).resolve().parents[2] / 'Assets/Art/Environment/StationRebuild'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
materials = {}
for key in ['Paint', 'Steel', 'Rubber', 'Timber', 'Paper', 'Plastic', 'Ivory', 'Cardboard', 'Blue', 'Brass', 'Enamel']:
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

# Panel door with separate rails, panel beads and serviceable hardware.
box(0,0,0,1.32,1.98,.035,'Timber',.004)
for x in [-.69,.69]:box(x,0,0,.1,2.14,.065,'Timber',.004)
for y,h in [(-1.02,.10),(.15,.14),(1.02,.10)]:box(0,y,0,1.32,h,.065,'Timber',.004)
for side in [-1,1]:
    for lo,hi in [(-.965,.07),(.23,.965)]:
        for x in [-.642,.642]:box(x,(lo+hi)/2,side*.027,.018,hi-lo,.016,'Timber',.003)
        for y in [lo,hi]:box(0,y,side*.027,1.27,.018,.016,'Timber',.003)
    box(.54,0,side*.04,.075,.22,.012,'Steel',.006)
    tube([(.54,.027,side*.05),(.54,.027,side*.095),(.47,.027,side*.11),(.35,.027,side*.11)],.014,'Brass')
    for y in [-.086,.086]:
        o=disk(.54,y,side*.049,.005,.003,'Brass');o.rotation_euler[0]=math.pi/2
        box(.54,y,side*.052,.006,.0015,.001,'Rubber',0)
    o=disk(.54,-.055,side*.049,.008,.003,'Rubber');o.rotation_euler[0]=math.pi/2
    box(.54,-.065,side*.05,.006,.015,.003,'Rubber',.001)
    box(0,-.875,side*.038,1.20,.17,.006,'Steel',.002)
    for x in [-.56,.56]:
        for y in [-.935,-.815]:
            o=disk(x,y,side*.043,.004,.002,'Steel');o.rotation_euler[0]=math.pi/2
for y in [-.76,0,.76]:
    box(-.714,y,.041,.041,.11,.009,'Brass',.002)
    o=disk(-.736,y,.036,.009,.105,'Brass')
export('RefinedDoorLeaf')

# Hollow cast-iron sections with connecting nipples and visible plumbing.
for i in range(12):
    x=-.4675+i*.085
    for z in [-.046,.046]:
        tube([(x,.13,z),(x,.15,z),(x,.59,z),(x,.62,z)],.021,'Enamel')
    for y in [.145,.605]:tube([(x,y,-.046),(x,y,.046)],.023,'Enamel')
for y in [.145,.605]:tube([(-.5,y,0),(.5,y,0)],.018,'Enamel')
for x in [-.39,.39]:
    box(x,.075,.025,.052,.14,.13,'Enamel',.012)
    box(x,.009,.025,.07,.014,.145,'Rubber',.003)
for x in [-.56,.56]:
    tube([(x,.59,0),(x,.15,0),(x,.15,.15)],.012,'Steel')
    o=disk(x,.59,0,.025,.045,'Brass');o.rotation_euler[2]=math.pi/2
# Thermostatic valve knob with grip grooves.
o=disk(.615,.59,0,.028,.075,'Ivory');o.rotation_euler[2]=math.pi/2
for a in [i*math.tau/12 for i in range(12)]:
    tube([(.59,.59+math.cos(a)*.028,math.sin(a)*.028),(.646,.59+math.cos(a)*.028,math.sin(a)*.028)],.0015,'Plastic')
export('RefinedRadiator')

# Clips, folded trim and louvres around existing ceiling diffuser and light.
for z in [-.182,.182]:box(0,0,z,1.28,.14,.016,'Enamel',.003)
for x in [-.633,.633]:box(x,0,0,.018,.14,.365,'Enamel',.003)
for z in [-.148,.148]:box(0,-.087,z,1.17,.011,.017,'Steel',.002)
for x in [-.585,.585]:box(x,-.087,0,.017,.011,.285,'Steel',.002)
for x in [-.43,.43]:
    for z in [-.188,.188]:box(x,-.04,z,.035,.05,.016,'Steel',.003)
for x in [-.50,-.40,-.30,-.20,-.10,0,.10,.20,.30,.40,.50]:
    box(x,-.09,0,.003,.008,.266,'Ivory',.001)
for x in [-.50,-.44,-.38,.38,.44,.50]:
    box(x,.013,-.191,.025,.004,.001,'Rubber',0)
export('RefinedFixtureTrim')

# Two modular meeting tables replace the loose round tables.
box(0,.751,0,1.58,.05,1.28,'Timber',.012)
for z in [-.64,.64]:box(0,.749,z,1.57,.033,.009,'Rubber',.002)
for x in [-.789,.789]:box(x,.749,0,.009,.033,1.28,'Rubber',.002)
for x in [-.65,.65]:
    for z in [-.5,.5]:
        box(x,.37,z,.035,.71,.035,'Steel',.003)
        box(x,.011,z,.045,.022,.045,'Rubber',.003)
    box(x,.69,0,.028,.035,1.04,'Steel')
for z in [-.49,.49]:box(0,.69,z,1.32,.035,.025,'Steel')
# A restrained meeting place setting, kept clear of table edges.
box(-.32,.78,0,.25,.013,.33,'Blue',.002)
box(-.32,.788,0,.228,.004,.305,'Paper',.001)
box(-.14,.786,.01,.009,.009,.16,'Plastic',.002)
export('RefinedBriefingTable')
