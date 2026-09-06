"""Rebuild Balsamiq diagrams using ONLY control typeIDs proven to open in Balsamiq 4.8.x.

Broken types that crash the editor (from 6472-core log):
  - Subtitle  (must be SubTitle)
  - Arrow     (invalid)
  - RoundButton (unsafe / not in working projects)

Valid set from agents-folder-structure.json / agents-loop-architecture.json:
  BrowserWindow, Title, SubTitle, Label, Paragraph, TextArea, Button,
  FieldSet, Tree, Alert, Canvas, DataGrid
"""
from __future__ import annotations

import json
import os
import sqlite3
import uuid

OUT_DIR = r"E:\barber\beauty-saloon-api\docs\architecture"

ALLOWED = {
    "BrowserWindow",
    "Title",
    "SubTitle",
    "Label",
    "Paragraph",
    "TextArea",
    "Button",
    "FieldSet",
    "Tree",
    "Alert",
    "Canvas",
    "DataGrid",
}


def new_canvas(title: str, width: int, height: int) -> dict:
    return {"title": title, "width": width, "height": height, "controls": []}


def add(wf: dict, type_id: str, x: int, y: int, w: int, h: int, text: str = "", **extra) -> int:
    if type_id not in ALLOWED:
        raise ValueError(f"Invalid Balsamiq typeID for BW 4.8: {type_id}")
    cid = len(wf["controls"]) + 1
    props = dict(extra)
    if text:
        props["text"] = text
    wf["controls"].append(
        {
            "ID": str(cid),
            "typeID": type_id,
            "x": str(x),
            "y": str(y),
            "w": str(w),
            "h": str(h),
            "zOrder": str(cid - 1),
            "properties": props,
        }
    )
    return cid


def export_json(wf: dict, path: str) -> None:
    payload = {"version": "1.0", "wireframe": wf}
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False, indent=2)


def export_bmpr(wf: dict, path: str) -> None:
    # Same schema as e:\Balsamiq_mcp\server.py export_bmpr
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    if os.path.exists(path):
        os.remove(path)
    conn = sqlite3.connect(path)
    c = conn.cursor()
    c.execute("CREATE TABLE INFO (NAME VARCHAR(255) PRIMARY KEY, VALUE TEXT)")
    c.execute("CREATE TABLE BRANCHES (ID VARCHAR(255) PRIMARY KEY, ATTRIBUTES TEXT)")
    c.execute(
        "CREATE TABLE RESOURCES (ID VARCHAR(255), BRANCHID VARCHAR(255), ATTRIBUTES TEXT, "
        "DATA LONGTEXT, PRIMARY KEY (ID, BRANCHID), FOREIGN KEY (BRANCHID) REFERENCES BRANCHES(ID))"
    )
    c.execute("CREATE TABLE THUMBNAILS (ID VARCHAR(255) PRIMARY KEY, ATTRIBUTES MEDIUMTEXT)")
    c.execute("CREATE TABLE USERS (ID VARCHAR(255) PRIMARY KEY, ATTRIBUTES TEXT)")
    c.execute(
        "CREATE TABLE COMMENTS (ID VARCHAR(255) PRIMARY KEY, BRANCHID VARCHAR(255), "
        "RESOURCEID VARCHAR(255), DATA LONGTEXT, USERID VARCHAR(255), ATTRIBUTES TEXT, "
        "FOREIGN KEY (USERID) REFERENCES USERS(ID), "
        "FOREIGN KEY (RESOURCEID, BRANCHID) REFERENCES RESOURCES(ID, BRANCHID))"
    )
    c.execute("INSERT INTO INFO VALUES (?, ?)", ("SchemaVersion", "2.0"))
    c.execute("INSERT INTO INFO VALUES (?, ?)", ("ArchiveFormat", "bmpr"))
    c.execute("INSERT INTO INFO VALUES (?, ?)", ("ArchiveRevisionUUID", ""))
    c.execute(
        "INSERT INTO INFO VALUES (?, ?)",
        ("ArchiveAttributes", json.dumps({"name": wf["title"]})),
    )
    c.execute("INSERT INTO INFO VALUES (?, ?)", ("ArchiveRevision", "1"))
    branches_attr = {
        "fontFace": "Balsamiq Sans",
        "fontSize": 13,
        "linkColor": 545684,
        "selectionColor": 9813234,
        "skinName": "sketch",
    }
    c.execute("INSERT INTO BRANCHES VALUES (?, ?)", ("Master", json.dumps(branches_attr)))
    mockup_data = {
        "mockup": {
            "controls": {"control": wf["controls"]},
            "measuredH": str(wf["height"]),
            "measuredW": str(wf["width"]),
            "mockupH": str(wf["height"]),
            "mockupW": str(wf["width"]),
            "version": "1.0",
        }
    }
    res_id = str(uuid.uuid4()).upper()
    res_attr = {
        "mimeType": "text/vnd.balsamiq.bmml",
        "kind": "mockup",
        "creationDate": 0,
        "trashed": False,
        "name": wf["title"],
        "importedFrom": "",
        "order": 1000000,
    }
    c.execute(
        "INSERT INTO RESOURCES VALUES (?, ?, ?, ?)",
        (res_id, "Master", json.dumps(res_attr, ensure_ascii=False), json.dumps(mockup_data, ensure_ascii=False)),
    )
    thumb_id = str(uuid.uuid4()).upper()
    thumb_attr = {
        "image": "iVBORw0KGgoAAAANSUhEUgAAAGQAAABkCAYAAABw4pVUAAAAPklEQVR42u3BMQEAAADCoPVPbQsvoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgKcBnKQAAdh7HF4AAAAASUVORK5CYII=",
        "resourceID": res_id,
        "branchID": "Master",
    }
    c.execute("INSERT INTO THUMBNAILS VALUES (?, ?)", (thumb_id, json.dumps(thumb_attr)))
    conn.commit()
    conn.close()


