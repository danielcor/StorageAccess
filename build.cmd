@echo off
SET PATH=%PATH%;C:\WINDOWS\Microsoft.NET\Framework\V3.5;

if not exist output ( mkdir output )

echo Compiling
msbuild /nologo /verbosity:quiet src/StorageAccess.sln /p:Configuration=Release /t:Clean
msbuild /nologo /verbosity:quiet src/StorageAccess.sln /p:Configuration=Release

echo Copying
copy src\proj\StorageAccess\bin\Release\*.* output\

echo Merging

if not exist output\NHibernate ( mkdir output\NHibernate )
SET FILES_TO_MERGE=
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/StorageAccess/bin/Release/StorageAccess.dll"
SET FILES_TO_MERGE=%FILES_TO_MERGE% "src/proj/StorageAccess.NHibernate/bin/Release/StorageAccess.NHibernate.dll"
bin\ilmerge-bin\ILMerge.exe /keyfile:src/StorageAccess.snk /v2 /xmldocs /out:output/NHibernate/StorageAccess.NHibernate.dll %FILES_TO_MERGE%
copy lib\nhibernate-bin\* output\NHibernate

echo Cleaning
msbuild /nologo /verbosity:quiet src/StorageAccess.sln /p:Configuration=Release /t:Clean

echo Done