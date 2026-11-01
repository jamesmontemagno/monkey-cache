#!/usr/bin/env python3
"""Offline controller/argument fixtures, NOT NuGet or native-platform qualification."""
import contextlib
import importlib.util
import io
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch


def load(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + ".py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


qualifier = load("qualify-sqlite")
dependencies = load("dependency-report")
COUNTS = {"seed": 21, "read": 21, "update": 34, "updated": 18, "clear": 19, "empty": 7}
GRAPHS = {
    "default": ["sqlite-net-pcl/1.9.172", "SQLitePCLRaw.bundle_green/2.1.11",
                "SQLitePCLRaw.core/2.1.11", "SQLitePCLRaw.provider.e_sqlite3/2.1.11",
                "SQLitePCLRaw.lib.e_sqlite3/2.1.14", "SQLitePCLRaw.lib.e_sqlite3.android/2.1.14"],
    "candidate": ["sqlite-net-pcl/1.11.285", "SQLitePCLRaw.core/3.0.3",
                  "SQLitePCLRaw.provider.e_sqlite3/3.0.3", "SourceGear.sqlite3/3.53.3"],
}


class ControllerTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory(prefix="monkeycache-tooling-")
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name)
        self.output = self.root / "output"
        self.commands = []
        self.fault = None

    def invoke(self, matrix_only=False, frameworks=("net8.0", "net10.0")):
        args = ["qualify-sqlite.py", "--output", str(self.output)]
        for framework in frameworks:
            args += ["--framework", framework]
        if matrix_only:
            args += ["--default-entry", str(self.root / "default/{framework}.dll"),
                     "--candidate-entry", str(self.root / "candidate/{framework}.dll")]
        with patch.object(sys, "argv", args), patch.object(qualifier.subprocess, "run", self.run_fixture), \
                patch.object(qualifier.platform, "platform", return_value="fixture-host"), \
                patch.object(qualifier.platform, "system", return_value="Linux"), \
                patch.object(qualifier.platform, "machine", return_value="x86_64"), \
                contextlib.redirect_stdout(io.StringIO()):
            qualifier.main()
        return json.loads((self.output / "summary.json").read_text())

    def run_fixture(self, command, *, cwd, stdout, stderr, timeout):
        command = [str(part) for part in command]
        self.commands.append(command)
        self.assertEqual(cwd, Path(__file__).resolve().parents[1])
        self.assertEqual(stderr, subprocess.STDOUT)
        self.assertEqual(timeout, 600)
        if self.fault == "timeout":
            raise subprocess.TimeoutExpired(command, timeout)
        if self.fault == "process":
            stdout.write("fixture process failed\n")
            return subprocess.CompletedProcess(command, 9)
        if command[1] in ("test", "build"):
            graph = "candidate" if "-p:MonkeyCacheSqliteCandidate=true" in command else "default"
            self.assertIn(f"-p:MonkeyCacheSqliteCandidate={str(graph == 'candidate').lower()}", command)
            self.assertIn("-p:MonkeyCachePortable=true", command)
            self.assertIn("-p:SelfContained=false", command)
            artifacts = Path(command[command.index("--artifacts-path") + 1])
            self.assertEqual(artifacts, self.output / "build" / graph)
            framework = command[command.index("-f") + 1]
            project = Path(command[2]).stem
            assets = artifacts / "obj" / project / "project.assets.json"
            assets.parent.mkdir(parents=True, exist_ok=True)
            libraries = GRAPHS[graph] + (["SQLitePCLRaw.bundle_green/2.1.11"] if graph == "candidate" and self.fault == "mixed" else [])
            assets.write_text(json.dumps({"libraries": dict.fromkeys(libraries, {})}))
            if command[1] == "test" and self.fault != "missing-trx":
                results = Path(command[command.index("--results-directory") + 1])
                results.mkdir(parents=True)
                total = 3 if graph == "candidate" and self.fault == "count" else 2
                passed = 1 if self.fault == "skipped" else total
                (results / "fixture.trx").write_text(f'<TestRun><Counters total="{total}" passed="{passed}"/></TestRun>')
            elif command[1] == "build":
                self.assertEqual(Path(command[command.index("--output") + 1]), self.output / "runner" / graph / framework)
        elif command[1] != "--info":
            graph = "candidate" if "candidate" in Path(command[1]).parts else "default"
            mode = command[2]
            orm, raw = ("1.9.172.0", "2.1.11.2622") if graph == "default" else ("1.11.285.0", "3.0.3.3048")
            stdout.write(f"ORM=SQLite-net, Version={orm}, Culture=neutral\nRaw=SQLitePCLRaw.core, Version={raw}, Culture=neutral\n")
            if self.fault != "no-assertions":
                stdout.write("PASS: synthetic controller fixture\n" * COUNTS[mode])
            if self.fault != "completion":
                stdout.write(f"COMPLETE {mode}\n")
        return subprocess.CompletedProcess(command, 0)

    def test_normal_mode_isolates_graphs_and_frameworks(self):
        result = self.invoke()
        self.assertEqual(result["mode"], "NuGet qualification")
        self.assertEqual((result["processes"], result["assertions"]), (28, 564))
        self.assertEqual(len(result["graphs"]), 4)
        self.assertEqual(len(result["test_runs"]), 4)
        self.assertEqual(len({c[-1] for c in self.commands if len(c) == 4}), 4)

    def test_matrix_only_never_builds_tests_or_restores(self):
        result = self.invoke(matrix_only=True)
        self.assertIn("matrix only", result["mode"])
        self.assertFalse(result["graphs"])
        self.assertFalse(result["test_runs"])
        self.assertFalse(any(c[1] in ("build", "test", "restore") for c in self.commands))

    def test_success_markers_without_assertions_are_rejected(self):
        self.fault = "no-assertions"
        with self.assertRaisesRegex(RuntimeError, "assertion count"):
            self.invoke(matrix_only=True, frameworks=("net8.0",))

    def test_missing_completion_is_rejected(self):
        self.fault = "completion"
        with self.assertRaisesRegex(RuntimeError, "completion marker"):
            self.invoke(matrix_only=True, frameworks=("net8.0",))

    def test_failed_process_stops_and_preserves_incomplete_summary(self):
        self.fault = "process"
        with self.assertRaisesRegex(RuntimeError, "exited 9"):
            self.invoke()
        self.assertEqual(len(self.commands), 1)
        self.assertEqual(json.loads((self.output / "summary.json").read_text())["status"], "incomplete")

    def test_timeout_stops_and_preserves_summary(self):
        self.fault = "timeout"
        with self.assertRaises(subprocess.TimeoutExpired):
            self.invoke()
        self.assertTrue((self.output / "summary.json").is_file())

    def test_trx_and_dependency_failures_are_not_success(self):
        for fault, message in [("missing-trx", "one test result"), ("skipped", "failed or skipped"),
                               ("count", "test counts differ"), ("mixed", "Unexpected candidate")]:
            with self.subTest(fault=fault):
                self.output = self.root / fault
                self.fault = fault
                with self.assertRaisesRegex(RuntimeError, message):
                    self.invoke(frameworks=("net8.0",))


class DependencyTests(unittest.TestCase):
    def test_both_classic_and_artifacts_layouts_are_inventoried(self):
        with tempfile.TemporaryDirectory(prefix="monkeycache-inventory-") as directory:
            root = Path(directory)
            for relative, version in [("src/P/obj/project.assets.json", "1.0.0"),
                                      ("artifacts/build/candidate/obj/P/project.assets.json", "2.0.0")]:
                path = root / relative
                path.parent.mkdir(parents=True)
                path.write_text(json.dumps({"libraries": {f"Fixture/{version}": {"type": "package"}}}))
            with patch.object(dependencies.urllib.request, "urlopen",
                              return_value=io.BytesIO(b'{"versions":["1.0.0","2.0.0"]}')) as request:
                result = dependencies.report(root, True)
            self.assertEqual({r["version"] for r in result["packages"][0]["references"]}, {"1.0.0", "2.0.0"})
            self.assertEqual(request.call_count, 1)


if __name__ == "__main__":
    unittest.main()
