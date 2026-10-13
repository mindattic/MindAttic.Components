"""Optional deep font check: load each woff2 with fontTools and print JSON {path: {family, glyphs, error}}.
Used by specs/assets/fonts.spec.mjs when `python` with fontTools (and brotli) is available; skipped otherwise."""
import json
import sys

try:
    from fontTools.ttLib import TTFont
except Exception as e:  # pragma: no cover
    print(json.dumps({"__unavailable__": str(e)}))
    sys.exit(0)

out = {}
for p in sys.argv[1:]:
    try:
        f = TTFont(p)
        out[p] = {"family": f["name"].getDebugName(1), "glyphs": len(f.getGlyphOrder())}
    except Exception as e:
        out[p] = {"error": str(e)}
print(json.dumps(out))
