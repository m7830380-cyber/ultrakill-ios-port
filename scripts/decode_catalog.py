"""Decodes an Addressables 1.21 catalog.json into a plain JSON map of bundles, their assets and the scene keys.

Usage: python decode_catalog.py <catalog.json> <out.json>
"""
import base64
import json
import struct
import sys


def read_object(data, pos):
    kind = data[pos]
    pos += 1
    if kind == 0:  # AsciiString
        (n,) = struct.unpack_from("<i", data, pos)
        return data[pos + 4:pos + 4 + n].decode("ascii"), kind
    if kind == 1:  # UnicodeString
        (n,) = struct.unpack_from("<i", data, pos)
        return data[pos + 4:pos + 4 + n].decode("utf-16-le"), kind
    if kind == 2:
        return struct.unpack_from("<H", data, pos)[0], kind
    if kind == 3:
        return struct.unpack_from("<I", data, pos)[0], kind
    if kind == 4:
        return struct.unpack_from("<i", data, pos)[0], kind
    if kind == 5:  # Hash128 as length-prefixed ascii
        n = data[pos]
        return data[pos + 1:pos + 1 + n].decode("ascii"), kind
    if kind == 7:  # JsonObject
        n = data[pos]
        pos += 1 + n
        n = data[pos]
        class_name = data[pos + 1:pos + 1 + n].decode("ascii")
        pos += 1 + n
        (n,) = struct.unpack_from("<i", data, pos)
        text = data[pos + 4:pos + 4 + n].decode("utf-16-le")
        return {"class": class_name, "json": json.loads(text)}, kind
    raise ValueError("unsupported object type %d at %d" % (kind, pos - 1))


def main(catalog_path, out_path):
    with open(catalog_path, encoding="utf-8") as f:
        cat = json.load(f)

    keys_data = base64.b64decode(cat["m_KeyDataString"])
    bucket_data = base64.b64decode(cat["m_BucketDataString"])
    entry_data = base64.b64decode(cat["m_EntryDataString"])
    extra_data = base64.b64decode(cat["m_ExtraDataString"])
    internal_ids = cat["m_InternalIds"]
    providers = cat["m_ProviderIds"]
    types = [t["m_ClassName"] for t in cat["m_resourceTypes"]]

    (bucket_count,) = struct.unpack_from("<i", bucket_data, 0)
    pos = 4
    keys = []
    buckets = []
    for _ in range(bucket_count):
        offset, count = struct.unpack_from("<ii", bucket_data, pos)
        pos += 8
        entries = list(struct.unpack_from("<%di" % count, bucket_data, pos))
        pos += 4 * count
        keys.append(read_object(keys_data, offset)[0])
        buckets.append(entries)

    (entry_count,) = struct.unpack_from("<i", entry_data, 0)
    entries = []
    for i in range(entry_count):
        internal, provider, dep_key, _dep_hash, data_index, primary, rtype = struct.unpack_from("<7i", entry_data, 4 + i * 28)
        entries.append({
            "internalId": internal_ids[internal],
            "provider": providers[provider],
            "depKey": dep_key,
            "dataIndex": data_index,
            "primaryKey": keys[primary],
            "type": types[rtype],
        })

    bundles = {}
    for e in entries:
        if e["provider"].endswith("AssetBundleProvider"):
            options = read_object(extra_data, e["dataIndex"])[0] if e["dataIndex"] >= 0 else None
            name = e["internalId"].split("\\", 2)[-1]
            bundles[e["primaryKey"]] = {"file": name, "options": options["json"] if options else None, "assets": []}

    scenes = []
    for e in entries:
        if e["provider"].endswith("AssetBundleProvider"):
            continue
        deps = buckets[e["depKey"]] if e["depKey"] >= 0 else []
        dep_bundles = [entries[d]["primaryKey"] for d in deps]
        own = dep_bundles[0] if dep_bundles else None
        record = {"internalId": e["internalId"], "primaryKey": e["primaryKey"], "type": e["type"]}
        if own in bundles:
            bundles[own]["assets"].append(record)
        if e["type"].endswith("SceneInstance"):
            scenes.append({"key": e["primaryKey"], "internalId": e["internalId"], "bundle": own})

    with open(out_path, "w", encoding="utf-8") as f:
        json.dump({"bundles": list(bundles.values()), "scenes": scenes}, f, indent=1)

    print("keys=%d entries=%d bundles=%d scenes=%d" % (len(keys), len(entries), len(bundles), len(scenes)))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
