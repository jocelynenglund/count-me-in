#!/usr/bin/env bash
# Generate whats-new.json from git history — the user-facing changelog the
# "What's new" pill reads. Portable: run locally for dev or in CI at deploy.
# Deps: git + python3 only. Usage: ./whats-new.sh [outfile]  (default whats-new.json)
set -euo pipefail
OUT="${1:-whats-new.json}"

git log --no-merges --pretty=format:'%H%x1f%ad%x1f%s' --date=short -n 200 \
  | WHATS_NEW_LIMIT="${WHATS_NEW_LIMIT:-15}" python3 -c '
import sys, os, json, re
# Skip housekeeping commits — the changelog is for humans, not maintainers.
EXCLUDE = re.compile(r"^(chore|docs|tests?|ci|build|style|refactor)(\(.+?\))?:", re.I)
PREFIX  = re.compile(r"^(feat|fix|perf)(\(.+?\))?:\s*", re.I)   # strip conventional prefix
PRNUM   = re.compile(r"\s*\(#\d+\)\s*$")                        # strip trailing PR number
items = []
for line in sys.stdin:
    line = line.rstrip("\n")
    if not line:
        continue
    parts = line.split("\x1f")
    if len(parts) < 3:
        continue
    sha, date, subj = parts[0], parts[1], parts[2]
    if EXCLUDE.match(subj):
        continue
    subj = PRNUM.sub("", PREFIX.sub("", subj)).strip()
    if not subj:
        continue
    subj = subj[0].upper() + subj[1:]
    items.append({"sha": sha[:7], "date": date, "title": subj})
limit = int(os.environ.get("WHATS_NEW_LIMIT", "15"))
print(json.dumps(items[:limit], ensure_ascii=False, indent=0))
' > "$OUT"

echo "Wrote $OUT"
