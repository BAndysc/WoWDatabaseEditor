#!/bin/bash
# Downloads the latest world database of every supported core and imports it into a
# persistent local database (ci_<Core>), dropping the previous one first. Unlike
# database_test.sh it neither runs DatabaseTester nor drops the database afterwards -
# use it to keep local ci_* databases fresh for DatabaseTester runs.
#
# Usage: ./refresh_databases.sh [host] [port] <user> <password> [mysql path] [core filter]
#   core filter (optional): only refresh cores whose tag contains this substring,
#   e.g. ./refresh_databases.sh localhost 3306 user pass mysql TrinityWrath
set -e

HOST="${1:-localhost}"
PORT="${2:-3306}"
USER="${3:?usage: ./refresh_databases.sh [host] [port] <user> <password> [mysql path] [core filter]}"
PASSWORD="${4:?missing password}"
MYSQL_PATH="${5:-mysql}"
FILTER="${6:-}"

export MYSQL_PWD="$PASSWORD"
MYSQL=("$MYSQL_PATH" -h "$HOST" -P "$PORT" -u "$USER")

if command -v 7z >/dev/null 2>&1; then
    EXTRACT() { 7z e "$1"; }
elif command -v 7zz >/dev/null 2>&1; then
    EXTRACT() { 7zz e "$1"; }
else
    EXTRACT() { unzip -o -j "$1"; }
fi

URLs=( 'https://github.com/TrinityCore/TrinityCore' 'https://github.com/TrinityCore/TrinityCore' 'https://github.com/The-Cataclysm-Preservation-Project/TrinityCore' 'https://github.com/azerothcore/azerothcore-wotlk' 'https://github.com/cmangos/wotlk-db' 'https://github.com/cmangos/tbc-db' 'https://github.com/cmangos/classic-db' )
BRANCHEs=( 'master' '3.3.5' 'master' 'master' 'master' 'master' 'master' )
UPDATEs=( 'master' '3.3.5' '4.3.4' '' '' '' '' )
COREs=( 'TrinityMaster' 'TrinityWrath' 'TrinityCata' 'Azeroth' 'CMaNGOS-WoTLK' 'CMaNGOS-TBC' 'CMaNGOS-Classic' )
MANGOSFILEs=( '' '' '' '' 'wotlk' 'tbc' 'classic' )
TYPEs=( 'TC' 'TC' 'TC' 'TC' 'CMANGOS' 'CMANGOS' 'CMANGOS' )

WORKDIR="$(mktemp -d)"
trap 'rm -rf "$WORKDIR"' EXIT
cd "$WORKDIR"

for index in ${!URLs[*]}; do
    CORE="${COREs[$index]}"

    if [ -n "$FILTER" ] && [[ "$CORE" != *"$FILTER"* ]]; then
        continue
    fi

    DB="ci_${CORE//-/_}"
    echo "=== $CORE -> $DB"

    "${MYSQL[@]}" -e "DROP DATABASE IF EXISTS \`$DB\`"
    "${MYSQL[@]}" -e "CREATE DATABASE \`$DB\`"

    rm -rf Repo
    git clone \
        -b "${BRANCHEs[$index]}" \
        --depth 1 \
        --filter=blob:none \
        --sparse \
        "${URLs[$index]}" Repo
    cd Repo

    if [ "${TYPEs[$index]}" = "CMANGOS" ]; then
        curl -L "${URLs[$index]}/releases/download/latest/${MANGOSFILEs[$index]}-world-db.zip" -o "${MANGOSFILEs[$index]}-world-db.zip"
        EXTRACT "${MANGOSFILEs[$index]}-world-db.zip"
        "${MYSQL[@]}" "$DB" < "${MANGOSFILEs[$index]}mangos.sql"
    else
        git sparse-checkout set sql/base/dev
        git sparse-checkout add "sql/updates/world/${UPDATEs[$index]}"
        git sparse-checkout add data/sql/base/db_world/
        git sparse-checkout add data/sql/updates/db_world/

        if [ -f "sql/base/dev/world_database.sql" ];
        then
            echo "--- importing base world_database.sql"
            "${MYSQL[@]}" "$DB" < sql/base/dev/world_database.sql

            for i in sql/updates/world/${UPDATEs[$index]}/*.sql
            do
                echo "--- $i"
                "${MYSQL[@]}" "$DB" < "${i}"
            done
        else
            # AzerothCore: skip updates already contained in the base dump (updates.sql lists
            # them as ('2026_01_01_00.sql','<hash>','RELEASED',...) tuples, one per line)
            grep -oE "[0-9]{4}_[0-9]{2}_[0-9]{2}_[0-9]+\.sql" data/sql/base/db_world/updates.sql | sort -u | while read -r applied; do
                rm -f "data/sql/updates/db_world/$applied"
            done

            rm -rf data/sql/updates/db_world/2024_03_04_00.sql || true

            for i in data/sql/base/db_world/*.sql
            do
                echo "--- $i"
                "${MYSQL[@]}" "$DB" < "${i}"
            done

            for i in data/sql/updates/db_world/*.sql
            do
                echo "--- $i"
                "${MYSQL[@]}" "$DB" < "${i}"
            done
        fi
    fi

    cd ..
    rm -rf Repo
    echo "=== $CORE done"
done

echo "ALL DONE"
