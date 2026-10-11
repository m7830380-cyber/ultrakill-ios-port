"""Build the final ULTRAKILL.ipa from the CI shell IPA plus the converted game data.

  python tools/package_ipa.py --work work [--shell path/to/ULTRAKILL-shell.ipa] [--embed-content]

Without --shell the latest `shell-latest` release is downloaded with the GitHub CLI.
Output: work/ULTRAKILL.ipa and work/ULTRAKILL-Content (copy that folder into the app's Documents).
With --embed-content the Addressables bundles go inside the IPA instead (big install, no copying).
"""
import argparse, json, os, shutil, subprocess, sys, zipfile

# Player data files that come from the retail game (converted). Everything else in Data/ (IL2CPP
# metadata, "unity default resources", ...) is the iOS shell's own.
RETAIL_DATA = ['globalgamemanagers', 'globalgamemanagers.assets', 'globalgamemanagers.assets.resS',
               'level0', 'level0.resS', 'resources.assets', 'resources.assets.resS',
               'sharedassets0.assets', 'sharedassets0.assets.resS', 'Resources/unity_builtin_extra']
STORE_EXT = ('.bundle', '.resS', '.resource', '.webm', '.assets')


def find_app(payload):
    for n in os.listdir(payload):
        if n.endswith('.app'):
            return os.path.join(payload, n)
    raise SystemExit('no .app in shell ipa')


def merge_rtiol(app_data, retail_data):
    """Union of RuntimeInitializeOnLoads.json from retail and shell (shell adds UltrakillIOS)."""
    p_shell = os.path.join(app_data, 'RuntimeInitializeOnLoads.json')
    p_retail = os.path.join(retail_data, 'RuntimeInitializeOnLoads.json')
    if not os.path.exists(p_retail):
        return
    shell = json.load(open(p_shell)) if os.path.exists(p_shell) else {'root': []}
    retail = json.load(open(p_retail))
    seen = {(e['assemblyName'], e['nameSpace'], e['className'], e['methodName']) for e in shell['root']}
    added = 0
    for e in retail['root']:
        k = (e['assemblyName'], e['nameSpace'], e['className'], e['methodName'])
        if k not in seen:
            shell['root'].append(e)
            seen.add(k)
            added += 1
    json.dump(shell, open(p_shell, 'w'))
    print('RuntimeInitializeOnLoads: +%d retail entries' % added)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--work', default='work')
    ap.add_argument('--shell')
    ap.add_argument('--game', default=r'C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL')
    ap.add_argument('--embed-content', action='store_true')
    a = ap.parse_args()

    shell = a.shell
    if not shell:
        shell = os.path.join(a.work, 'ULTRAKILL-shell.ipa')
        if os.path.exists(shell):
            os.remove(shell)
        subprocess.check_call(['gh', 'release', 'download', 'shell-latest', '-p', 'ULTRAKILL-shell.ipa', '-D', a.work])

    stage = os.path.join(a.work, 'ipa_stage')
    shutil.rmtree(stage, ignore_errors=True)
    with zipfile.ZipFile(shell) as z:
        z.extractall(stage)
    app = find_app(os.path.join(stage, 'Payload'))
    data = os.path.join(app, 'Data')
    print('shell app:', app)

    # drop the shell's own scene/asset files, then copy the converted retail ones
    for n in os.listdir(data):
        if n.startswith(('level', 'sharedassets', 'resources.assets', 'globalgamemanagers')):
            os.remove(os.path.join(data, n))
    conv = os.path.join(a.work, 'Data')
    for rel in RETAIL_DATA:
        src = os.path.join(conv, rel)
        if os.path.exists(src):
            dst = os.path.join(data, rel)
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            shutil.copyfile(src, dst)
    merge_rtiol(data, os.path.join(a.game, 'ULTRAKILL_Data'))

    # StreamingAssets -> Data/Raw
    raw = os.path.join(data, 'Raw')
    os.makedirs(raw, exist_ok=True)
    if os.path.isdir(os.path.join(conv, 'Raw')):
        shutil.copytree(os.path.join(conv, 'Raw'), raw, dirs_exist_ok=True)
    aa_src = os.path.join(a.work, 'ULTRAKILL-Content', 'aa')
    aa_dst = os.path.join(raw, 'aa')
    os.makedirs(aa_dst, exist_ok=True)
    for f in ('catalog.json', 'settings.json'):
        shutil.copyfile(os.path.join(aa_src, f), os.path.join(aa_dst, f))
    if a.embed_content:
        shutil.copytree(os.path.join(aa_src, 'StandaloneWindows64'), os.path.join(aa_dst, 'StandaloneWindows64'), dirs_exist_ok=True)

    out = os.path.join(a.work, 'ULTRAKILL.ipa')
    if os.path.exists(out):
        os.remove(out)
    with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED, allowZip64=True, compresslevel=6) as z:
        for root, dirs, files in os.walk(os.path.join(stage, 'Payload')):
            for f in files:
                p = os.path.join(root, f)
                arc = os.path.relpath(p, stage).replace('\\', '/')
                ct = zipfile.ZIP_STORED if f.endswith(STORE_EXT) else zipfile.ZIP_DEFLATED
                z.write(p, arc, compress_type=ct)
    print('wrote', out, os.path.getsize(out) >> 20, 'MB')

    if not a.embed_content:
        print('copy this folder into the app Documents:', os.path.join(a.work, 'ULTRAKILL-Content'))


if __name__ == '__main__':
    main()
