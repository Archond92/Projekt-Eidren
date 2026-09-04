from __future__ import annotations

import sys
from pathlib import Path

import UnityPy


def main() -> int:
    if len(sys.argv) != 2:
        print("Usage: Inspect-UnityVisualAssets.py <data.unity3d>", file=sys.stderr)
        return 2

    source = Path(sys.argv[1])
    env = UnityPy.load(str(source))
    rows: list[tuple[str, str, str]] = []

    for obj in env.objects:
        if obj.type.name not in {"Sprite", "Texture2D"}:
            continue
        try:
            data = obj.read()
            name = getattr(data, "m_Name", "") or "<unnamed>"
            dimensions = ""
            if obj.type.name == "Texture2D":
                dimensions = f"{getattr(data, 'm_Width', '?')}x{getattr(data, 'm_Height', '?')}"
            rows.append((obj.type.name, name, dimensions))
        except Exception as exc:
            rows.append((obj.type.name, f"<read-error:{type(exc).__name__}>", ""))

    for asset_type, name, dimensions in sorted(rows, key=lambda row: (row[0], row[1].lower())):
        print(f"{asset_type}\t{name}\t{dimensions}")
    print(f"TOTAL\t{len(rows)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
