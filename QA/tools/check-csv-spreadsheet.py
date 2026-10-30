#!/usr/bin/env python3
"""Bounded real Calc probe using only synthetic arithmetic, never customer CSVs.

Requires Node 24, Python 3 and a local soffice executable. An alpha build is
scoped evidence only. Exit 2 means a consumer case timed out/was not accepted.
This does not establish HTTP/SQL/browser or supported Excel acceptance.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import signal
import uuid
import xml.etree.ElementTree as ET
import zipfile

REPO = Path(__file__).resolve().parents[2]
VALUES = ["=1+1", "+SUM(1,2)", "-1+2", "@SUM(1,2)", "\t=1+1", "\r=1+1",
          "\n=1+1", "  =1+1", "\ufeff=1+1", "\tordinary text", "＝1+1",
          "＋1+1", "－1+1", "＠SUM(1,2)", 'comma, "quote"\r\nnext', "नाम", "", "ordinary"]
NS = {"table": "urn:oasis:names:tc:opendocument:xmlns:table:1.0",
      "office": "urn:oasis:names:tc:opendocument:xmlns:office:1.0"}
# Token 7=false avoids forced-text import; token 13=true evaluates formulas.
# https://help.libreoffice.org/latest/en-US/text/shared/guide/csv_params.html
OPTIONS = "44,34,76,1,,1033,false,true,true,false,false,0,true"


def spreadsheet_summary(path):
    with zipfile.ZipFile(path) as archive:
        root = ET.fromstring(archive.read("content.xml"))
    rows = root.findall(".//table:table-row", NS)
    formulas = root.findall(".//table:table-cell[@table:formula]", NS)
    return {"rows": len(rows), "formulaCells": len(formulas), "secondCells": [
        {"type": cell.get("{" + NS["office"] + "}value-type"),
         "value": cell.get("{" + NS["office"] + "}value")}
        for row in rows for cell in row.findall("table:table-cell", NS)[1:2]]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--soffice", default="soffice")
    parser.add_argument("--output-root", type=Path, default=REPO / ".build-check/csv-spreadsheet")
    args = parser.parse_args()
    folder = args.output_root.resolve() / uuid.uuid4().hex
    folder.mkdir(parents=True, exist_ok=False)
    (folder / "input").mkdir(); (folder / "output").mkdir(); (folder / "cache").mkdir()
    version = subprocess.run([args.soffice, "--version"], check=True, capture_output=True, text=True, timeout=15).stdout.strip()
    node = """const fs=require('node:fs'); const {spreadsheetCsv}=require('./apps/web/src/lib/csv.ts');
const values=JSON.parse(fs.readFileSync(0,'utf8'));
process.stdout.write(spreadsheetCsv(values.map((value,index)=>[String(index),value])));"""
    for name, values in [("protected", VALUES), ("protected-nul", VALUES + ["\0=1+1"])]:
        encoded = subprocess.run(["node", "-e", node], cwd=REPO, input=json.dumps(values).encode("utf-8"),
                                 capture_output=True, check=True, timeout=15).stdout
        (folder / "input" / (name + ".csv")).write_bytes(encoded)
    (folder / "input/raw.csv").write_bytes(b'"0","=1+1"\r\n"1","+SUM(1,2)"')
    summary = {"version": version, "csvImportOptions": OPTIONS, "sourceCommit": subprocess.run(
        ["git", "rev-parse", "HEAD"], cwd=REPO, capture_output=True, text=True, check=True).stdout.strip(),
        "encoderSha256": hashlib.sha256((REPO / "apps/web/src/lib/csv.ts").read_bytes()).hexdigest(),
        "probeSha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(), "cases": []}
    accepted = True
    for name in ["raw", "protected", "protected-nul"]:
        # Every invocation owns its profile/cache/output. No home profile is used.
        env = dict(os.environ, XDG_CACHE_HOME=str(folder / "cache"))
        command = [args.soffice, "-env:UserInstallation=" + (folder / ("profile-" + name)).as_uri(),
                   "--headless", "--infilter=Text - txt - csv (StarCalc):" + OPTIONS,
                   "--convert-to", "ods", "--outdir", str(folder / "output"), str(folder / "input" / (name + ".csv"))]
        case = {"name": name, "accepted": False}
        process = subprocess.Popen(command, env=env, stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True)
        try:
            stdout, stderr = process.communicate(timeout=45)
            (folder / (name + ".log")).write_bytes(stdout + stderr)
            output = folder / "output" / (name + ".ods")
            if process.returncode == 0 and output.is_file():
                evidence = spreadsheet_summary(output); case.update(evidence)
                if name == "raw":
                    case["accepted"] = evidence["formulaCells"] >= 1 and evidence["secondCells"][0]["value"] == "2"
                else:
                    expected = len(VALUES) + (name == "protected-nul")
                    case["accepted"] = evidence["rows"] == expected and evidence["formulaCells"] == 0 and all(
                        cell["type"] == "string" for index, cell in enumerate(evidence["secondCells"]) if index != 16)
            else:
                case["reason"] = "conversion did not produce its document"
        except subprocess.TimeoutExpired:
            # Only the fresh process group created above is ours to terminate.
            os.killpg(process.pid, signal.SIGKILL)
            stdout, stderr = process.communicate()
            (folder / (name + ".log")).write_bytes(stdout + stderr)
            case["reason"] = "conversion timeout; not accepted"
        accepted &= case["accepted"]
        summary["cases"].append(case)
        print(name + ": " + ("PASS" if case["accepted"] else "BLOCKED/FAIL"), flush=True)
    (folder / "summary.json").write_text(json.dumps(summary, indent=2) + "\n")
    print("Evidence: " + str(folder))
    print("Scoped synthetic consumer evidence only; stable supported consumers and HTTP/SQL/browser acceptance remain separate.")
    return 0 if accepted else 2


if __name__ == "__main__":
    raise SystemExit(main())
