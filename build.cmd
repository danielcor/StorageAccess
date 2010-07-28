@echo off
SET PATH=%PATH%;C:\WINDOWS\Microsoft.NET\Framework\V3.5;

if not exist output ( mkdir output )

echo Compiling
msbuild /nologo /verbosity:quiet src/StorageAccess.sln /p:Configuration=Release /t:Clean
msbuild /nologo /verbosity:quiet src/StorageAccess.sln /p:Configuration=Release

echo Copying
copy src\proj\StorageAccess.NHibernate\bin\Release\*.* output

echo Cleaning
msbuild /nologo /verbosity:quiet src/StorageAccess.sln /p:Configuration=Release /t:Clean

echo Done