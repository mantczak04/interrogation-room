"""Download selected CC0 Poly Haven source assets, preserving source URLs and hashes.

Powered by Poly Haven. https://polyhaven.com/license
This authoring script has no runtime or Unity scene dependencies.
"""
import concurrent.futures
import hashlib
import json
from pathlib import Path
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Art/Environment/StationRebuild/Scanned'
SELECTION = {
    'painted_plaster_wall': 'texture', 'terrazzo_tiles': 'texture',
    'concrete_floor_worn_001': 'texture', 'oak_wood_planks': 'texture',
    'rusty_painted_metal': 'texture', 'grey_plaster_03': 'texture',
    'drawer_cabinet': 'model', 'power_box_01': 'model',
    'potted_plant_04': 'model', 'binder_notebook': 'model',
    'metal_toolbox': 'model', 'cardboard_box_01': 'model',
    'fire_alarm': 'model', 'desk_lamp_arm_01': 'model',
}

def fetch(url):
    req = urllib.request.Request(url, headers={'User-Agent': 'InterrogationRoomAssetAuthoring/1.0'})
    with urllib.request.urlopen(req, timeout=90) as response:
        return response.read()

def download(entry, destination):
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists() and hashlib.md5(destination.read_bytes()).hexdigest() == entry['md5']:
        return
    data = fetch(entry['url'])
    if hashlib.md5(data).hexdigest() != entry['md5']:
        raise ValueError('Asset hash mismatch: ' + entry['url'])
    destination.write_bytes(data)

def asset(item):
    name, kind = item
    catalog = json.loads(fetch('https://api.polyhaven.com/files/' + name))
    folder = OUT / name
    record = {'id': name, 'kind': kind, 'source': 'https://polyhaven.com/a/' + name, 'license': 'CC0', 'files': []}
    for channel, choices in [('albedo', ['Diffuse', 'diff']), ('normal', ['nor_gl']), ('arm', ['arm'])]:
        key = next((k for k in choices if k in catalog), None)
        if key is None:
            raise ValueError('Missing ' + channel + ' for ' + name)
        formats = catalog[key]['2k']
        ext = next(e for e in ['jpg', 'png', 'exr'] if e in formats)
        entry = formats[ext]
        target = folder / (channel + '.' + ext)
        download(entry, target)
        record['files'].append({'channel': channel, 'path': target.relative_to(ROOT).as_posix(), 'url': entry['url'], 'md5': entry['md5']})
    if kind == 'model':
        entry = catalog['fbx']['2k']['fbx']
        target = folder / (name + '.fbx')
        download(entry, target)
        record['files'].append({'channel': 'model', 'path': target.relative_to(ROOT).as_posix(), 'url': entry['url'], 'md5': entry['md5']})
    print(name + ' downloaded and verified', flush=True)
    return record

if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        records = list(pool.map(asset, SELECTION.items()))
    (OUT / 'sources.json').write_text(json.dumps({'provider': 'Powered by Poly Haven', 'license': 'https://polyhaven.com/license', 'assets': records}, indent=2), encoding='utf-8')
