"""Authored close-range props; run with Blender --background --python this_file."""
import bpy
import math
from pathlib import Path

OUT = Path(__file__).resolve().parents[2] / 'Assets/Art/Environment/StationRebuild'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
materials = {}
for name, color in {'Plastic':(.12,.13,.12,1), 'Glass':(.045,.07,.075,1),
                    'Steel':(.22,.24,.23,1), 'Timber':(.3,.22,.15,1),
                    'Paper':(.7,.66,.53,1), 'Ivory':(.65,.63,.55,1),
                    'Rubber':(.025,.03,.026,1)}.items():
    m = bpy.data.materials.new(name); m.diffuse_color = color; materials[name] = m

def finish(o, mat, bevel=0):
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(materials[mat])
    if bevel:
        b=o.modifiers.new('Machined edges','BEVEL'); b.width=bevel; b.segments=3
        bpy.ops.object.modifier_apply(modifier=b.name)
        b=o.modifiers.new('Corner normals','WEIGHTED_NORMAL')
        bpy.ops.object.modifier_apply(modifier=b.name)
    return o

def box(x,y,z,w,h,d,mat,bevel=.003):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(-x,-z,y))
    o=bpy.context.object; o.dimensions=(w,d,h)
    return finish(o,mat,min(bevel,min(w,h,d)*.2))

def sphere(x,y,z,w,h,d,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,location=(-x,-z,y))
    o=bpy.context.object; o.dimensions=(w,d,h)
    finish(o,mat)
    for p in o.data.polygons:p.use_smooth=True
    return o

def cable(points,r=.003,mat='Rubber'):
    c=bpy.data.curves.new('Cable','CURVE'); c.dimensions='3D'; c.bevel_depth=r; c.bevel_resolution=3
    s=c.splines.new('BEZIER'); s.bezier_points.add(len(points)-1)
    for b,(x,y,z) in zip(s.bezier_points,points):
        b.co=(-x,-z,y); b.handle_left_type='AUTO'; b.handle_right_type='AUTO'
    o=bpy.data.objects.new('Cable',c); bpy.context.collection.objects.link(o)
    bpy.context.view_layer.objects.active=o; o.select_set(True)
    bpy.ops.object.convert(target='MESH'); o=bpy.context.object; o.data.materials.append(materials[mat])
    o.select_set(False)

def screen():
    vertices=[]; faces=[]; nx=32; ny=24
    for j in range(ny+1):
        v=j/ny*2-1
        for i in range(nx+1):
            u=i/nx*2-1
            x=u*.275; y=v*.195
            # A shallow convex rectangular CRT face, not an oval lens.
            z=-.199-.012*(1-u*u)*(1-v*v)
            vertices.append((-(x-.05),-z,y+.34))
    for j in range(ny):
        for i in range(nx):
            a=j*(nx+1)+i; faces.append((a,a+1,a+nx+2,a+nx+1))
    mesh=bpy.data.meshes.new('Curved CRT glass'); mesh.from_pydata(vertices,[],faces); mesh.update()
    o=bpy.data.objects.new('Curved CRT glass',mesh); bpy.context.collection.objects.link(o)
    mesh.materials.append(materials['Glass'])
    for p in mesh.polygons:p.use_smooth=True

def export(name):
    groups={}
    for o in list(bpy.context.scene.objects):groups.setdefault(o.data.materials[0].name,[]).append(o)
    for mat,objects in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for o in objects:o.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        if len(objects)>1:bpy.ops.object.join()
        o=bpy.context.object; o.name=name+'_'+mat
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(island_margin=.02); bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},
        axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False)
    print(name, sum(len(p.vertices)-2 for o in bpy.context.scene.objects for p in o.data.polygons),'triangles')
    bpy.ops.object.delete(use_global=False)

