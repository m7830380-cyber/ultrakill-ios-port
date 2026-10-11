"""Convert retail ULTRAKILL (Windows) data so the iOS shell player can load it.

  python tools/convert_data.py --game "C:/Program Files (x86)/Steam/steamapps/common/ULTRAKILL" --out work

Produces
  work/Data/      -> goes into ULTRAKILL.app/Data (player data, small)
  work/ULTRAKILL-Content/ -> copied into the app's Documents folder (Addressables bundles, ~3.5 GB)

For every serialized file / asset bundle:
  * the target-platform tag is changed from StandaloneWindows64 (19) to iOS (9)
  * every Shader gets its DX11 programs translated to Metal (see convert_shader.py)
Textures/audio/meshes are left untouched (Unity decompresses unsupported texture formats on load).
"""
import argparse, hashlib, json, os, pickle, shutil, sys, time, traceback
from concurrent.futures import ProcessPoolExecutor, as_completed

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import UnityPy
from convert_shader import convert_shader

IOS_PLATFORM = 9
PLAYER_FILES = ['globalgamemanagers', 'globalgamemanagers.assets', 'level0', 'resources.assets',
                'sharedassets0.assets', 'Resources/unity_builtin_extra']
COPY_WITH = ['.resS', '.resource']


def log(*a):
    print(time.strftime('%H:%M:%S'), *a, flush=True)


def shader_key(tt, blob):
    h = hashlib.sha1()
    h.update(tt['m_ParsedForm']['m_Name'].encode())
    h.update(blob)
    return h.hexdigest()


def convert_shaders_in(sf_objects, cache_dir, work_dir, report):
    n = 0
    for obj in sf_objects:
        if obj.type.name != 'Shader':
            continue
        tt = obj.read_typetree()
        blob = bytes(tt['compressedBlob'])
        name = tt['m_ParsedForm']['m_Name']
        key = shader_key(tt, blob)
        cp = os.path.join(cache_dir, key + '.pkl')
        if os.path.exists(cp):
            nt, stats = pickle.load(open(cp, 'rb'))
        else:
            tt['compressedBlob'] = b''
            msgs = []
            res = convert_shader(tt, blob, os.path.join(work_dir, 'hlslcc', key), log=msgs.append)
            if res is None:
                report.setdefault('no_dx11', []).append(name)
                continue
            nt, stats = res
            stats = dict(stats, msgs=msgs[:50])
            pickle.dump((nt, stats), open(cp, 'wb'))
            shutil.rmtree(os.path.join(work_dir, 'hlslcc', key), ignore_errors=True)
        report.setdefault('shaders', {})[name] = {k: v for k, v in stats.items() if k != 'msgs'}
        if stats.get('fail'):
            report.setdefault('shader_msgs', {})[name] = stats.get('msgs', [])
        obj.save_typetree(nt)
        n += 1
    return n


def retag(sf):
    if hasattr(sf, '_m_target_platform') and sf._m_target_platform != IOS_PLATFORM:
        sf._m_target_platform = IOS_PLATFORM
        sf.mark_changed()


def process_bundle(src, dst, cache_dir, work_dir):
    report = {}
    env = UnityPy.load(src)
    bf = list(env.files.values())[0]
    nshaders = 0
    for name, sf in bf.files.items():
        if not hasattr(sf, 'objects'):
            continue
        nshaders += convert_shaders_in(sf.objects.values(), cache_dir, work_dir, report)
        retag(sf)
    data = bf.save(packer='lz4')
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    with open(dst, 'wb') as f:
        f.write(data)
    return nshaders, report


def process_player_file(src, dst, cache_dir, work_dir):
    report = {}
    env = UnityPy.load(src)
    sf = list(env.files.values())[0]
    n = convert_shaders_in(sf.objects.values(), cache_dir, work_dir, report)
    retag(sf)
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    with open(dst, 'wb') as f:
        f.write(sf.save())
    for ext in COPY_WITH:
        if os.path.exists(src + ext):
            shutil.copyfile(src + ext, dst + ext)
    return n, report


