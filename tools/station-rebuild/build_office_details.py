"""Office desk, telephone and filing storage.

Run in background Blender. Writes only OfficeDetailDesk, OfficeDetailPhone and OfficeFilingCabinet.
"""
import bpy
import math
from pathlib import Path

OUT = Path(__file__).resolve().parents[2] / 'Assets/Art/Environment/StationRebuild'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
materials = {}
for key in ['Paint', 'Steel', 'Rubber', 'Timber', 'Paper', 'Plastic', 'Ivory']:
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
    c=bpy.data.curves.new('Bent tube','CURVE'); c.dimensions='3D'; c.bevel_depth=r; c.bevel_resolution=2; c.resolution_u=3
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

# Existing 1.8 x .82 m desk footprint; manufactured panels, drawer reveals and cable rail.
box(0,.735,0,1.8,.06,.82,'Timber',.012)
for z in [-.401,.401]:box(0,.733,z,1.78,.038,.012,'Plastic')
for x in [-.77,.77]:
    for z in [-.30,.30]:
        box(x,.36,z,.045,.70,.045,'Paint')
        disk(x,.016,z,.028,.025,'Rubber')
box(0,.43,.285,1.53,.40,.023,'Paint')
box(.59,.39,.025,.51,.68,.65,'Paint',.007)
for y,h in [(.62,.15),(.425,.20),(.195,.23)]:
    box(.59,y,-.307,.475,h,.018,'Rubber')
    box(.59,y,-.321,.462,h-.012,.018,'Paint')
    tube([(.48,y+.02,-.334),(.48,y+.02,-.357),(.70,y+.02,-.357),(.70,y+.02,-.334)],.007)
    box(.59,y-.035,-.333,.085,.023,.002,'Paper',.001)
box(0,.62,.325,1.30,.025,.10,'Paint')
tube([(.1,.625,.32),(.4,.625,.33),(.48,.35,.33),(.49,.05,.33)],.006,'Rubber')
box(0,.767,-.16,.71,.004,.40,'Rubber',.009)
# Raised two-tier document trays at the rear left, with uneven paper stacks.
for y in [.795,.90]:
    box(-.61,y,.22,.32,.014,.29,'Paint')
    for x in [-.765,-.455]:box(x,y+.026,.22,.012,.055,.29,'Paint')
    box(-.61,y+.026,.36,.32,.055,.012,'Paint')
    for j in range(5):box(-.61+j*.001,y+.012+j*.003,.215,.27,.002,.245,'Paper',0)
for x in [-.74,-.48]:box(x,.84,.335,.014,.16,.014,'Steel')
# Pen cup, pencil shafts and an eraser occupy the back edge, away from keyboard/mouse.
disk(.31,.81,.29,.033,.09,'Plastic')
disk(.31,.857,.29,.027,.002,'Rubber')
for x,y,z in [(.30,.91,.28),(.32,.92,.29),(.31,.90,.31)]:
    tube([(x,y-.05,z),(x,y+.035,z)],.003,'Ivory')
# Compact computer at the rear right. Join the retained monitor, keyboard and mouse leads.
box(.59,.805,.285,.32,.08,.23,'Plastic',.008)
for i in range(9):box(.47+i*.025,.807,.167,.009,.038,.003,'Rubber',0)
box(.706,.807,.165,.013,.013,.004,'Steel',.004)
for x in [.48,.515]:box(x,.786,.164,.018,.008,.006,'Steel',.001)
tube([(.15,.785,.18),(.24,.773,.21),(.37,.773,.20),(.48,.786,.158)],.003,'Rubber')
tube([(.06,.775,-.05),(.18,.773,-.015),(.29,.773,.10),(.38,.78,.15),(.515,.786,.158)],.003,'Rubber')
tube([(.2,.770,.41),(.31,.776,.427),(.46,.794,.428),(.57,.81,.404)],.003,'Rubber')
export('OfficeDetailDesk')

# Desk telephone: rounded body, individual keys, receiver, speaker grille and coiled lead.
box(0,.033,0,.265,.055,.245,'Plastic',.017)
box(.039,.065,-.01,.156,.012,.16,'Rubber',.009)
for row in range(4):
    for col in range(3):
        x=.005+col*.037;z=-.075+row*.031
        box(x,.077,z,.027,.014,.022,'Ivory',.004)
        for j in range(1+(row+col)%3):box(x-.006+j*.005,.085,z,.002,.001,.009,'Plastic',0)
box(.042,.071,.082,.135,.008,.035,'Ivory',.002)
for i in range(6):box(.002+i*.016,.077,.083,.008,.002,.002,'Plastic',0)
for z in [-.082,.082]:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=(.098,-z,.113))
    o=bpy.context.object;o.dimensions=(.075,.085,.045);finish(o,'Plastic',0)
    for p in o.data.polygons:p.use_smooth=True
    box(-.098,.08,z,.065,.055,.045,'Plastic',.008)
tube([(-.098,.125,-.077),(-.098,.149,-.025),(-.098,.149,.025),(-.098,.125,.077)],.017,'Plastic')
coil=[]
for i in range(181):
    a=i/180*math.pi*2*18
    coil.append((-.169+math.cos(a)*.009,.026+math.sin(a)*.009,-.08+i/180*.19))
tube([(-.13,.10,-.082),(-.165,.055,-.095),coil[0]],.002,'Rubber')
tube(coil,.002,'Rubber')
tube([coil[-1],(-.14,.02,.135),(-.09,.025,.115)],.002,'Rubber')
tube([(.04,.028,.12),(.08,.008,.20),(.17,.008,.20)],.003,'Rubber')
for x in [-.095,.095]:
    for z in [-.09,.09]:disk(x,.005,z,.012,.01,'Rubber')
export('OfficeDetailPhone')

# Paired four-drawer filing columns, matching the former locker envelope.
box(0,.065,0,1.13,.12,.45,'Rubber')
box(0,1,0,1.16,1.80,.49,'Paint',.008)
box(0,1.915,0,1.18,.032,.51,'Paint')
for x in [-.285,.285]:
    for row in range(4):
        y=.35+row*.433
        box(x,y,-.251,.536,.416,.018,'Rubber')
        box(x,y,-.267,.525,.40,.018,'Paint',.004)
        tube([(x-.105,y+.06,-.281),(x-.105,y+.06,-.31),(x+.105,y+.06,-.31),(x+.105,y+.06,-.281)],.008)
        box(x,y-.04,-.282,.18,.071,.006,'Steel')
        box(x,y-.04,-.286,.159,.05,.003,'Paper',0)
        for j in range(2):box(x,y-.048+j*.018,-.288,.10,.003,.001,'Plastic',0)
    box(x+.19,1.787,-.283,.025,.025,.009,'Steel',.006)
    box(x+.19,1.787,-.29,.003,.014,.002,'Rubber',0)
export('OfficeFilingCabinet')
