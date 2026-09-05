"""Author the September station shell in Blender; export metre-scale FBX + layout.

Run with Blender --background --factory-startup --python this_file.
Coordinates in the layout are Unity X/Z; Blender's FBX export reverses X and Y.
No existing Blender document or Unity scene is modified by this script.
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Art/Environment/StationRebuild'
SOURCE = ROOT / 'ArtSource/StationRebuild'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(parents=True, exist_ok=True)
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)

palette = {
    'Plaster': (.68, .66, .60, 1), 'Sage': (.24, .29, .27, 1),
    'Stone': (.34, .35, .33, 1), 'Ceiling': (.74, .73, .69, 1),
    'Oak': (.25, .15, .085, 1), 'Metal': (.12, .14, .145, 1),
    'Glass': (.40, .51, .58, 1), 'Diffuser': (.92, .92, .86, 1),
    'Floor': (.40, .40, .36, 1), 'Tile': (.48, .49, .45, 1),
    'Brass': (.40, .31, .16, 1)
}
materials = {}
for key, color in palette.items():
    m = bpy.data.materials.get(key) or bpy.data.materials.new(key)
    m.diffuse_color = color
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = .72 if key != 'Metal' else .4
    materials[key] = m

def box(name, location, size, material, bevel=0):
    location = (-location[0], -location[1], location[2])
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    o = bpy.context.object
    o.name = name
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(materials[material])
    if bevel:
        m = o.modifiers.new('Milled edges', 'BEVEL')
        m.width = bevel
        m.segments = 2
        bpy.ops.object.modifier_apply(modifier=m.name)
        n = o.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
        bpy.ops.object.modifier_apply(modifier=n.name)
    # Metre-scale planar UVs, chosen per polygon normal.
    uv = o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
    for p in o.data.polygons:
        axis = max(range(3), key=lambda k: abs(p.normal[k]))
        axes = [k for k in range(3) if k != axis]
        for li in p.loop_indices:
            v = o.data.vertices[o.data.loops[li].vertex_index].co + o.location
            uv.data[li].uv = (v[axes[0]], v[axes[1]])
    return o

rooms = [
    ('interrogation', 'pokoj-przesluchan', '01  PRZESLUCHANIA', -3, 3, -2, 3, 'Floor'),
    ('common', 'sala-wspolna', '02  SALA WSPOLNA', -6, 6, 6.25, 14, 'Floor'),
    ('archive', 'archiwum', '03  ARCHIWUM', -15, -6.25, -5, 1, 'Floor'),
    ('evidence', 'dowody', '04  DEPOZYT', -15, -6.25, 1.25, 8, 'Tile'),
    ('office', 'biuro', '05  BIURO', -15, -6.25, 8.25, 14, 'Floor'),
    ('social', 'pokoj-socjalny', '06  POKOJ SOCJALNY', 6.25, 15, -5, 1, 'Tile'),
    ('workshop', 'warsztat', '07  WARSZTAT', 6.25, 15, 1.25, 8, 'Tile'),
    ('briefing', 'odprawy', '08  SALA ODPRAW', 6.25, 15, 8.25, 14, 'Floor'),
    ('storage', 'magazyn', '09  MAGAZYN', -6, -.125, -13, -5.25, 'Tile'),
    ('reception', 'recepcja', '10  RECEPCJA', .125, 6, -13, -5.25, 'Floor'),
    ('hall_south', 'korytarz', '', -6, 6, -5, -2.25, 'Tile'),
    ('hall_north', 'korytarz', '', -6, 6, 3.25, 6, 'Tile'),
    ('hall_west', 'korytarz', '', -6, -3.25, -2.25, 3.25, 'Tile'),
    ('hall_east', 'korytarz', '', 3.25, 6, -2.25, 3.25, 'Tile'),
]
layout = {'rooms': [], 'doors': [], 'lights': [], 'windows': []}
for key, rid, label, x0, x1, z0, z1, floor in rooms:
    cx, cz = (x0+x1)/2, (z0+z1)/2
    layout['rooms'].append(dict(key=key, id=rid, label=label, x=cx, z=cz, width=x1-x0, depth=z1-z0))
    box('Floor_'+key, (cx, cz, -.10), (x1-x0+.025, z1-z0+.025, .20), floor)
    box('Ceiling_'+key, (cx, cz, 3.53), (x1-x0+.28, z1-z0+.28, .26), 'Ceiling')
    if key.startswith('hall'):
        points = [(cx,cz)] if x1-x0 < 6 else [(x0+2,cz),(cx,cz),(x1-2,cz)]
    elif key == 'interrogation':
        points = [(0,.5)]
    else:
        points = [(cx-2,cz),(cx+2,cz)] if x1-x0 > 7 else [(cx,cz-2),(cx,cz+2)]
    for i,(x,z) in enumerate(points):
        box('Fixture_'+key+str(i), (x,z,3.29), (1.24,.35,.13), 'Metal', .025)
        box('Diffuser_'+key+str(i), (x,z,3.214), (1.12,.27,.025), 'Diffuser', .012)
        layout['lights'].append(dict(name=key+str(i), x=x,z=z,central=key=='interrogation'))

def wall(name, axis, fixed, lo, hi, openings=()):
    # Opening tuples: along-wall center, width, sill, head, kind.
    cuts = [lo,hi]
    for c,w,b,t,k in openings:
        cuts.extend([c-w/2,c+w/2])
    cuts = sorted(set(cuts))
    for i,(a,b) in enumerate(zip(cuts,cuts[1:])):
        mid = (a+b)/2
        opening = next((o for o in openings if abs(mid-o[0])<o[1]/2),None)
        spans = [(0,3.4)] if opening is None else [(0,opening[2]),(opening[3],3.4)]
        for low,high in spans:
            for bottom,top,mat in [(0,.14,'Stone'),(.14,1.15,'Sage'),(1.15,3.4,'Plaster')]:
                y0,y1 = max(low,bottom),min(high,top)
                if y1-y0 <= .001: continue
                loc = (mid,fixed,(y0+y1)/2) if axis=='x' else (fixed,mid,(y0+y1)/2)
                dim = (b-a,.25,y1-y0) if axis=='x' else (.25,b-a,y1-y0)
                box('Wall_'+name+'_'+str(i)+'_'+mat, loc,dim,mat,.008)
            if low <= 1.15 < high:
                loc=(mid,fixed,1.15) if axis=='x' else (fixed,mid,1.15)
                dim=(b-a,.285,.035) if axis=='x' else (.285,b-a,.035)
                box('Rail_'+name+str(i),loc,dim,'Oak',.008)
    for c,w,sill,head,kind in openings:
        height = head-sill
        # Two recessed jambs and lintel; no coplanar cover panels.
        for at in [c-w/2+.025,c+w/2-.025]:
            loc=(at,fixed,(sill+head)/2) if axis=='x' else (fixed,at,(sill+head)/2)
            dim=(.055,.29,height) if axis=='x' else (.29,.055,height)
            box('Jamb_'+name+str(at),loc,dim,'Oak' if kind=='door' else 'Metal',.012)
        loc=(c,fixed,head-.025) if axis=='x' else (fixed,c,head-.025)
        dim=(w,.29,.055) if axis=='x' else (.29,w,.055)
        box('Lintel_'+name+str(c),loc,dim,'Oak' if kind=='door' else 'Metal',.012)
        if kind=='window':
            loc=(c,fixed,(sill+head)/2) if axis=='x' else (fixed,c,(sill+head)/2)
            dim=(w-.07,.025,height-.07) if axis=='x' else (.025,w-.07,height-.07)
            box('Window_'+name+str(c),loc,dim,'Glass')
            dim=(.045,.08,height) if axis=='x' else (.08,.045,height)
            box('Mullion_'+name+str(c),loc,dim,'Metal',.008)
            loc=(c,fixed,sill-.035) if axis=='x' else (fixed,c,sill-.035)
            dim=(w+.16,.42,.075) if axis=='x' else (.42,w+.16,.075)
            box('Sill_'+name+str(c),loc,dim,'Stone',.02)
            layout['windows'].append(dict(x=c if axis=='x' else fixed,z=fixed if axis=='x' else c,axis=axis))

def door(c): return (c,1.6,0,2.25,'door')
def window(c): return (c,2.0,1.35,2.85,'window')

wall('CentralSouth','x',-2.125,-3.25,3.25,[door(0)])
wall('CentralNorth','x',3.125,-3.25,3.25)
wall('CentralWest','z',-3.125,-2,3)
wall('CentralEast','z',3.125,-2,3)
wall('CommonSouth','x',6.125,-6.25,6.25,[door(0)])
wall('SouthRoomsNorth','x',-5.125,-6.25,6.25,[door(-3),door(3)])
wall('SouthDivision','z',0,-13,-5.25)
for sign,side in [(-1,'West'),(1,'East')]:
    wall(side+'Inner','z',sign*6.125,-5,14,[door(-.5),door(4.5),door(10.5)])
    wall(side+'LowerDivision','x',1.125,-15 if sign<0 else 6.25,-6.25 if sign<0 else 15,[door(sign*10.5)])
    wall(side+'UpperDivision','x',8.125,-15 if sign<0 else 6.25,-6.25 if sign<0 else 15,[door(sign*10.5)])
    wall(side+'Outer','z',sign*15.125,-5.25,14.25,[window(-2.5),window(4.5),window(11)])
    wall(side+'WingSouth','x',-5.125,-15.25 if sign<0 else 6.25,-6.25 if sign<0 else 15.25)
    wall(side+'SouthOuter','z',sign*6.125,-13.25,-5.25,[window(-9)])
wall('North','x',14.125,-15.25,15.25,[window(-10.5),window(-3),window(3),window(10.5)])
wall('South','x',-13.125,-6.25,6.25,[window(-3),window(3)])

def portal(name,x,z,angle,a,b,label):
    layout['doors'].append(dict(name=name,x=x,z=z,angle=angle,a=a,b=b,label=label))
portal('DrzwiPrzesluchania',0,-2.125,0,'korytarz','pokoj-przesluchan','01  PRZESLUCHANIA')
portal('DrzwiSala',0,6.125,0,'korytarz','sala-wspolna','02  SALA WSPOLNA')
portal('DrzwiArchiwum',-6.125,-.5,90,'korytarz','archiwum','03  ARCHIWUM')
portal('DrzwiSocjalny',6.125,-.5,90,'korytarz','pokoj-socjalny','06  SOCJALNY')
portal('DrzwiDepozyt',-6.125,4.5,90,'korytarz','dowody','04  DEPOZYT')
portal('DrzwiWarsztat',6.125,4.5,90,'korytarz','warsztat','07  WARSZTAT')
portal('DrzwiBiuro',-6.125,10.5,90,'sala-wspolna','biuro','05  BIURO')
portal('DrzwiOdprawy',6.125,10.5,90,'sala-wspolna','odprawy','08  ODPRAWY')
portal('DrzwiMagazyn',-3,-5.125,0,'korytarz','magazyn','09  MAGAZYN')
portal('DrzwiRecepcja',3,-5.125,0,'korytarz','recepcja','10  RECEPCJA')
portal('DrzwiArchiwumDepozyt',-10.5,1.125,0,'archiwum','dowody','DEPOZYT / ARCHIWUM')
portal('DrzwiDepozytBiuro',-10.5,8.125,0,'dowody','biuro','BIURO / DEPOZYT')
portal('DrzwiSocjalnyWarsztat',10.5,1.125,0,'pokoj-socjalny','warsztat','WARSZTAT / SOCJALNY')
portal('DrzwiWarsztatOdprawy',10.5,8.125,0,'warsztat','odprawy','ODPRAWY / WARSZTAT')

# Reusable furniture built as separate named objects in the same authored source.
# Static steel shelving has real open shelves and braced uprights.
for index,(cx,cz) in enumerate([(-13,3),(-13,6),(-8,6),(-4.9,-9),(-1.2,-11),(13,4),(13,6)]):
    for dx in [-.85,.85]:
        for dz in [-.32,.32]:
            box('ShelfPost_'+str(index),(cx+dx,cz+dz,1.02),(.045,.045,2.04),'Metal',.007)
    for h in [.15,.65,1.15,1.65,2.02]:
        box('Shelf_'+str(index),(cx,cz,h),(1.8,.7,.045),'Metal',.009)
    for level in [.68,1.18,1.68]:
        for j in [-.55,0,.55]:
            box('ArchiveBox_'+str(index),(cx+j,cz,level+.17),(.46,.52,.32),'Oak',.025)

# Combine static pieces by spatial cell and material, keeping ceilings separate.
# This limits renderer count while retaining useful lightmap packing boundaries.
groups={}
for o in list(bpy.context.scene.objects):
    if o.type!='MESH': continue
    cat='Ceiling' if o.name.startswith('Ceiling_') else 'Shell'
    if o.name.startswith(('Shelf','ArchiveBox')): cat='Furniture'
    key=(cat,int(math.floor(o.location.x/6)),int(math.floor(o.location.y/6)),o.data.materials[0].name)
    groups.setdefault(key,[]).append(o)
for (cat,x,z,mat),objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects: o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    if len(objects) > 1:
        bpy.ops.object.join()
    o=bpy.context.object
    o.name=f'{cat}_{x}_{z}_{mat}'
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)

bpy.context.scene.unit_settings.system='METRIC'
bpy.context.scene.unit_settings.scale_length=1
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'StationRebuild.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'StationRebuild.fbx'),use_selection=False,
    object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
    bake_space_transform=True,add_leaf_bones=False,mesh_smooth_type='FACE')
(OUT/'layout.json').write_text(json.dumps(layout,indent=2),encoding='utf-8')
triangles=sum(len(o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH')
print(f'STATION_EXPORT objects={len(bpy.context.scene.objects)} polygons={triangles} rooms=10 doors=14')
