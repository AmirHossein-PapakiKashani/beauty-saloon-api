import json
import os
import sqlite3
import uuid
from mcp.server.fastmcp import FastMCP

# ۱. ساخت نمونه سرور
mcp = FastMCP("Balsamiq-Builder")

# دیتابیس حافظه موقت برای نگهداری المان‌های صفحه جاری
current_wireframe = {
    "title": "New Wireframe",
    "width": 1024,
    "height": 768,
    "controls": []
}

@mcp.tool()
def init_canvas(title: str = "Main Page", width: int = 1024, height: int = 768) -> str:
    """ایجاد یک بوم خالی برای طراحی صفحه"""
    global current_wireframe
    current_wireframe = {
        "title": title,
        "width": width,
        "height": height,
        "controls": []
    }
    return f"بوم جدید با عنوان '{title}' و ابعاد {width}x{height} ایجاد شد."

@mcp.tool()
def add_control(
    control_type: str,
    x: int,
    y: int,
    w: int = -1,
    h: int = -1,
    text: str = "",
    properties: dict | None = None
) -> str:
    """
    اضافه کردن یک المان UI به وایرفریم Balsamiq.
    انواع مجاز control_type شامل:
    BrowserWindow, Button, TextInput, TextArea, Label, Title, Subtitle,
    DataGrid, ComboBox, CheckBox, RadioButton, SearchBox, Icon, Image, Paragraph
    """
    props = properties or {}
    if text:
        props["text"] = text

    control_id = len(current_wireframe["controls"]) + 1
    
    control_data = {
        "ID": control_id,
        "typeID": control_type,
        "x": x,
        "y": y,
        "w": w,
        "h": h,
        "zOrder": control_id,
        "properties": props
    }
    
    current_wireframe["controls"].append(control_data)
    return f"المان '{control_type}' با متن '{text}' در مختصات ({x}, {y}) ثبت شد (شناسه: {control_id})."


@mcp.tool()
def export_wireframe(file_path: str = "wireframe.json") -> str:
    """
    ذخیره وایرفریم طراحی‌شده در قالب استاندارد Balsamiq Wireframe JSON.
    این فایل مستقیماً از منوی Project -> Import -> Wireframe JSON در Balsamiq قابل باز شدن است.
    """
    if not current_wireframe["controls"]:
        return "خطا: هیچ المانی برای ذخیره‌سازی وجود ندارد. ابتدا با add_control المان اضافه کنید."
    balsamiq_json = {
        "version": "1.0",
        "wireframe": {
            "title": current_wireframe["title"],
            "width": current_wireframe["width"],
            "height": current_wireframe["height"],
            "controls": current_wireframe["controls"]
        }
    }
    dir_path = os.path.dirname(file_path)
    if dir_path:
        os.makedirs(dir_path, exist_ok=True)
    with open(file_path, "w", encoding="utf-8") as f:
        json.dump(balsamiq_json, f, ensure_ascii=False, indent=2)
    return f"فایل با موفقیت با {len(current_wireframe['controls'])} المان در '{file_path}' ذخیره شد."