def build_folder_structure() -> dict:
    wf = new_canvas("Backend Agent .agents Folder Structure (v2)", 1400, 1000)
    add(wf, "BrowserWindow", 20, 20, 1360, 960, "beauty-saloon-api / .agents — Skills vs State vs Agents")
    add(wf, "Title", 50, 80, 980, 36, "Folder Blueprint — Three Buckets (How / Where / Who)")
    add(
        wf,
        "Label",
        50,
        120,
        1200,
        24,
        "Skills = static HOW | State = runtime WHERE | agents/ = persona WHO | Script = run-backend-cycle.ps1",
    )

    # Left: .agents tree
    add(wf, "FieldSet", 50, 170, 420, 700, ".agents/ (approved v2 layout)")
    add(
        wf,
        "Tree",
        70,
        210,
        380,
        520,
        "F .agents\n"
        " > rules/\n"
        "  - always-on.md\n"
        "  - architecture-laws.md\n"
        " > skills/\n"
        "  > backend-cycle/SKILL.md\n"
        "  > unit-test/SKILL.md\n"
        "  > integration-test/SKILL.md\n"
        "  > clean-code/SKILL.md\n"
        "  > code-review/SKILL.md\n"
        "  > architecture-check/\n"
        "  > storyteller/SKILL.md\n"
        "  > storyconnector/SKILL.md\n"
        "  > eventwriter/SKILL.md\n"
        "  > scenariowriter/SKILL.md\n"
        "  > prompt-engineer/\n"
        "  > handoff/\n"
        " > state/\n"
        "  - queue.json\n"
        "  - progress.md\n"
        "  - current-plan.md\n"
        "  - current-prompt.txt\n"
        "  > handoff-reports/\n"
        " > plans/\n"
        "  - target.md\n"
        "  - phases.md\n"
        "  - bootstrap.md\n"
        " > agents/\n"
        "  - orchestrator.md\n"
        "  - implementer.md\n"
        "  - reviewer.md\n"
        "  - tester.md\n"
        " > logs/",
    )
    add(wf, "Label", 70, 750, 380, 40, "Never mix Skill files with State files")

    # Middle: repo
    add(wf, "FieldSet", 500, 170, 420, 700, "beauty-saloon-api/")
    add(
        wf,
        "Tree",
        520,
        210,
        380,
        400,
        "F beauty-saloon-api\n"
        " - CONTEXT.md\n"
        " - BarberSalon.slnx\n"
        " - run-backend-cycle.ps1\n"
        " > src/\n"
        "  > Domain/\n"
        "  > Application/\n"
        "  > Infrastructure/\n"
        "  > API/\n"
        " > tests/\n"
        "  > Domain.Tests/\n"
        "  > Application.Tests/\n"
        "  > IntegrationTests/\n"
        " > docs/architecture/\n"
        "  - this wireframe",
    )
    add(wf, "SubTitle", 520, 640, 360, 28, "Hard rules")
    add(
        wf,
        "Paragraph",
        520,
        675,
        380,
        140,
        "1. IRepository lives in Domain\n"
        "2. Dotnet build + test gate every UC\n"
        "3. Ponytail / YAGNI always-on\n"
        "4. Unit of work = full vertical slice\n"
        "5. First UC = GetActiveSalonServices",
    )

    # Right: test hierarchy
    add(wf, "FieldSet", 950, 170, 400, 700, "API → Event → Scenario")
    add(wf, "SubTitle", 970, 210, 360, 28, "Test coverage model")
    add(
        wf,
        "Paragraph",
        970,
        250,
        360,
        200,
        "Use Case\n"
        "  → eventwriter: Events\n"
        "     (groups of API actions)\n"
        "  → scenariowriter: Scenarios\n"
        "     unit + integration cases\n"
        "  → unit-test skill\n"
        "  → integration-test skill\n\n"
        "storyteller: front → build path\n"
        "storyconnector: API → front map",
    )
    add(wf, "Alert", 970, 480, 360, 80, "Import tip: open .bmpr OR Project → Import → Wireframe JSON")
    add(
        wf,
        "TextArea",
        970,
        590,
        360,
        200,
        "CLI Inject adapters:\n"
        "- OpenCode: opencode run --auto\n"
        "- Antigravity: agy --print ...\n"
        "- Hermes: hermes chat -q --yolo\n\n"
        "Wire in run-backend-cycle.ps1 Phase 3",
    )
    return wf


