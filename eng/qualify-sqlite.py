#!/usr/bin/env python3
"""Opt-in SQLite graph tests; disposable databases and separate builds/processes."""
import argparse
import json
import platform
from pathlib import Path
import shlex
import subprocess
import xml.etree.ElementTree as ET


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True, help="new output directory (never reused or deleted)")
    parser.add_argument("--framework", choices=["net8.0", "net10.0"], action="append")
    parser.add_argument("--default-entry", help="existing default DLL path, with optional {framework}; matrix only, no build/tests/restore")
    parser.add_argument("--candidate-entry", help="existing candidate DLL path, with optional {framework}; requires --default-entry")
    args = parser.parse_args()
    if bool(args.default_entry) != bool(args.candidate_entry):
        parser.error("Provide both entry paths or neither")
    root = Path(__file__).resolve().parents[1]
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    frameworks = args.framework or ["net8.0"]
    os_name = {"Linux": "linux", "Windows": "win", "Darwin": "osx"}[platform.system()]
    architecture = {"x86_64": "x64", "AMD64": "x64", "arm64": "arm64", "aarch64": "arm64"}[platform.machine()]
    rid = f"{os_name}-{architecture}"
    # Protocol shared with MonkeyCache.SqliteCompatibility/Program.cs.
    assertions_per_mode = {"seed": 21, "read": 21, "update": 34, "updated": 18, "clear": 19, "empty": 7}
    fixture = root / "tests/MonkeyCache.SqliteCompatibility/MonkeyCache.SqliteCompatibility.csproj"
    tests = root / "src/MonkeyCache.TestsSQLite/MonkeyCache.TestsSQLite.csproj"
    expected = {
        "default": {
            "sqlite-net-pcl/1.9.172", "SQLitePCLRaw.bundle_green/2.1.11",
            "SQLitePCLRaw.core/2.1.11", "SQLitePCLRaw.provider.e_sqlite3/2.1.11",
            "SQLitePCLRaw.lib.e_sqlite3/2.1.14", "SQLitePCLRaw.lib.e_sqlite3.android/2.1.14",
        },
        "candidate": {
            "sqlite-net-pcl/1.11.285", "SQLitePCLRaw.core/3.0.3",
            "SQLitePCLRaw.provider.e_sqlite3/3.0.3", "SourceGear.sqlite3/3.53.3",
        },
    }
    report = {"platform": platform.platform(), "rid": rid, "frameworks": frameworks, "graphs": {}, "commands": [],
              "processes": 0, "assertions": 0, "test_runs": {}, "status": "incomplete",
              "mode": "existing-binary matrix only; no restore/build/provider suites" if args.default_entry else "NuGet qualification"}

    def run(name, command):
        log = output / f"{name}.log"
        report["commands"].append({"log": log.name, "argv": [str(part) for part in command]})
        print(shlex.join(str(part) for part in command), flush=True)
        with log.open("w", encoding="utf-8") as stream:
            result = subprocess.run(command, cwd=root, stdout=stream, stderr=subprocess.STDOUT, timeout=600)
        text = log.read_text(encoding="utf-8", errors="replace")
        if result.returncode:
            raise RuntimeError(f"{name} exited {result.returncode}; see {log}\n{text[-12000:]}")
        return text

    try:
        run("environment", ["dotnet", "--info"])
        for graph in ([] if args.default_entry else expected):
            artifacts = output / "build" / graph
            properties = [f"-p:MonkeyCacheSqliteCandidate={str(graph == 'candidate').lower()}",
                          "-p:MonkeyCachePortable=true", "-p:SelfContained=false"]
            common = ["-c", "Release", "-r", rid, "--artifacts-path", str(artifacts), *properties]
            for framework in frameworks:
                run(f"{graph}-{framework}-tests", ["dotnet", "test", str(tests), "-f", framework, *common,
                    "--logger", "trx", "--results-directory", str(output / "results" / graph / framework)])
                results = list((output / "results" / graph / framework).glob("*.trx"))
                if len(results) != 1:
                    raise RuntimeError(f"Expected one test result for {graph}/{framework}, got {results}")
                counters = ET.parse(results[0]).find(".//{*}Counters").attrib
                if int(counters["total"]) == 0 or counters["passed"] != counters["total"]:
                    raise RuntimeError(f"Missing, failed or skipped tests: {counters}")
                report["test_runs"][f"{graph}/{framework}"] = counters
                executable = output / "runner" / graph / framework
                run(f"{graph}-{framework}-build", ["dotnet", "build", str(fixture), "-f", framework, *common,
                    "--output", str(executable)])
            for project in ["MonkeyCache.SqliteCompatibility", "MonkeyCache.TestsSQLite"]:
                assets = json.loads((artifacts / "obj" / project / "project.assets.json").read_text(encoding="utf-8"))
                selected = {key for key in assets["libraries"] if key.lower().startswith(("sqlite", "sourcegear.sqlite3/"))}
                if selected != expected[graph]:
                    raise RuntimeError(f"Unexpected {graph}/{project} graph: {sorted(selected)}")
                report["graphs"][f"{graph}/{project}"] = sorted(selected)

        for framework in frameworks:
            if not args.default_entry and report["test_runs"][f"default/{framework}"]["total"] != report["test_runs"][f"candidate/{framework}"]["total"]:
                raise RuntimeError(f"Provider test counts differ for {framework}")
            for writer, reader in [("default", "candidate"), ("candidate", "default")]:
                database = output / "databases" / framework / f"{writer}-first"
                sequence = [(writer, "seed"), (writer, "read"), (reader, "read"), (reader, "update"),
                            (writer, "updated"), (writer, "clear"), (reader, "empty")]
                for index, (graph, mode) in enumerate(sequence):
                    executable = output / "runner" / graph / framework / "MonkeyCache.SqliteCompatibility.dll"
                    if args.default_entry:
                        entry = args.default_entry if graph == "default" else args.candidate_entry
                        executable = Path(entry.format(framework=framework)).resolve()
                    text = run(f"{framework}-{writer}-first-{index}-{graph}-{mode}",
                               ["dotnet", str(executable), mode, str(database)])
                    if f"COMPLETE {mode}" not in text.splitlines():
                        raise RuntimeError(f"Missing completion marker: {framework}/{graph}/{mode}")
                    orm = "1.9.172.0" if graph == "default" else "1.11.285.0"
                    raw = "2.1.11.2622" if graph == "default" else "3.0.3.3048"
                    if f"ORM=SQLite-net, Version={orm}," not in text or f"Raw=SQLitePCLRaw.core, Version={raw}," not in text:
                        raise RuntimeError(f"Unexpected loaded assemblies for {graph}")
                    assertions = sum(line.startswith("PASS: ") for line in text.splitlines())
                    if assertions != assertions_per_mode[mode]:
                        raise RuntimeError(f"Unexpected assertion count for {graph}/{mode}: {assertions}, expected {assertions_per_mode[mode]}")
                    report["processes"] += 1
                    report["assertions"] += assertions
        report["status"] = "passed"
        print(f"PASS: {report['processes']} database processes / {report['assertions']} assertions; {report['mode']}", flush=True)
    finally:
        (output / "summary.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
