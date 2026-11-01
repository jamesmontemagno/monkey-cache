#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

work=$(mktemp -d "${TMPDIR:-/tmp}/monkeycache-packages.XXXXXXXX")
version="0.0.0-local.$(date -u +%Y%m%d%H%M%S)"
echo "Local-only packages and consumer evidence: $work ($version)"
for project in MonkeyCache MonkeyCache.FileStore MonkeyCache.SQLite MonkeyCache.LiteDB; do
    dotnet pack "src/$project/$project.csproj" -c Release \
        -p:MonkeyCachePortable=true -p:AllowPortablePackage=true \
        -p:Version="$version" -p:PackageVersion="$version" -o "$work/packages"
done

mkdir "$work/consumer"
cp tests/MonkeyCache.Regression/MonkeyCache.Regression.csproj tests/MonkeyCache.Regression/Program.cs "$work/consumer/"
dotnet restore "$work/consumer/MonkeyCache.Regression.csproj" \
    -p:SmokePackageVersion="$version" --source "$work/packages" --source https://api.nuget.org/v3/index.json
for framework in net8.0 net10.0; do
    dotnet run --project "$work/consumer/MonkeyCache.Regression.csproj" \
        -f "$framework" -c Release --no-restore -p:SmokePackageVersion="$version"
done
echo "Smoke passed; retain $work for inspection. These portable packages must not be published."
