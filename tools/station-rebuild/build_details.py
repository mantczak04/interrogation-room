"""Reusable station furnishings, authored through Blender MCP. Unity units are metres."""
import bpy
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Art/Environment/StationRebuild'
previous = next(s for s in bpy.data.scenes if s.name not in ('StationDetails','DoorAsset'))
old = bpy.data.scenes.get('StationDetails')
if old:
    for obj in list(old.objects):
        bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.scenes.remove(old)
scene = bpy.data.scenes.new('StationDetails')
bpy.context.window.scene = scene
colors = {'Paint':(.22,.28,.27,1), 'Steel':(.20,.23,.23,1), 'Timber':(.32,.22,.13,1),
          'Paper':(.74,.71,.60,1), 'Cork':(.31,.21,.12,1), 'Ink':(.055,.065,.065,1),
          'Red':(.38,.075,.045,1), 'Ceramic':(.60,.65,.62,1), 'Feather':(.23,.27,.30,1)}
for name,color in colors.items():
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color=color
    m.use_nodes=True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=color
    m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.65

parts=[]
def box(x,y,z,w,h,d,mat,bevel=.008):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(-x,-z,y))
    o=bpy.context.object
    o.dimensions=(w,d,h)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(bpy.data.materials[mat])
    if bevel:
        mod=o.modifiers.new('Rounded edges','BEVEL');mod.width=min(bevel,min(w,h,d)/3);mod.segments=2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(o)
    return o

def ellipsoid(x,y,z,w,h,d,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,location=(-x,-z,y))
    o=bpy.context.object;o.scale=(w/2,d/2,h/2)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(bpy.data.materials[mat])
    for p in o.data.polygons:p.use_smooth=True
    parts.append(o)
    return o

def export(name):
    groups={}
    for o in parts:groups.setdefault(o.data.materials[0].name,[]).append(o)
    for mat,same in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for o in same:o.select_set(True)
        bpy.context.view_layer.objects.active=same[0]
        if len(same)>1:bpy.ops.object.join()
        o=bpy.context.object;o.name=name+'_'+mat
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bpy.ops.object.select_all(action='DESELECT')
    selected=[o for o in scene.objects if o.name.startswith(name+'_')]
    for o in selected:o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},
        axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False)
    parts.clear()

# Table: a non-reflective oak top, steel frame and a modest paper folio.
box(0,.75,0,1.7,.065,.9,'Timber',.022)
for x in [-.7,.7]:
    for z in [-.32,.32]:box(x,.37,z,.055,.73,.055,'Steel')
box(0,.64,0,1.5,.06,.65,'Steel')
box(-.43,.794,0,.30,.018,.22,'Paper',.002)
box(-.46,.805,.025,.16,.002,.012,'Ink',.001)
export('InterviewTable')

# Wall utility panel, with physical controls and ventilation slots.
box(0,0,0,.65,.86,.16,'Paint',.024)
box(0,0,-.093,.57,.77,.035,'Steel')
box(-.1,.18,-.12,.28,.16,.02,'Ink')
for x in [-.18,0,.18]:
    box(x,-.12,-.13,.08,.13,.04,'Red' if x==.18 else 'Ceramic')
for i in range(5):box(0,-.26-i*.027,-.114,.38,.009,.008,'Ink',0)
box(.255,.19,-.13,.025,.1,.028,'Timber')
export('UtilityPanel')

# Escape mechanisms are clearly recognisable secured service doors.
box(0,1.13,0,1.20,2.26,.12,'Steel',.012)
box(0,1.13,-.075,1.07,2.12,.04,'Paint')
for x in [-.62,.62]:box(x,1.17,-.025,.075,2.34,.22,'Steel')
box(0,2.31,-.025,1.32,.075,.22,'Steel')
box(0,1.06,-.18,.8,.05,.065,'Ceramic')
for x in [-.4,.4]:box(x,1.06,-.12,.05,.12,.11,'Steel')
box(0,.23,-.105,.94,.30,.012,'Steel')
box(0,1.7,-.103,.4,.21,.014,'Paper')
for i in range(4):box(0,1.65+i*.035,-.114,.26,.009,.007,'Ink',0)
export('ServiceExit')

box(0,.025,0,.43,.04,.30,'Steel')
for x in [-.215,.215]:box(x,.065,0,.016,.10,.30,'Steel')
box(0,.065,.145,.43,.1,.016,'Steel')
box(0,.055,0,.36,.045,.245,'Paper',.001)
for i in range(5):box(-.03,.079,-.07+i*.025,.23,.002,.006,'Ink',0)
export('ReceiptTray')

# Reception counter with a knee space on its staff side.
box(0,1.02,0,2.6,.07,.80,'Timber',.018)
box(0,.47,-.33,2.54,.9,.10,'Paint')
for x in [-1.18,1.18]:box(x,.46,0,.15,.91,.66,'Timber')
for x in [-.78,0,.78]:box(x,.48,-.395,.66,.68,.018,'Timber')
box(0,.07,-.39,2.53,.10,.04,'Steel')
export('ReceptionCounter')

