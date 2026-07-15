#!/usr/bin/env python3
import argparse
import json
from pathlib import Path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--path", type=Path, required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--target-abi", required=True)
    parser.add_argument("--source-url", required=True)
    parser.add_argument("--checksum", required=True)
    parser.add_argument("--timestamp", required=True)
    parser.add_argument("--changelog", required=True)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    plugin = {
        "guid": "4c4061d6-1c66-4a34-99f9-bbf618c51621",
        "name": "Send to Kindle",
        "overview": "Convert books and send them to Kindle from Jellyfin",
        "description": (
            "Adds an administrator-only Send to Kindle action to supported books. "
            "Comics use KCC and other ebook formats use Calibre before SMTP delivery."
        ),
        "owner": "vitorpy",
        "category": "General",
        "versions": [],
    }

    if args.path.exists():
        document = json.loads(args.path.read_text(encoding="utf-8"))
        if not isinstance(document, list) or len(document) != 1:
            raise ValueError("Existing manifest must contain exactly one plugin entry.")
        existing = document[0]
        if existing.get("guid") != plugin["guid"]:
            raise ValueError("Existing manifest GUID does not match this plugin.")
        plugin["versions"] = existing.get("versions", [])

    plugin["versions"] = [
        version for version in plugin["versions"] if version.get("version") != args.version
    ]
    plugin["versions"].insert(
        0,
        {
            "version": args.version,
            "changelog": args.changelog,
            "targetAbi": args.target_abi,
            "sourceUrl": args.source_url,
            "checksum": args.checksum,
            "timestamp": args.timestamp,
        },
    )
    args.path.write_text(json.dumps([plugin], indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
