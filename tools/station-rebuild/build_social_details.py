"""Staff-room furniture with recessed sink and manufactured appliance details.

Run in background Blender. Writes only SocialKitchenDetail and SocialFridgeDetail.
"""
import bpy
import math
from pathlib import Path

OUT = Path(__file__).resolve().parents[2] / 'Assets/Art/Environment/StationRebuild'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
materials = {}
for key in ['Enamel', 'Steel', 'Rubber', 'Worktop', 'Paper']:
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
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},
        axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False)
    print(name,sum(len(p.vertices)-2 for o in bpy.context.scene.objects for p in o.data.polygons),'triangles')
    bpy.ops.object.delete(use_global=False)

# Four separate cabinet boxes, with recessed toe-kick and concealed carcase.
box(0,.08,.025,2.9,.15,.51,'Rubber')
box(0,.16,0,3,.035,.61,'Enamel')
box(0,.49,.298,3,.65,.025,'Enamel')
for x in [-1.49,-.75,0,.75,1.49]:
    box(x,.44 if x == .75 else .51,0,.022,.56 if x == .75 else .7,.61,'Enamel')
for x in [-1.125,-.375,.375,1.125]:
    box(x,.46,-.308,.726,.58,.023,'Rubber')
    box(x,.46,-.327,.714,.565,.028,'Enamel',.004)
    box(x,.812,-.308,.726,.112,.023,'Rubber')
    box(x,.812,-.327,.714,.10,.028,'Enamel',.004)
    tube([(x-.105,.813,-.343),(x-.10,.813,-.374),(x+.10,.813,-.374),(x+.105,.813,-.343)],.009)
    tube([(x+.23,.62,-.343),(x+.23,.62,-.374),(x+.23,.48,-.374),(x+.23,.48,-.343)],.009)

# Counter built around a real hole, maintaining the original .935 m support height.
box(-.52,.91,0,2.02,.05,.69,'Worktop',.007)
box(1.395,.91,0,.27,.05,.69,'Worktop',.007)
box(.875,.91,-.285,.77,.05,.12,'Worktop',.006)
box(.875,.91,.285,.77,.05,.12,'Worktop',.006)
box(0,.985,.326,3.06,.1,.024,'Worktop')
# Tapered basin: open top, bottom and side walls, no false plate over the opening.
v=[]
for y,w,d in [(.939,.76,.46),(.77,.54,.30)]:
    for x,z in [(-w/2,-d/2),(w/2,-d/2),(w/2,d/2),(-w/2,d/2)]:
        v.append((-(.875+x),-z,y))
faces=[(1,5,4,0),(2,6,5,1),(3,7,6,2),(0,4,7,3),(5,6,7,4)]
mesh=bpy.data.meshes.new('Recessed basin');mesh.from_pydata(v,[],faces);mesh.update()
o=bpy.data.objects.new('Recessed basin',mesh);bpy.context.collection.objects.link(o);o.data.materials.append(materials['Steel'])
disk(.875,.772,0,.028,.002,'Rubber');disk(.875,.774,0,.02,.002,'Steel')
for x in [.475,1.275]:box(x,.94,0,.026,.014,.50,'Steel')
for z in [-.245,.245]:box(.875,.94,z,.826,.014,.026,'Steel')
tube([(.875,.94,.28),(.875,1.19,.28),(.875,1.26,.18),(.875,1.23,.06)],.017)
disk(.875,.943,.28,.039,.01,'Steel')
tube([(.98,.945,.28),(.98,1.005,.28),(1.04,1.005,.28)],.009)
for i in range(8):box(.06+i*.04,.939,0,.013,.005,.38,'Steel',.001)
# Soap bottle and sponge stay on the sink rim, away from task mugs.
box(1.37,.986,.19,.065,.09,.05,'Enamel',.01)
tube([(1.37,1.03,.19),(1.37,1.06,.19),(1.33,1.06,.19)],.006,'Rubber')
export('SocialKitchenDetail')

# Freestanding refrigerator. Rear coils, actual separate skins and recessed gaskets.
box(0,.935,.015,.72,1.83,.64,'Enamel',.02)
for x in [-.28,.28]:
    for z in [-.24,.24]:disk(x,.022,z,.025,.04,'Rubber')
for y,h in [(1.52,.59),(.665,1.075)]:
    box(0,y,-.318,.685,h,.025,'Rubber',.008)
    box(0,y,-.353,.666,h-.019,.065,'Enamel',.013)
    tube([(-.258,y+h*.22,-.388),(-.258,y+h*.22,-.425),(-.258,y-h*.22,-.425),(-.258,y-h*.22,-.388)],.013)
    for sy in [-1,1]:box(.328,y+sy*(h*.42),-.327,.027,.045,.058,'Steel')
for y in [.065,.085,.105]:box(0,y,-.321,.57,.008,.015,'Rubber',.001)
for i in range(12):tube([(-.29,.28+i*.115,.349),(.29,.28+i*.115,.349)],.004,'Rubber')
box(.16,1.72,-.39,.145,.026,.005,'Steel',.002)
for i in range(5):box(.113+i*.023,1.72,-.394,.01,.014,.003,'Rubber',.001)
box(.08,1.05,-.389,.135,.18,.002,'Paper',0)
for i in range(5):box(.074,.995+i*.022,-.391,.085,.003,.001,'Rubber',0)
box(.08,1.135,-.394,.025,.015,.008,'Steel')
export('SocialFridgeDetail')
