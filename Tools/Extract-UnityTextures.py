from __future__ import annotations

import argparse
from pathlib import Path

import UnityPy


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Extract named Texture2D assets from a Unity data bundle.")
    parser.add_argument("bundle", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("names", nargs="+")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    wanted = set(args.names)
    args.output.mkdir(parents=True, exist_ok=True)
    env = UnityPy.load(str(args.bundle))
    exported: set[str] = set()

    for obj in env.objects:
        if obj.type.name != "Texture2D":
            continue
        data = obj.read()
        name = getattr(data, "m_Name", "")
        if name not in wanted:
            continue
        destination = args.output / f"{name}.png"
        data.image.save(destination)
        print(destination)
        exported.add(name)

    missing = wanted - exported
    if missing:
        print("Missing: " + ", ".join(sorted(missing)))
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
