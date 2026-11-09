#!/usr/bin/env python3
"""Put the screens back into a build book. Stdlib only.

The C# Given/When/Then export knows the API's commands, events and views, not the screens. The screens
are observed instead: tests/screens-pass.test.mjs runs each screen against a recording fetch and writes
SCREENS_OUT (routes called, who drew or tapped them, the components on each screen). This script merges
that into an export directory, before scripts/save-gwt-export.py combines and saves it.

  add-screens.py API_EXPORT_DIR SCREENS_JSON
      The API's book (count-me-in-api). API_EXPORT_DIR holds the *.test.json files and the routes.json the
      C# tests write. Every scenario of a view the web draws gets  usedBy: [{screen, how: "draws", path}],
      every scenario of a command a control sends gets  how: "taps"  (path: [component, screen file]),
      and seenOn.json (screen -> components seen on it) is written for save-gwt-export.py to carry into the
      saved book as "seenOn".

  add-screens.py --web WEB_EXPORT_DIR SCREENS_JSON [API_EXPORT_DIR]
      The web's book (count-me-in-web). The component tests wrote one scenario each (view, value, component,
      shows); this adds chapter (the screen), the API's view name, componentAt, usedBy, one tap scenario per
      control and command, and seenOn.json. API_EXPORT_DIR (optional) names views and commands as the API does.

Route -> API name is read from the API, not a table: routes.json gives each route's owning class
(PitchCard, ChooseThemeEndpoint); the export's scenarios give that class its view or command name
(slice.type PitchCard -> "Pitch Card"; X + "Endpoint" -> X + "Slice" -> its When). A route whose class has
no scenarios keeps the class name, spaced. A problem is a warning and exit 0: the tests are the gate.
"""
import glob, json, os, re, sys


def warn(msg):
    print(f'::warning::{msg}' if os.environ.get('GITHUB_ACTIONS') else f'warning: {msg}', file=sys.stderr)


def scenarios_of(data):
    return data if isinstance(data, list) else data.get('scenarios', []) if isinstance(data, dict) else []


def load_dir(d):
    out = []
    for f in sorted(glob.glob(os.path.join(d, '*.test.json'))):
        with open(f, encoding='utf-8') as fh:
            out.append((f, json.load(fh)))
    return out


def save(f, data):
    with open(f, 'w', encoding='utf-8') as fh:
        json.dump(data, fh, indent=1, ensure_ascii=False)


def spaced(cls):
    return re.sub(r'(?<=[a-z])(?=[A-Z])', ' ', re.sub(r'(Endpoint|Slice)$', '', cls))


def api_names(api_dir):
    """(method, route) -> API view/command name, from routes.json and the export."""
    routes_file = os.path.join(api_dir, 'routes.json')
    if not os.path.isfile(routes_file):
        warn(f'{routes_file} is missing: were the C# tests run with GWT_EXPORT? API names unknown.')
        return {}
    with open(routes_file, encoding='utf-8') as fh:
        routes = json.load(fh)
    by_type = {}
    for _, data in load_dir(api_dir):
        for s in scenarios_of(data):
            t = ((s.get('slice') or {}).get('type') or '').split('.')[-1]
            then = s.get('then') if isinstance(s.get('then'), dict) else {}
            name = then.get('view') or s.get('when')
            if t and name:
                by_type[t] = name
    out = {}
    for r in routes:
        t = r['type']
        base = re.sub(r'Endpoint$', '', t)
        out[(r['method'], r['route'])] = by_type.get(t) or by_type.get(base + 'Slice') or spaced(t)
    return out


def usedby(entry):
    return {'screen': entry['screen'], 'how': entry['how'], 'path': [entry['component'], entry['screenFile']]}


def add_unique(scenario, items):
    have = scenario.setdefault('usedBy', [])
    for u in items:
        if u not in have:
            have.append(u)


def write_seen_on(d, screens):
    save(os.path.join(d, 'seenOn.json'), screens['seenOn'])


