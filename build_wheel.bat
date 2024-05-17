@echo off
set PYTHONHOME=c:\python310

if exist dist rd /s /q dist
python -m pip wheel -w dist --no-deps .
if exist build rd /s /q build
if exist gcbminputloader.egg-info rd /s /q gcbminputloader.egg-info
