#!/usr/bin/env python3
"""Read-only NuGet freshness report; never changes versions or restores projects."""
import argparse
import datetime
import json
from pathlib import Path
import urllib.request
import xml.etree.ElementTree as ET


def report(root, transitive):
    packages = {}
    for pattern in ("**/*.csproj", "**/*.projitems", "**/packages.config"):
        for path in sorted(root.glob(pattern)):
            if "obj" in path.parts or "bin" in path.parts:
                continue
            for node in ET.parse(path).getroot().iter():
                tag = node.tag.split("}")[-1]
                if tag not in ("PackageReference", "package"):
                    continue
                name = node.get("Include") or node.get("id")
                version = node.get("Version") or node.get("version")
                if not version:
                    version = next((n.text for n in node if n.tag.split("}")[-1] == "Version"), None)
                if name:
                    packages.setdefault(name.lower(), []).append({
                        "version": version, "source": str(path.relative_to(root)), "kind": "declared"
                    })
    if transitive:
        for path in sorted(root.glob("**/obj/**/project.assets.json")):
            for key, library in json.loads(path.read_text()).get("libraries", {}).items():
                if library["type"] == "package":
                    name, version = key.rsplit("/", 1)
                    packages.setdefault(name.lower(), []).append({
                        "version": version, "source": str(path.relative_to(root)), "kind": "resolved"
                    })
    result = {
        "observedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "note": "Latest stable is not a compatibility recommendation. Resolved entries cover only existing assets files; restore each platform separately for a complete graph.",
        "packages": [],
    }
    for name, references in sorted(packages.items()):
        url = f"https://api.nuget.org/v3-flatcontainer/{name}/index.json"
        with urllib.request.urlopen(url, timeout=60) as response:
            versions = json.load(response)["versions"]
        stable = [version for version in versions if "-" not in version]
        latest = max(stable, key=lambda version: tuple(int(part) for part in version.split(".")))
        result["packages"].append({
            "id": name, "latestStable": latest, "source": url, "references": references
        })
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--include-transitive", action="store_true")
    args = parser.parse_args()
    print(json.dumps(report(Path(__file__).resolve().parents[1], args.include_transitive), indent=2))
