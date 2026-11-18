#!/usr/bin/env python3
"""Isolated loader probe packaging audit, not a real game launch."""
import pathlib
import zipfile
import xml.etree.ElementTree as ET
import hashlib
import sys
source = pathlib.Path(sys.argv[1])
assert source.exists()
prefix="Modules/CULoaderProbe/"
expected={prefix+"SubModule.xml",
    prefix+"bin/Win64_Shipping_Client/CULoaderProbe.dll"}
with zipfile.ZipFile(source,"r") as archive:
    names={x for x in archive.namelist() if not x.endswith("/")}
    assert names==expected,(names,expected)
    root=ET.fromstring(archive.read(prefix+"SubModule.xml"))
    assert root.find("Id").attrib["value"]=="CULoaderProbe"
    assert root.find(".//DLLName").attrib["value"]=="CULoaderProbe.dll"
    content=archive.read(prefix+"bin/Win64_Shipping_Client/CULoaderProbe.dll")
    assert len(content)>1024
    assert archive.testzip() is None
    print("PASS: standalone probe, only native dependency, one DLL, valid manifest, sha256",hashlib.sha256(content).hexdigest())
