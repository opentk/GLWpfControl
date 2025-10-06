@echo off

SET PATH=%LOCALAPPDATA%\Microsoft\dotnet;%PATH%
.paket\paket.bootstrapper.exe

SET BuildTarget=
if "%BuildRunner%" == "MyGet" (
  SET BuildTarget=NuGet

  :: Replace the existing release notes file with one for this build only
  echo ### %PackageVersion% > RELEASE_NOTES.md
  echo 	* git build >> RELEASE_NOTES.md
)

dotnet tool restore
dotnet fake run build.fsx %*