box(0,0,0,2.3,1.3,.055,'Cork')
for x in [-1.17,1.17]:box(x,0,-.018,.055,1.4,.075,'Timber')
for y in [-.67,.67]:box(0,y,-.018,2.39,.055,.075,'Timber')
for i,(x,y) in enumerate([(-.8,.22),(-.36,.16),(.12,.24),(.70,.15),(-.58,-.30),(.10,-.30),(.71,-.31)]):
    box(x,y,-.042,.31,.36,.008,'Paper',.002)
    box(x,y+.16,-.052,.022,.022,.012,'Red',.004)
    for j in range(5):box(x,y+.10-j*.037,-.048,.21,.008,.003,'Ink',0)
export('NoticeBoard')

for i in range(14):box(-.52+i*.08,.38,0,.055,.58,.14,'Ceramic',.017)
for y in [.16,.58]:box(0,y,.07,1.14,.035,.06,'Steel')
for x in [-.46,.46]:box(x,.08,.05,.06,.16,.14,'Steel')
export('Radiator')

# Wall clock has twelve markers and fixed hands for a consistent visual.
ellipsoid(0,0,0,.48,.48,.055,'Steel')
ellipsoid(0,0,-.032,.43,.43,.015,'Paper')
for i in range(12):
    a=i*math.pi/6
    box(math.sin(a)*.175,math.cos(a)*.175,-.045,.018,.026,.008,'Ink',.001)
box(.045,.035,-.06,.12,.014,.008,'Ink',.001)
box(0,.06,-.067,.012,.16,.008,'Ink',.001)
export('WallClock')

# A mechanical typewriter for the existing interactive Easter egg.
box(0,.07,0,.48,.14,.38,'Ink',.02)
box(0,.14,.075,.47,.16,.17,'Steel',.03)
box(0,.28,.11,.35,.19,.015,'Paper',.002)
for row in range(3):
    for col in range(9):ellipsoid(-.185+col*.046,.15,-.125+row*.04,.031,.018,.028,'Ceramic')
box(0,.15,-.18,.24,.018,.03,'Steel')
export('Typewriter')

for x in [-.18,0,.18]:
    # Cylinder-like faceted cups with dark recessed opening, not a floating marker.
    ellipsoid(x,.085,0,.13,.16,.13,'Ceramic')
    ellipsoid(x,.152,0,.095,.009,.095,'Ink')
    box(x+.073,.09,0,.055,.07,.028,'Ceramic')
export('MugSet')

# The authored Easter egg explicitly describes a cardboard pigeon.
outline=[(-.18,.09),(-.11,.14),(-.10,.22),(-.03,.26),(.02,.30),(.02,.37),(.09,.41),(.15,.37),(.15,.33),(.22,.31),(.15,.29),(.12,.22),(.08,.12),(.02,.08)]
vertices=[(-x,-z,y) for z in [-.009,.009] for x,y in outline]
n=len(outline)
faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
mesh=bpy.data.meshes.new('Cardboard silhouette');mesh.from_pydata(vertices,[],faces);mesh.update()
bird=bpy.data.objects.new('CardboardPigeon',mesh);scene.collection.objects.link(bird)
bird.data.materials.append(bpy.data.materials['Paper']);parts.append(bird)
for z in [-.011,.011]:ellipsoid(.112,.355,z,.016,.016,.005,'Ink')
box(0,.04,0,.045,.08,.03,'Cork')
box(0,.012,0,.18,.025,.12,'Timber')
export('Pigeon')

box(0,0,0,.27,.40,.1,'Steel',.025)
for i in range(8):box(0,.11-i*.02,-.056,.17,.007,.01,'Ink',0)
box(0,-.12,-.07,.07,.06,.04,'Ceramic')
export('Intercom')

# Office storage with separate drawer fronts, label holders and handles.
box(0,.75,0,.85,1.5,.48,'Paint',.014)
for y in [.22,.58,.94,1.30]:
    box(0,y,-.253,.78,.32,.028,'Steel')
    box(0,y+.025,-.279,.19,.07,.01,'Paper',.002)
    box(0,y-.075,-.30,.22,.025,.03,'Ink')
export('FileCabinet')

box(0,.75,0,2.04,.075,.13,'Steel')
for i in range(17):
    o=box(0,.68-i*.08,0,1.97,.018,.10,'Ceramic',.005)
    o.rotation_euler.x=.35
for x in [-.7,.7]:box(x,0,-.055,.009,1.4,.009,'Timber',0)
export('WindowBlind')

box(0,0,.025,1.08,.88,.13,'Steel')
box(0,0,-.05,.92,.72,.018,'Ink')
for i in range(9):
    o=box(0,-.31+i*.078,-.085,.93,.022,.09,'Steel',.005)
    o.rotation_euler.x=.45
for x in [-.50,.50]:
    for y in [-.4,.4]:ellipsoid(x,y,-.049,.026,.026,.012,'Ceramic')
export('ServiceVent')

bpy.context.window.scene=previous
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/StationRebuild/StationRebuild.blend'))
print('DETAILS_EXPORT complete: 15 reusable models')
