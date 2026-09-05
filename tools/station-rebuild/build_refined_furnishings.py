"""Purpose-built station furniture. Run through Blender MCP; keeps other scenes intact."""
import bpy
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Art/Environment/StationRebuild'
original = bpy.context.window.scene
scene = bpy.data.scenes.new('Station furniture refinement')
bpy.context.window.scene = scene
parts = []
colors = {'Paint':(.37,.43,.40,1), 'Steel':(.16,.18,.18,1), 'Timber':(.38,.26,.16,1),
          'Paper':(.72,.68,.57,1), 'Ink':(.035,.045,.045,1), 'Ceramic':(.73,.75,.71,1),
          'Red':(.4,.065,.04,1)}
for name, color in colors.items():
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = color

def box(x,y,z,w,h,d,mat,bevel=.006):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(-x,-z,y))
    o=bpy.context.object; o.dimensions=(w,d,h)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(bpy.data.materials[mat])
    if bevel:
        m=o.modifiers.new('Edge bevel','BEVEL');m.width=min(bevel,min(w,h,d)*.3);m.segments=3
        bpy.ops.object.modifier_apply(modifier=m.name)
        m=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=m.name)
    uv=o.data.uv_layers.active
    for poly in o.data.polygons:
        axes=[i for i in range(3) if i!=max(range(3),key=lambda i:abs(poly.normal[i]))]
        for li in poly.loop_indices:
            v=o.data.vertices[o.data.loops[li].vertex_index].co+o.location
            uv.data[li].uv=(v[axes[0]],v[axes[1]])
    parts.append(o)
    return o

def label(x,y,z,w=.12,h=.045):
    box(x,y,z,w,h,.004,'Paper',.001)
    for row in range(2):box(x,y-.008+row*.015,z-.003,w*.65,.004,.003,'Ink',0)

def export(name):
    groups={}
    for o in parts:groups.setdefault(o.data.materials[0].name,[]).append(o)
    for mat,objects in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for o in objects:o.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        if len(objects)>1:bpy.ops.object.join()
        o=bpy.context.object;o.name=name+'_'+mat
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},
        axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False)
    print(name, 'triangles',sum(len(p.vertices)-2 for o in scene.objects for p in o.data.polygons))
    for o in list(scene.objects):bpy.data.objects.remove(o,do_unlink=True)
    parts.clear()

# Continuous counter with inset plinth, four doors, drawers and a sink at one end.
box(0,.075,.025,2.88,.15,.50,'Ink')
box(0,.50,0,3,.75,.63,'Paint',.012)
box(0,.905,0,3.06,.06,.69,'Timber',.018)
box(0,1.01,.326,3.06,.15,.025,'Ceramic')
for x in [-1.125,-.375,.375,1.125]:
    box(x,.465,-.33,.726,.64,.035,'Ceramic',.009)
    box(x,.807,-.33,.726,.12,.035,'Ceramic')
    box(x,.77,-.37,.22,.022,.035,'Steel')
    box(x+.25,.64,-.37,.022,.16,.035,'Steel')
box(1,.938,0,.60,.009,.45,'Steel')
box(1,.944,0,.49,.008,.34,'Ink')
box(1,.95,0,.41,.01,.27,'Steel')
box(1,1.075,.24,.035,.28,.035,'Steel',.015)
box(1,1.20,.13,.035,.035,.24,'Steel',.015)
export('KitchenRun')

# Full-height refrigerator: separate doors, gasket reveals, handles and bottom grille.
box(0,.93,0,.72,1.84,.68,'Ceramic',.028)
for y,h in [(1.52,.59),(.66,1.07)]:
    box(0,y,-.353,.676,h,.027,'Ink')
    box(0,y,-.38,.66,h-.02,.045,'Ceramic',.018)
    box(-.26,y,-.426,.026,h*.55,.045,'Steel',.01)
label(.15,1.65,-.407,.2,.04)
for y in [.095,.115,.135]:box(0,y,-.355,.55,.009,.014,'Steel',0)
export('StationFridge')

# A standard 76 cm table, with room for knees rather than a low coffee-table profile.
box(0,.735,0,1.6,.05,.9,'Timber',.025)
for x in [-.66,.66]:
    for z in [-.32,.32]:box(x,.36,z,.05,.71,.05,'Steel')
box(0,.66,.32,1.36,.06,.04,'Steel')
box(0,.66,-.32,1.36,.06,.04,'Steel')
export('StationDiningTable')

# Stocked archive shelves with separate folders, label holders and rear bracing.
for x in [-.79,.79]:
    for z in [-.22,.22]:box(x,1.04,z,.035,2.08,.035,'Steel')
