@echo off

dotnet tool restore
dotnet restore
dotnet msbuild -graph -isolate -binaryLogger:artifacts/log/build.binlog
dotnet pack --no-build --no-restore --nologo --output artifacts/packages