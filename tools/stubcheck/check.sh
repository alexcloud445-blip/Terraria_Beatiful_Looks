#!/usr/bin/env bash
# Проверка, что код мода компилируется, без установленного tModLoader.
# Код собирается против ЗАГЛУШЕК API Terraria/tModLoader (*.cs.txt рядом) и настоящих
# библиотек XNA из репозитория tModLoader. Заглушки написаны по исходникам tML вручную:
# они ловят опечатки и нестыковки в коде мода, но не гарантируют, что API совпадает с игрой.
# Окончательная проверка — Build + Reload в самой игре.
#
# Нужно: .NET SDK 8 (apt install dotnet-sdk-8.0) и клон tModLoader:
#   GIT_LFS_SKIP_SMUDGE=1 git clone --depth 1 -b stable https://github.com/tModLoader/tModLoader /home/user/tmodloader/tmodloader
# Заглушки лежат как .cs.txt, потому что tModLoader компилирует все .cs в папке мода.
set -e
HERE=$(cd "$(dirname "$0")" && pwd)
MOD=$(cd "$HERE/../.." && pwd)
WORK=${WORK:-/tmp/tbl-stubcheck}
TML=${TML:-/home/user/tmodloader/tmodloader}

rm -rf "$WORK/Stubs"
mkdir -p "$WORK/Stubs"
for f in "$HERE"/*.cs.txt; do
	cp "$f" "$WORK/Stubs/$(basename "${f%.txt}")"
done
sed "s#@MOD@#$MOD#g" "$HERE/Check.csproj.txt" > "$WORK/Check.csproj"
for dll in Microsoft.Xna.Framework Microsoft.Xna.Framework.Graphics Microsoft.Xna.Framework.Game; do
	cp "$TML/setup/xna_redist/$dll.dll" "$WORK/"
done
cd "$WORK"
dotnet build -nologo -v q 2>&1 | grep -E "error|Warn|Error" | sed "s#$MOD/##" | sort -u