def build_loop_architecture() -> dict:
    wf = new_canvas("Backend Headless Agent Loop Architecture (v2)", 1400, 1050)
    add(wf, "BrowserWindow", 20, 20, 1360, 1010, "BarberSalon — Hybrid Orchestrator C")
    add(wf, "Title", 50, 80, 1100, 36, "Autonomous Loop — Select → Enrich → Inject → Verify → Close")
    add(
        wf,
        "Label",
        50,
        120,
        1250,
        24,
        "Script owns queue/gates/tests | LLM enriches prompt only | Subagent implements | Done = green dotnet test",
    )

    # Phase strip as buttons + labels for arrows
    phases = [
        (50, "0 Bootstrap\n(thin once)"),
        (250, "1 Select\nqueue.json"),
        (450, "2 Enrich\nLLM prompt"),
        (650, "3 Inject\nsubagent"),
        (850, "4 Verify\ndotnet test"),
        (1050, "5 Close\nhandoff"),
    ]
    for x, text in phases:
        add(wf, "Button", x, 170, 170, 70, text)
    for x, label in ((225, ">"), (425, ">"), (625, ">"), (825, ">"), (1025, ">")):
        add(wf, "Label", x, 190, 20, 30, label)

    add(wf, "Label", 50, 260, 1100, 24, "loop: if pending remain → back to Select   |   DONE when queue has no pending")

    add(wf, "FieldSet", 50, 310, 400, 320, "Orchestrator Script")
    add(
        wf,
        "TextArea",
        70,
        350,
        360,
        250,
        "run-backend-cycle.ps1\n\n"
        "• INIT / RESUME / DONE / BLOCKED\n"
        "• dependency-aware Select\n"
        "• reset stuck in-progress\n"
        "• write current-prompt.txt\n"
        "• call CLI inject\n"
        "• dotnet test (max 3 retries)\n"
        "• update queue + progress",
    )

    add(wf, "FieldSet", 480, 310, 400, 320, "Enrich (LLM)")
    add(
        wf,
        "TextArea",
        500,
        350,
        360,
        250,
        "Reads:\n"
        "  CONTEXT.md\n"
        "  architecture-laws\n"
        "  base-template\n"
        "  eventwriter + scenariowriter\n"
        "  storyteller\n\n"
        "Writes:\n"
        "  current-prompt.txt\n"
        "  current-plan.md\n\n"
        "CANNOT pick next use case",
    )

    add(wf, "FieldSet", 910, 310, 400, 320, "Implementer Subagent")
    add(
        wf,
        "TextArea",
        930,
        350,
        360,
        250,
        "Reads current-prompt.txt first\n\n"
        "1 Domain (+ IRepository)\n"
        "2 Application Handler\n"
        "3 Infrastructure Repo\n"
        "4 API Controller\n"
        "5 Unit tests\n"
        "6 Integration tests\n\n"
        "Out-of-scope enforced\n"
        "Handoff on green",
    )

    add(wf, "SubTitle", 50, 660, 500, 28, "Personas (.agents/agents/)")
    add(wf, "Button", 50, 700, 200, 60, "Orchestrator\nmanage loop")
    add(wf, "Button", 280, 700, 200, 60, "Implementer\nTDD + layers")
    add(wf, "Button", 510, 700, 200, 60, "Reviewer\narch checklist")
    add(wf, "Button", 740, 700, 200, 60, "Tester\nscenario cover")

    add(wf, "FieldSet", 980, 660, 330, 200, "Gates")
    add(
        wf,
        "Paragraph",
        1000,
        700,
        290,
        140,
        "GREEN → completed\n"
        "RED → fix ≤3 retries\n"
        "FAIL×3 → failed STOP\n"
        "DONE → no pending\n"
        "BLOCKED → unmet deps",
    )

    add(wf, "SubTitle", 50, 800, 600, 28, "Inject CLI adapters (Phase 3)")
    add(wf, "Button", 50, 840, 280, 55, "OpenCode\nopencode run --auto")
    add(wf, "Button", 360, 840, 320, 55, "Antigravity\nagy --print --skip-permissions")
    add(wf, "Button", 710, 840, 300, 55, "Hermes\nhermes chat -q --yolo")

    add(
        wf,
        "Alert",
        50,
        930,
        1260,
        50,
        "First UC: GetActiveSalonServices | Full vertical slice | Thin bootstrap | Hybrid Orchestrator C",
    )
    return wf


def main() -> None:
    folder = build_folder_structure()
    loop = build_loop_architecture()

    # Validate no bad types slipped in
    for name, wf in (("folder", folder), ("loop", loop)):
        bad = {c["typeID"] for c in wf["controls"]} - ALLOWED
        if bad:
            raise SystemExit(f"{name} has invalid types: {bad}")

    paths = [
        (folder, "agents-folder-structure-v2.json", "agents-folder-structure-v2.bmpr"),
        (loop, "agents-loop-architecture-v2.json", "agents-loop-architecture-v2.bmpr"),
    ]
    for wf, jn, bn in paths:
        export_json(wf, os.path.join(OUT_DIR, jn))
        export_bmpr(wf, os.path.join(OUT_DIR, bn))
        print(f"OK {jn} controls={len(wf['controls'])} types={sorted({c['typeID'] for c in wf['controls']})}")


if __name__ == "__main__":
    main()
