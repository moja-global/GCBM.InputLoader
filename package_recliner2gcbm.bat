@echo off

if exist dist rd /s /q dist

xcopy /s /y GUI\bin\x64\Release dist\Recliner2GCBM-x64\
xcopy /s /y CLI\bin\x64\Release dist\Recliner2GCBM-x64\

xcopy /s /y GUI\bin\Release dist\Recliner2GCBM-x86\
xcopy /s /y CLI\bin\Release dist\Recliner2GCBM-x86\
