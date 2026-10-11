"""Pack Builds/Server into an OCI layer tarball (no Docker needed).

Layout inside the image:
  /app/BaitSwitchServer.x86_64 (+ _Data, UnityPlayer.so ...)   executable bits set explicitly
  /etc/ssl/certs/ca-certificates.crt                             CA bundle so the server can HTTPS the broker
Usage: python -I make_layer.py <server_build_dir> <ca_bundle> <out_tar> [webgl_build_dir]
  /app/web/...                                                   the WebGL build, served by the server (browser play)
"""
import os
import sys
import tarfile

src, ca, out = sys.argv[1], sys.argv[2], sys.argv[3]
web = sys.argv[4] if len(sys.argv) > 4 and os.path.isfile(os.path.join(sys.argv[4], "index.html")) else None
EXEC = (".x86_64", ".so")


def add_dir(tar, name):
    ti = tarfile.TarInfo(name)
    ti.type = tarfile.DIRTYPE
    ti.mode = 0o755
    tar.addfile(ti)


def add_file(tar, path, name, mode):
    ti = tar.gettarinfo(path, name)
    ti.mode = mode
    ti.uid = ti.gid = 0
    ti.uname = ti.gname = "root"
    with open(path, "rb") as f:
        tar.addfile(ti, f)


with tarfile.open(out, "w", format=tarfile.PAX_FORMAT) as tar:
    add_dir(tar, "app")
    count = 0
    for root, dirs, files in os.walk(src):
        dirs[:] = [d for d in dirs if "DoNotShip" not in d]
        rel = os.path.relpath(root, src).replace("\\", "/")
        base = "app" if rel == "." else "app/" + rel
        if rel != ".":
            add_dir(tar, base)
        for fn in files:
            exe = fn.endswith(EXEC) or ".so." in fn
            add_file(tar, os.path.join(root, fn), base + "/" + fn, 0o755 if exe else 0o644)
            count += 1
    if web:
        add_dir(tar, "app/web")
        for root, dirs, files in os.walk(web):
            rel = os.path.relpath(root, web).replace("\\", "/")
            base = "app/web" if rel == "." else "app/web/" + rel
            if rel != ".":
                add_dir(tar, base)
            for fn in files:
                add_file(tar, os.path.join(root, fn), base + "/" + fn, 0o644)
                count += 1
    for d in ("etc", "etc/ssl", "etc/ssl/certs"):
        add_dir(tar, d)
    add_file(tar, ca, "etc/ssl/certs/ca-certificates.crt", 0o644)
print(f"layer: {count} files{' (with WebGL)' if web else ''} -> {out} ({os.path.getsize(out) // (1024 * 1024)} MB)")