for y in [.12,.62,1.12,1.62,2.08]:box(0,y,0,1.64,.035,.49,'Paint')
for row,y in enumerate([.145,.645,1.145,1.645]):
    for col in range(7):
        x=-.66+col*.205;h=.34+(col%3)*.018
        box(x,y+h/2,.035,.175,h,.35,'Timber' if row%2 else 'Paint')
        box(x,y+h/2,-.145,.17,h,.012,'Paper')
        label(x,y+h/2,-.154,.12,.045)
box(-.76,1.1,.245,.022,1.95,.018,'Steel')
box(.76,1.1,.245,.022,1.95,.018,'Steel')
export('StationArchiveRack')

# Lockable personal storage, with inset doors, vent slots and numbered label plates.
box(0,.99,0,1.16,1.9,.49,'Paint',.016)
for x in [-.285,.285]:
    box(x,1,-.26,.54,1.78,.025,'Steel')
    box(x,1,-.279,.514,1.75,.018,'Paint')
    box(x+.17,1.02,-.307,.028,.14,.04,'Steel')
    label(x,1.58,-.292,.2,.075)
    for y in [1.68,1.71,1.74,.32,.35,.38]:box(x,y,-.291,.30,.009,.007,'Ink',0)
for x in [-.46,.46]:box(x,.045,0,.07,.09,.36,'Steel')
export('StationStaffLocker')

# Workshop bench with drawer bank, backsplash and an orderly perforated tool board.
box(0,.92,0,2.2,.07,.76,'Timber',.02)
for x in [-.98,.98]:
    for z in [-.29,.29]:box(x,.45,z,.055,.87,.055,'Steel')
box(.63,.49,0,.65,.75,.63,'Paint')
for y in [.25,.48,.71]:
    box(.63,y,-.333,.6,.2,.025,'Steel')
    box(.63,y,-.363,.23,.025,.035,'Ceramic')
box(0,1.47,.32,2.18,.96,.035,'Paint')
for x in range(19):
    for y in range(7):box(-.98+x*.108,1.11+y*.115,.298,.013,.013,.004,'Ink',0)
for x in [-.72,-.42,-.12,.18,.48]:
    box(x,1.45,.277,.026,.34,.025,'Steel')
    box(x,1.63,.277,.085,.058,.025,'Steel')
box(-.55,.2,0,.76,.035,.59,'Paint')
export('StationWorkbench')

# Alarm panel has a fixed control face. Runtime status lens mounts inside its upper recess.
box(0,0,0,.46,.59,.13,'Paint',.018)
box(0,0,-.075,.42,.55,.025,'Ceramic')
label(0,.175,-.092,.28,.06)
box(-.105,.02,-.108,.11,.11,.04,'Red',.025)
box(.105,.02,-.108,.11,.11,.04,'Ink',.025)
for y in [-.13,-.16,-.19]:box(0,y,-.094,.27,.009,.004,'Ink',0)
for x in [-.185,.185]:
    for y in [-.245,.245]:box(x,y,-.095,.014,.014,.008,'Steel',.005)
export('StationAlarmPanel')

# Office desk at a realistic working height, with cable slot and file pedestal.
box(0,.735,0,1.8,.06,.82,'Timber',.018)
box(.59,.36,.02,.51,.7,.68,'Paint')
for y in [.2,.43,.63]:
    box(.59,y,-.337,.46,.15,.025,'Paint')
    box(.59,y,-.363,.17,.018,.035,'Steel')
for z in [-.30,.30]:box(-.76,.36,z,.045,.70,.045,'Steel')
box(0,.755,0,.64,.009,.39,'Ink',.005)
box(-.55,.77,.1,.27,.024,.2,'Paper',.003)
export('StationOfficeDesk')

box(0,.012,0,.30,.024,.20,'Steel',.012)
box(0,.105,.055,.05,.18,.045,'Steel')
box(0,.29,.035,.54,.33,.035,'Steel',.012)
box(0,.29,.014,.49,.282,.006,'Ink',.004)
box(-.07,.32,.009,.20,.004,.004,'Paper',0)
box(-.09,.30,.009,.16,.004,.004,'Paper',0)
export('StationMonitor')

box(0,.038,0,.24,.06,.20,'Ink',.018)
for x in [-.035,0,.035]:
    for z in [-.05,-.02,.01,.04]:box(x,.073,z,.024,.01,.018,'Ceramic',.004)
box(-.085,.085,0,.042,.04,.18,'Steel',.018)
for z in [-.072,.072]:box(-.085,.078,z,.064,.04,.055,'Ink',.018)
export('StationDeskPhone')
box(0,-.1,0,1.60,.20,.25,'Steel',0)
export('StationThreshold')
for x in [-.775,.775]:box(x,1.125,0,.09,2.25,.32,'Timber',0)
box(0,2.22,0,1.64,.12,.32,'Timber',0)
export('StationDoorLining')
bpy.context.window.scene=original
bpy.data.scenes.remove(scene)