@mcp.tool()
def export_bmpr(file_path: str = "project.bmpr") -> str:
    """
    ذخیره مستقیم پروژه در قالب فایل اصلی و بومی Balsamiq (.bmpr)
    این فایل یک پایگاه‌داده SQLite کاملاً سازگار با Balsamiq Wireframes 4.x است.
    """
    if not current_wireframe["controls"]:
        return "خطا: هیچ المانی برای ذخیره‌سازی وجود ندارد. ابتدا با add_control المان اضافه کنید."

    dir_path = os.path.dirname(file_path)
    if dir_path:
        os.makedirs(dir_path, exist_ok=True)

    if os.path.exists(file_path):
        os.remove(file_path)

    conn = sqlite3.connect(file_path)
    c = conn.cursor()

    # ۱. ایجاد جداول دقیق Balsamiq Wireframes 4
    c.execute('CREATE TABLE INFO (NAME VARCHAR(255) PRIMARY KEY, VALUE TEXT)')
    c.execute('CREATE TABLE BRANCHES (ID VARCHAR(255) PRIMARY KEY, ATTRIBUTES TEXT)')
    c.execute('CREATE TABLE RESOURCES (ID VARCHAR(255), BRANCHID VARCHAR(255), ATTRIBUTES TEXT, DATA LONGTEXT, PRIMARY KEY (ID, BRANCHID), FOREIGN KEY (BRANCHID) REFERENCES BRANCHES(ID))')
    c.execute('CREATE TABLE THUMBNAILS (ID VARCHAR(255) PRIMARY KEY, ATTRIBUTES MEDIUMTEXT)')
    c.execute('CREATE TABLE USERS (ID VARCHAR(255) PRIMARY KEY, ATTRIBUTES TEXT)')
    c.execute('CREATE TABLE COMMENTS (ID VARCHAR(255) PRIMARY KEY, BRANCHID VARCHAR(255), RESOURCEID VARCHAR(255), DATA LONGTEXT, USERID VARCHAR(255), ATTRIBUTES TEXT, FOREIGN KEY (USERID) REFERENCES USERS(ID), FOREIGN KEY (RESOURCEID, BRANCHID) REFERENCES RESOURCES(ID, BRANCHID))')

    # ۲. ثبت متادیتای INFO
    c.execute('INSERT INTO INFO VALUES (?, ?)', ('SchemaVersion', '2.0'))
    c.execute('INSERT INTO INFO VALUES (?, ?)', ('ArchiveFormat', 'bmpr'))
    c.execute('INSERT INTO INFO VALUES (?, ?)', ('ArchiveRevisionUUID', ''))
    c.execute('INSERT INTO INFO VALUES (?, ?)', ('ArchiveAttributes', json.dumps({'name': current_wireframe["title"]})))
    c.execute('INSERT INTO INFO VALUES (?, ?)', ('ArchiveRevision', '1'))

    # ۳. ثبت شاخه اصلی Master
    branches_attr = {
        'fontFace': 'Balsamiq Sans',
        'fontSize': 13,
        'linkColor': 545684,
        'selectionColor': 9813234,
        'skinName': 'sketch'
    }
    c.execute('INSERT INTO BRANCHES VALUES (?, ?)', ('Master', json.dumps(branches_attr)))

    # ۴. ثبت وایرفریم در RESOURCES
    mockup_data = {
        'mockup': {
            'controls': {
                'control': current_wireframe["controls"]
            },
            'measuredH': str(current_wireframe["height"]),
            'measuredW': str(current_wireframe["width"]),
            'mockupH': str(current_wireframe["height"]),
            'mockupW': str(current_wireframe["width"]),
            'version': '1.0'
        }
    }

    res_id = str(uuid.uuid4()).upper()
    res_attr = {
        'mimeType': 'text/vnd.balsamiq.bmml',
        'kind': 'mockup',
        'creationDate': 0,
        'trashed': False,
        'name': current_wireframe["title"],
        'importedFrom': '',
        'order': 1000000
    }

    c.execute('INSERT INTO RESOURCES VALUES (?, ?, ?, ?)',
              (res_id, 'Master', json.dumps(res_attr, ensure_ascii=False), json.dumps(mockup_data, ensure_ascii=False)))

    # ۵. ثبت تصویر بندانگشتی پیش‌فرض
    thumb_id = str(uuid.uuid4()).upper()
    thumb_attr = {
        'image': 'iVBORw0KGgoAAAANSUhEUgAAAGQAAABkCAYAAABw4pVUAAAAPklEQVR42u3BMQEAAADCoPVPbQsvoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgKcBnKQAAdh7HF4AAAAASUVORK5CYII=',
        'resourceID': res_id,
        'branchID': 'Master'
    }
    c.execute('INSERT INTO THUMBNAILS VALUES (?, ?)', (thumb_id, json.dumps(thumb_attr)))

    conn.commit()
    conn.close()

    return f"فایل اصلی Balsamiq (.bmpr) با موفقیت در '{file_path}' ساخته شد."


if __name__ == "__main__":
    mcp.run(transport="stdio")