def api_mode(d, screens):
    names = api_names(d)
    by_name = {}
    for e in screens['observed']:
        name = names.get((e['method'], e['route']))
        if name is None:
            warn(f"{e['screen']}: {e['method']} {e['route']} is not an API route")
            continue
        by_name.setdefault((e['how'], name), []).append(usedby(e))
    n = 0
    for f, data in load_dir(d):
        for s in scenarios_of(data):
            then = s.get('then') if isinstance(s.get('then'), dict) else {}
            how, name = ('draws', then.get('view')) if then.get('view') else ('taps', s.get('when'))
            if name and (how, name) in by_name:
                add_unique(s, by_name[(how, name)])
                n += 1
        save(f, data)
    write_seen_on(d, screens)
    print(f'screens: usedBy on {n} scenarios, seenOn for {len(screens["seenOn"])} screens')


def web_mode(d, screens, api_dir):
    names = api_names(api_dir) if api_dir else {}
    seen_on, files, comps = screens['seenOn'], screens['screenFiles'], screens['components']
    view_name = {}  # view slice -> the API's name for it
    for e in screens['observed']:
        if e['how'] == 'draws':
            view_name.setdefault(e['slice'], names.get((e['method'], e['route'])) or spaced(e['slice'].title().replace('-', '')))
    n = 0
    for f, data in load_dir(d):
        keep = []
        for s in scenarios_of(data):
            then = s.get('then') or {}
            c = then.get('component')
            if then.get('view') == '(none)':
                continue  # a control: the tap scenarios below say what it sends
            if c:
                shown = [sc for sc, cs in seen_on.items() if c in cs]
                s['usedBy'] = [{'screen': sc, 'how': 'draws', 'path': [c, files[sc]]} for sc in shown]
                if shown:
                    s['chapter'] = shown[0]
                src = then['view']
                then['view'] = view_name.get(src, src)
                if c in comps:
                    then['componentAt'] = {'method': c, 'file': comps[c]['file'], 'line': comps[c]['line']}
                    s['slice'] = {'type': f"{comps[c]['slice']}/slice", 'method': c, 'file': comps[c]['file'], 'line': comps[c]['line']}
                n += 1
            keep.append(s)
        save(f, keep if isinstance(data, list) else {**data, 'scenarios': keep})
    taps, seen = [], set()
    for e in screens['observed']:
        if e['how'] != 'taps':
            continue
        cmd = names.get((e['method'], e['route'])) or spaced(e['slice'].title().replace('-', ''))
        key = (e['screen'], e['component'], cmd)
        if key in seen:
            continue
        seen.add(key)
        at = comps.get(e['component'], {'file': f"src/web/slices/{e['slice']}.slice.js", 'line': 1})
        taps.append({
            'chapter': e['screen'], 'title': f"{e['component']} sends {cmd}", 'given': [], 'when': cmd, 'then': {'events': []},
            'tap': e['component'], 'tapAt': {'method': e['component'], 'file': at['file'], 'line': at['line']},
            'usedBy': [usedby(e)],
            'spec': {'file': 'tests/screens-pass.test.mjs', 'line': 1},
            'slice': {'type': f"{e['slice']}/command", 'method': 'send', 'file': f"src/web/slices/{e['slice']}.slice.js", 'line': 1},
        })
    save(os.path.join(d, 'screens-pass.test.json'), taps)
    write_seen_on(d, screens)
    print(f'screens: usedBy on {n} component scenarios, {len(taps)} tap scenarios, seenOn for {len(seen_on)} screens')


def main(argv):
    web = '--web' in argv
    args = [a for a in argv if a != '--web']
    if len(args) < 2 or (not web and len(args) != 2) or len(args) > 3:
        print(__doc__, file=sys.stderr)
        return 2
    d, screens_file = args[0], args[1]
    try:
        with open(screens_file, encoding='utf-8') as fh:
            screens = json.load(fh)
        if not os.path.isdir(d):
            warn(f'{d} does not exist. Nothing changed.')
            return 0
        screens.setdefault('screenFiles', {})
        for e in screens['observed']:
            screens['screenFiles'][e['screen']] = e['screenFile']
        if web:
            web_mode(d, screens, args[2] if len(args) == 3 else None)
        else:
            api_mode(d, screens)
    except (OSError, ValueError, KeyError) as e:
        warn(f'could not add the screens ({e!r})')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
