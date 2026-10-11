"""Summarise work/convert_report.json."""
import json, os, sys

r = json.load(open(sys.argv[1] if len(sys.argv) > 1 else 'work/convert_report.json'))
for f, rep in r.items():
    sh = rep.get('shaders', {})
    ok = sum(v['ok'] for v in sh.values())
    fail = sum(v['fail'] for v in sh.values())
    if rep.get('error'):
        print(os.path.basename(f), 'ERROR', rep['error'][-600:])
        continue
    if not sh:
        continue
    print(os.path.basename(f), 'shaders', len(sh), 'ok', ok, 'fail', fail, 'no_dx11', rep.get('no_dx11'))
    for n, v in sh.items():
        if v['fail'] or n == 'ULTRAKILL/Master':
            print('   ', n, v)
    for n, m in list(rep.get('shader_msgs', {}).items())[:10]:
        print('   MSG', n, m[:3])
