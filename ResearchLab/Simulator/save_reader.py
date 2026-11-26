"""Read-only Bannerlord 1.5.7 save metadata and framing, based on SAV-003.
No game assemblies, no object deserialization, no save writes.
"""
import hashlib
import json
import struct
import zlib
from pathlib import Path

MAX_FILE = 128 * 1024 * 1024
MAX_PAYLOAD = 128 * 1024 * 1024
MAX_BLOCKS = 2_000_000
VERSION = "v1.5.7.125802"

def inspect_save(path):
    path = Path(path)
    with path.open("rb") as stream:
        data = stream.read(MAX_FILE + 1)
    if len(data) > MAX_FILE or len(data) < 5:
        raise ValueError("unsupported file size")
    digest = hashlib.sha256(data).hexdigest()
    size = struct.unpack_from("<i", data)[0]
    if not 0 < size <= 65536 or 4 + size >= len(data):
        raise ValueError("invalid metadata boundary")
    meta = json.loads(data[4:4+size].decode("utf-8"))["List"]
    if meta.get("ApplicationVersion") != VERSION or str(meta.get("Version")) != "1":
        raise ValueError("unsupported save version")
    dec = zlib.decompressobj(-zlib.MAX_WBITS)
    payload = dec.decompress(data[4+size:], MAX_PAYLOAD + 1)
    if len(payload) > MAX_PAYLOAD or not dec.eof or dec.unused_data or dec.unconsumed_tail:
        raise ValueError("invalid or oversized compressed stream")
    pos = 0
    def integer():
        nonlocal pos
        if pos + 4 > len(payload):
            raise ValueError("truncated integer")
        n = struct.unpack_from("<i", payload, pos)[0]
        pos += 4
        if n < 0:
            raise ValueError("negative length")
        return n
    def block():
        nonlocal pos
        n = integer()
        start = pos
        pos += n
        if pos > len(payload):
            raise ValueError("truncated block")
        return start, n
    _, head = block()
    def blocks():
        count = integer()
        if count > MAX_BLOCKS:
            raise ValueError("block count limit")
        total = 0
        for _ in range(count):
            _, length = block()
            total += length
        return count, total
    oc, ob = blocks()
    cc, cb = blocks()
    start, sb = block()
    if pos != len(payload):
        raise ValueError("trailing payload bytes")
    if meta.get("SavePredictedSizes") != f"h={head} obj={ob} ctr={cb} str={sb}":
        raise ValueError("metadata section size mismatch")
    strings = payload[start:start+sb]
    markers = {m: m.encode("utf-8") in strings for m in (
        "Needs Help With Brigands", "Your men should be on their way", "resolved the matter")}
    def field(k):
        return int(meta[k])
    result = {"filename": path.name, "sha256": digest,
        "bytes": len(data), "version": meta["ApplicationVersion"],
        "modules": meta["Modules"].split(";"),
        "module_versions": {m: meta["Module_"+m] for m in meta["Modules"].split(";")},
        "healthy": field("MainPartyHealthyMemberCount"),
        "wounded": field("MainPartyWoundedMemberCount"), "gold": field("MainHeroGold"),
        "food": float(meta["MainPartyFood"]), "payload_bytes": len(payload),
        "object_blocks": oc, "container_blocks": cc, "markers": markers}
    # Verify original unchanged; no campaign ID emitted to console or GitHub.
    with path.open("rb") as stream:
        after = hashlib.file_digest(stream, "sha256").hexdigest()
    if after != digest:
        raise ValueError("save changed during read")
    return result, meta["UniqueGameId"]
