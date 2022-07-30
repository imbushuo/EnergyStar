# Packaging script, assuming buildout succeeded and artifacts are placed in expected location.
# Run this with cwd of ./EnergyStar

# Dependencies
Import-Module ".\Package\new-cabinetfile.ps1"

# Cleanup artifact folder
Remove-Item -Recurse -ErrorAction SilentlyContinue Package/buildout.arm64.release
Remove-Item -Recurse -ErrorAction SilentlyContinue Package/buildout.amd64.release
New-Item -ItemType Directory -Force -Path Package/buildout.arm64.release
New-Item -ItemType Directory -Force -Path Package/buildout.amd64.release

# Stamp architecture in INF file
((Get-Content -Path Package/EnergyStar.inx -Raw).Replace('$ARCH$','arm64')) | Set-Content -Path Package/buildout.arm64.release/EnergyStar.inf

((Get-Content -Path Package/EnergyStar.inx -Raw).Replace('$ARCH$','amd64')) | Set-Content -Path Package/buildout.amd64.release/EnergyStar.inf

# Copy artifacts to buildout folder
Copy-Item EnergyStar\bin\Release\net6.0\win-arm64\publish\*.* Package/buildout.arm64.release
Copy-Item EnergyStar\bin\Release\net6.0\win-x64\publish\*.* Package/buildout.amd64.release

# Generate Catalog
. "C:\Program Files (x86)\Windows Kits\10\bin\x86\inf2cat.exe" /driver:Package/buildout.arm64.release /os:10_CO_ARM64
. "C:\Program Files (x86)\Windows Kits\10\bin\x86\inf2cat.exe" /driver:Package/buildout.amd64.release /os:10_CO_X64

# Package pre-signed CAB
New-CabinetFile -Name buildout.amd64.release.cab -ContentDirectory Package/buildout.amd64.release -DestinationDir Release -DestinationPath Package
New-CabinetFile -Name buildout.arm64.release.cab -ContentDirectory Package/buildout.arm64.release -DestinationDir Release -DestinationPath Package