def fix_catalog(out_content):
    """Windows-built catalog uses backslash separators (a plain filename character on iOS)."""
    cat = os.path.join(out_content, 'aa', 'catalog.json')
    c = json.load(open(cat, encoding='utf8'))
    c['m_InternalIds'] = [i.replace('\\', '/') for i in c['m_InternalIds']]
    json.dump(c, open(cat, 'w', encoding='utf8'), separators=(',', ':'))
    st = os.path.join(out_content, 'aa', 'settings.json')
    s = json.load(open(st, encoding='utf8'))
    s['m_buildTarget'] = 'iOS'
    json.dump(s, open(st, 'w', encoding='utf8'), separators=(',', ':'))


def job(kind, src, dst, cache_dir, work_dir):
    try:
        if kind == 'bundle':
            return process_bundle(src, dst, cache_dir, work_dir)
        return process_player_file(src, dst, cache_dir, work_dir)
    except Exception:
        return -1, {'error': traceback.format_exc()}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--game', required=True)
    ap.add_argument('--out', default='work')
    ap.add_argument('--only', default=None, help='substring filter on file names (testing)')
    ap.add_argument('--jobs', type=int, default=3)
    ap.add_argument('--skip-existing', action='store_true')
    a = ap.parse_args()

    data = os.path.join(a.game, 'ULTRAKILL_Data')
    out_data = os.path.join(a.out, 'Data')
    out_content = os.path.join(a.out, 'ULTRAKILL-Content')
    cache_dir = os.path.join(a.out, 'cache', 'shaders')
    work_dir = os.path.join(a.out, 'tmp')
    os.makedirs(cache_dir, exist_ok=True)

    jobs = []
    for f in PLAYER_FILES:
        src = os.path.join(data, f)
        if os.path.exists(src):
            jobs.append(('player', src, os.path.join(out_data, f)))
    aa = os.path.join(data, 'StreamingAssets', 'aa')
    for root, _, files in os.walk(aa):
        for f in files:
            src = os.path.join(root, f)
            rel = os.path.relpath(src, aa)
            dst = os.path.join(out_content, 'aa', rel)
            if f.endswith('.bundle'):
                jobs.append(('bundle', src, dst))
            else:
                os.makedirs(os.path.dirname(dst), exist_ok=True)
                shutil.copyfile(src, dst)
    fix_catalog(out_content)
    if a.only:
        jobs = [j for j in jobs if a.only in j[1]]
    if a.skip_existing:
        jobs = [j for j in jobs if not os.path.exists(j[2])]
    # biggest shader carrier first so the long Master conversion starts early
    jobs.sort(key=lambda j: (0 if 'shaders.bundle' in j[1] else 1, -os.path.getsize(j[1])))

    # other StreamingAssets content that ships inside the app (videos etc.)
    raw = os.path.join(out_data, 'Raw')
    vids = os.path.join(data, 'StreamingAssets', 'Videos')
    if os.path.isdir(vids) and not a.only:
        shutil.copytree(vids, os.path.join(raw, 'Videos'), dirs_exist_ok=True)

    log('converting', len(jobs), 'files')
    full_report = {}
    t0 = time.time()
    # shaders.bundle alone (it parallelises internally through HLSLcc processes)
    first = [j for j in jobs if 'shaders.bundle' in j[1]]
    rest = [j for j in jobs if j not in first]
    for kind, src, dst in first:
        n, rep = job(kind, src, dst, cache_dir, work_dir)
        log('done', os.path.basename(src), 'shaders', n, rep.get('error', ''))
        full_report[src] = rep
    with ProcessPoolExecutor(max_workers=a.jobs) as ex:
        futs = {ex.submit(job, kind, src, dst, cache_dir, work_dir): src for kind, src, dst in rest}
        for fu in as_completed(futs):
            src = futs[fu]
            n, rep = fu.result()
            log('done', os.path.relpath(src, data), 'shaders', n, rep.get('error', ''))
            full_report[src] = rep
    json.dump(full_report, open(os.path.join(a.out, 'convert_report.json'), 'w'), indent=1)
    log('finished in %.0fs' % (time.time() - t0))


if __name__ == '__main__':
    main()
