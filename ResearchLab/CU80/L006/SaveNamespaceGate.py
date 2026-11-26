"""Read-only gate for CU80 lab save namespace. Never moves or deletes a real save."""
from pathlib import Path
import hashlib, os, stat

def digest(p):
    h=hashlib.sha256()
    with p.open("rb") as f:
        for b in iter(lambda:f.read(1048576),b""):h.update(b)
    return h.hexdigest().lower()

def physical(p):
    p=Path(p).absolute()
    # Refuse redirected paths, including Windows junctions, at every ancestor.
    for part in (p,*p.parents):
        if part.is_symlink() or (hasattr(os.path,"isjunction") and os.path.isjunction(part)):
            raise ValueError("redirected path")
    return p.resolve(strict=True)

def validate(vault, active, originals, approved):
    """Validate physical namespace and files, not engine/runtime or cloud state."""
    a,v=physical(active),physical(vault)
    if not a.is_dir() or not v.is_dir() or a==v or a in v.parents or v in a.parents:
        raise ValueError("namespace collision")
    if not originals or not approved or set(originals)&set(approved):
        raise ValueError("manifest scope")
    for manifest in (originals,approved):
        for n,h in manifest.items():
            if Path(n).name!=n or not n.lower().endswith(".sav") or len(h)!=64 or any(c not in "0123456789abcdef" for c in h.lower()):
                raise ValueError("invalid manifest")
    va={p.name for p in v.iterdir()}
    aa={p.name for p in a.iterdir()}
    if va!=set(originals):raise ValueError("vault inventory differs")
    if aa!=set(approved):raise ValueError("active inventory differs")
    identity=set()
    for root,manifest in ((v,originals),(a,approved)):
        for n,h in manifest.items():
            p=physical(root/n)
            if p.parent!=root or not p.is_file():raise ValueError("not a direct file")
            st=p.stat()
            if st.st_nlink!=1 or (st.st_dev,st.st_ino) in identity:
                raise ValueError("linked or aliased save")
            identity.add((st.st_dev,st.st_ino))
            if digest(p)!=h.lower():raise ValueError("save fingerprint differs")
    return True

def live_gate(vault,active,originals,approved,closed,autosave_off,cloud_verified_off,native_path_verified):
    if not closed:raise ValueError("game open")
    if not autosave_off:raise ValueError("autosave enabled")
    if not cloud_verified_off:raise ValueError("cloud safety unknown")
    if not native_path_verified:raise ValueError("native active path not established")
    return validate(vault,active,originals,approved)
