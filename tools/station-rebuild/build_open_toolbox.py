"""Open workshop toolbox with a raised lid and fitted hand tools.

Run in background Blender. Writes only WorkshopOpenToolbox.
"""
import bpy
import math
from pathlib import Path

OUT = Path(__file__).resolve().parents[2] / 'Assets/Art/Environment/StationRebuild'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
materials = {}
for key in ['Paint', 'Steel', 'Rubber', 'Timber', 'Paper', 'Plastic', 'Ivory', 'Red', 'Rust']:
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

# Open steel toolbox; front is -Z, lid raised behind the open tray.
box(0,.018,0,.40,.022,.235,'Paint')
for x in [-.195,.195]: box(x,.086,0,.010,.14,.235,'Paint')
for z in [-.113,.113]: box(0,.086,z,.39,.14,.010,'Paint')
box(0,.036,0,.373,.013,.207,'Rubber')
# Upright lid and shallow folded rim. The open interior faces the bench user.
box(0,.253,.124,.40,.22,.012,'Paint')
for x in [-.195,.195]:box(x,.253,.112,.01,.22,.022,'Steel')
box(0,.358,.112,.39,.012,.022,'Steel')
for x in [-.135,.135]:
    box(x,.15,.117,.046,.020,.020,'Steel')
    box(x,.112,-.123,.03,.038,.01,'Steel')
# Carry handle on the outside of the lid.
tube([(-.075,.28,.14),(-.075,.32,.155),(.075,.32,.155),(.075,.28,.14)],.007,'Steel')
box(0,.322,.155,.10,.017,.018,'Rubber')
# Tray dividers and a few deliberately placed hand tools.
box(0,.07,.04,.37,.07,.005,'Steel')
for x in [-.09,.085]:box(x,.058,-.035,.006,.044,.14,'Steel')
for x in [-.145,-.12]:
    tube([(x,.052,-.07),(x,.052,.018)],.003,'Steel')
    box(x,.053,-.077,.018,.013,.044,'Plastic')
tube([(.035,.05,-.075),(.035,.05,.014)],.007,'Steel')
box(.035,.052,.021,.035,.012,.024,'Steel')
for x in [.115,.145]:disk(x,.05,-.035,.012,.021,'Steel')
# Folded lips, pressed strengthening ribs and exposed rolled edges.
for z in [-.117,.117]:box(0,.158,z,.398,.009,.009,'Steel',.002)
for x in [-.198,.198]:box(x,.158,0,.009,.009,.235,'Steel',.002)
for y in [.06,.125]:box(0,y,-.119,.36,.006,.003,'Paint',.001)
for x in [-.17,.17]:box(x,.251,.115,.005,.176,.004,'Paint',.001)
for y in [.177,.332]:box(0,y,.115,.34,.005,.004,'Paint',.001)
# Lid supports, rivets and articulated latch bails.
for x in [-.176,.176]:
    tube([(x,.13,.055),(x,.20,.08),(x,.265,.108)],.0025,'Steel')
    for y,z in [(.13,.055),(.265,.108)]:
        o=disk(x,y,z,.004,.005,'Steel');o.rotation_euler[1]=math.pi/2
for x in [-.135,.135]:
    box(x,.115,-.132,.024,.025,.004,'Steel',.001)
    tube([(x-.009,.126,-.139),(x-.009,.155,-.139),(x+.009,.155,-.139),(x+.009,.126,-.139)],.002,'Steel')
    for dx in [-.008,.008]:
        o=disk(x+dx,.104,-.135,.0025,.003,'Steel');o.rotation_euler[0]=math.pi/2
# A raised lift-out tray keeps the contents visible from standing height.
box(0,.09,0,.365,.007,.198,'Paint')
for z in [-.095,.095]:box(0,.108,z,.365,.035,.004,'Paint')
for x in [-.179,.179]:box(x,.108,0,.004,.035,.19,'Paint')
box(0,.109,.036,.36,.035,.004,'Steel')
# Two red screwdrivers with ribbed grips and visible flat blades.
for x in [-.145,-.105]:
    tube([(x,.12,-.07),(x,.12,.014)],.0022,'Steel')
    box(x,.12,-.066,.017,.017,.044,'Red',.004)
    for z in [-.08,-.072,-.064,-.056]:box(x,.13,z,.017,.002,.002,'Rubber',.0005)
    box(x,.12,.019,.006,.002,.012,'Steel',.0005)
# Pliers: separate jaws, pivot and curved rubber handles.
for side in [-1,1]:
    tube([(side*.018,.122,-.078),(side*.014,.122,-.041),(side*.005,.122,-.016)],.006,'Red')
    tube([(side*.005,.122,-.016),(-side*.006,.122,.004),(-side*.010,.122,.023)],.004,'Steel')
disk(0,.126,-.016,.006,.005,'Steel')
# Hollow sockets on a rail, with different sizes.
box(.115,.10,.06,.10,.005,.025,'Rubber')
for x,r in [(.08,.008),(.11,.010),(.145,.012)]:
    points=[(x+math.cos(a)*r,.119,.06+math.sin(a)*r) for a in [i*math.tau/16 for i in range(17)]]
    tube(points,.0025,'Steel')
    points=[(x+math.cos(a)*r,.107,.06+math.sin(a)*r) for a in [i*math.tau/16 for i in range(17)]]
    tube(points,.0025,'Steel')
# Paper inventory card attached to the inside of the lid.
box(.085,.273,.114,.112,.052,.0015,'Paper',.0003)
for i in range(4):box(.082,.29-i*.01,.1125,.080,.0014,.0005,'Plastic',0)
# Restrained chipped paint on contact edges, plus small rust at fasteners.
for x,w in [(-.16,.012),(-.071,.019),(.032,.008),(.153,.017)]:
    box(x,.157,-.122,w,.003,.001,'Steel',.0003)
for x in [-.135,.135]:box(x+.015,.104,-.12,.006,.005,.001,'Rust',.0003)
for x in [-.18,.18]:
    for z in [-.096,.096]:box(x,.008,z,.022,.01,.022,'Rubber')
export('WorkshopOpenToolbox')