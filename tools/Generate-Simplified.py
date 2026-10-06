"""Derive a mutually exclusive Simplified Chinese edition from the TC package.

Requires the build-time opencc-python-reimplemented package. Generated installers
have no Python/OpenCC dependency. English lookup keys and regexes stay unchanged.
"""
import argparse
import json
import re
import shutil
from pathlib import Path

from opencc import OpenCC

repo = Path(__file__).resolve().parent.parent
converter = OpenCC("tw2s")


def convert(value):
    return converter.convert(value)


def product_text(value):
    return (convert(value).replace("繁体中文", "简体中文")
            .replace("繁中", "简中").replace("Traditional Chinese", "Simplified Chinese"))


def write_text(path, value, bom=False):
    path.parent.mkdir(parents=True, exist_ok=True)
    normalized = value.replace("\r\n", "\n").replace("\r", "\n")
    path.write_bytes(normalized.replace("\n", "\r\n").encode("utf-8-sig" if bom else "utf-8"))


def build(destination):
    if destination.exists():
        raise FileExistsError("Output already exists; preserve it before regenerating.")
    destination.mkdir(parents=True)
    for name in ("LICENSE", "LICENSE-SCOPE.md"):
        shutil.copy2(repo / name, destination / name)
    for directory in ("payload", "source", "licenses", "installer"):
        shutil.copytree(repo / directory, destination / directory)
    for name in ("Install-TraditionalChinese.ps1", "Set-TraditionalChinese.ps1",
                 "Uninstall-TraditionalChinese.ps1", "Easy-Setup.ps1", "Install.cmd", "Uninstall.cmd"):
        text = (repo / name).read_text(encoding="utf-8-sig")
        write_text(destination / name, product_text(text), bom=name.endswith(".ps1"))
    for name in ("Build-Pack.ps1", "Test-Pack.ps1", "Test-Catalog.ps1"):
        text = (repo / "tools" / name).read_text(encoding="utf-8-sig")
        write_text(destination / "tools" / name,
                   product_text(text).replace("Asbury_Pines_TC_", "Asbury_Pines_SC_"), bom=True)
    # The precomputed TC measurements must never be relabeled as SC measurements.
    (destination / "source" / "layout-cache.json.gz").unlink()
    plugin = (repo / "source" / "Plugin.cs").read_text(encoding="utf-8-sig")
    plugin = product_text(plugin).replace("Default uses Windows Simplified Chinese font.",
                                         "Default uses Microsoft JhengHei for shared layout behavior.")
    write_text(destination / "source" / "Plugin.cs", plugin, bom=True)
    bootstrap = (repo / "installer" / "Bootstrap.cs").read_text(encoding="utf-8-sig")
    write_text(destination / "installer" / "Bootstrap.cs", product_text(bootstrap), bom=True)
    rows = 0
    changed = 0
    replacements = 0
    translations = destination / "payload" / "BepInEx" / "plugins" / "AsburyPines.ZhHant" / "translations"
    for path in sorted(translations.glob("*.json")):
        original = json.loads(path.read_text(encoding="utf-8-sig"))
        for row in original:
            key = "replacement" if path.name == "rules.json" else "target"
            if key in row and row[key] is not None:
                previous = row[key]
                row[key] = convert(previous)
                changed += row[key] != previous
                rows += key == "target"
                replacements += key == "replacement"
        write_text(path, json.dumps(original, ensure_ascii=False, indent=2) + "\n")
    (destination / "docs").mkdir()
    shutil.copytree(repo / "tools" / "simplified-template", destination, dirs_exist_ok=True)
    report = {"conversion": "OpenCC tw2s", "files": len(list(translations.glob('*.json'))),
              "translation_rows": rows, "dynamic_rules": replacements, "changed_outputs": changed,
              "source_keys_unchanged": True, "regex_patterns_unchanged": True,
              "traditional_layout_cache_omitted": True}
    write_text(destination / "docs" / "CONVERSION.json", json.dumps(report, ensure_ascii=False, indent=2))
    print(json.dumps(report, ensure_ascii=True))


def verify(destination):
    original = repo / "payload" / "BepInEx" / "plugins" / "AsburyPines.ZhHant" / "translations"
    output = destination / "payload" / "BepInEx" / "plugins" / "AsburyPines.ZhHant" / "translations"
    if sorted(p.name for p in original.glob("*.json")) != sorted(p.name for p in output.glob("*.json")):
        raise AssertionError("Translation file set changed.")
    rich_text = re.compile(r"<[^>]+>")
    substitutions = re.compile(r"\$\d+|\$\{[^}]+\}")
    count = 0
    for path in sorted(original.glob("*.json")):
        tc = json.loads(path.read_text(encoding="utf-8-sig"))
        sc = json.loads((output / path.name).read_text(encoding="utf-8-sig"))
        if len(tc) != len(sc):
            raise AssertionError("Row count changed: " + path.name)
        for before, after in zip(tc, sc):
            field = "replacement" if path.name == "rules.json" else "target"
            expected = dict(before)
            if expected.get(field) is not None:
                expected[field] = convert(expected[field])
                for pattern in (rich_text, substitutions):
                    if pattern.findall(before[field]) != pattern.findall(after[field]):
                        raise AssertionError("Markup or replacement placeholders changed: " + path.name)
                for char in ("\n", "\r", "\t"):
                    if before[field].count(char) != after[field].count(char):
                        raise AssertionError("Layout separators changed: " + path.name)
            if after != expected:
                raise AssertionError("Unexpected translation conversion: " + path.name)
            count += 1
    if (destination / "source" / "layout-cache.json.gz").exists():
        raise AssertionError("Stale TC measurements were included.")
    print("PASS: {} translation/rule rows; keys, regexes, tags, placeholders and layout separators preserved.".format(count))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--verify", action="store_true")
    parser.add_argument("--output", type=Path, default=repo / "simplified")
    args = parser.parse_args()
    destination = args.output.resolve()
    if destination != repo / "simplified":
        raise ValueError("The generated edition must stay in this repo's simplified directory.")
    if not args.verify:
        build(destination)
    verify(destination)