# A serviceable late-20th-century television, with bowed glass, grille, knobs and rear vents.
box(0,.32,.045,.78,.55,.40,'Plastic',.025)
box(-.05,.34,-.167,.59,.43,.055,'Rubber',.02)
screen()
for y in range(17):box(.31,.14+y*.021,-.165,.075,.006,.016,'Rubber',.001)
for y in [.14,.21]:sphere(.31,y,-.198,.038,.038,.026,'Steel')
for x in [-.29,.29]:box(x,.032,.025,.07,.045,.30,'Rubber')
for x in range(15):box(-.29+x*.04,.59,.04,.008,.008,.20,'Rubber',.001)
box(.30,.075,-.166,.045,.012,.008,'Ivory')
cable([(0,.15,.245),(.2,.02,.3),(.35,-.22,.32),(.32,-.50,.32)])
export('FinishTelevision')

# Low credenza, inset doors, feet, top lip and a cable opening at the rear.
box(0,.59,0,1.52,.045,.46,'Timber',.009)
for x in [-.73,.73]:box(x,.34,0,.03,.46,.44,'Timber')
for y in [.12,.34]:box(0,y,0,1.46,.025,.43,'Timber')
box(0,.36,.206,1.45,.44,.015,'Timber')
for x in [-.5,.5]:
    box(x,.35,-.227,.47,.42,.018,'Timber')
    box(x,.50,-.245,.14,.014,.02,'Steel')
for x in [-.61,.61]:
    for z in [-.14,.14]:box(x,.062,z,.04,.12,.04,'Steel')
for y in [.16,.21,.26]:box(0,y,-.05,.36,.035,.26,'Paper')
export('FinishCredenza')

# Keyboard and wired mouse. Individual beveled keys give a legible close silhouette.
box(0,.02,0,.44,.032,.16,'Ivory',.008)
for row in range(5):
    for col in range(14):box(-.201+col*.030,.042,.059-row*.026,.024,.012,.020,'Plastic',.002)
box(-.015,.045,-.055,.15,.012,.020,'Plastic')
sphere(.31,.025,-.01,.06,.045,.095,'Ivory')
box(.31,.047,.008,.003,.005,.052,'Rubber',.001)
cable([(.31,.03,.03),(.32,.007,.18),(.16,.007,.22),(.04,.01,.14)])
cable([(0,.02,.08),(-.05,.006,.22),(.1,.006,.31),(.13,.02,.37)])
export('FinishKeyboard')

# Desk monitor with separate bezel, stand, underside buttons and rear ventilation.
box(0,.29,0,.47,.31,.055,'Plastic',.009)
box(0,.29,-.029,.423,.26,.004,'Glass',.003)
box(0,.11,.02,.05,.18,.05,'Steel')
box(0,.012,0,.22,.025,.16,'Plastic',.009)
for x in range(4):sphere(.10+x*.023,.145,-.035,.009,.006,.007,'Steel')
for x in range(10):box(-.15+x*.032,.4,.03,.012,.06,.006,'Rubber',.001)
cable([(0,.21,.04),(.12,.09,.1),(.17,.005,.08),(.2,.005,.18)])
export('FinishMonitor')

# A toolbox tray, socket set and a coiled power lead arranged for use on a workbench.
box(0,.025,0,.48,.035,.24,'Steel')
for x in [-.24,.24]:box(x,.07,0,.016,.10,.24,'Steel')
for z in [-.12,.12]:box(0,.07,z,.48,.1,.016,'Steel')
for x in range(7):sphere(-.18+x*.06,.065,0,.034,.065,.034,'Steel')
for i in range(3):
    cable([(.48+math.cos(a)*(.10+i*.012),.008+i*.006,math.sin(a)*(.1+i*.012)) for a in [j*math.pi/8 for j in range(17)]])
box(.61,.018,0,.05,.025,.04,'Plastic')
export('FinishToolTray')

# Bundled records with straps, uneven page edges and a folder cover.
for i in range(9):box((i%3-1)*.002,.002+i*.003,0,.25,.0025,.32,'Paper',.0003)
box(0,.031,0,.26,.003,.33,'Timber',.0005)
for x in [-.075,.075]:box(x,.034,0,.012,.002,.33,'Ivory',.0002)
export('FinishRecordBundle')
