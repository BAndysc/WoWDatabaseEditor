set -e
USER="$3"
PASSWORD="$4"
PORT=$2
HOST="$1"
MYSQL_PATH="$5"

URLs=( 'https://github.com/TrinityCore/TrinityCore' 'https://github.com/TrinityCore/TrinityCore' 'https://github.com/The-Cataclysm-Preservation-Project/TrinityCore' 'https://github.com/azerothcore/azerothcore-wotlk' 'https://github.com/cmangos/wotlk-db' )
BRANCHEs=( 'master' '3.3.5' 'master' 'master' 'master' )
UPDATEs=( 'master' '3.3.5' '4.3.4' '' '' )
COREs=( 'TrinityMaster' 'TrinityWrath' 'TrinityCata' 'Azeroth' 'CMaNGOS-WoTLK' )
TYPEs=( 'TC' 'TC' 'TC' 'TC' 'MANGOS' )
SKIP=( false false false true false )

for index in ${!URLs[*]}; do

    if ${SKIP[$index]}; then
        echo "Skipping ${COREs[$index]}"
        continue
    fi

    "${MYSQL_PATH}" -u ${USER} -p${PASSWORD} -e 'DROP DATABASE IF EXISTS temp_CI'
    "${MYSQL_PATH}" -u ${USER} -p${PASSWORD} -e 'CREATE DATABASE temp_CI'
    
    git clone \
    -b ${BRANCHEs[$index]} \
    --depth 1  \
    --filter=blob:none  \
    --sparse \
    ${URLs[$index]} Repo \
    ;
    cd Repo

    if [ "${TYPEs[$index]}" = "MANGOS" ]; then
        rm -rf wotlk-world-db.zip
        rm -rf wotlkmangos.sql
        curl -L ${URLs[$index]}/releases/download/latest/wotlk-world-db.zip -o wotlk-world-db.zip
        7z e wotlk-world-db.zip
        rm -rf wotlk-world-db.zip
        "${MYSQL_PATH}" -u ${USER} -p${PASSWORD} temp_CI < wotlkmangos.sql
        rm -rf wotlkmangos.sql
    else
        git sparse-checkout set sql/base/dev
        git sparse-checkout add sql/updates/world/${UPDATEs[$index]}
        git sparse-checkout add data/sql/base/db_world/
        git sparse-checkout add data/sql/updates/db_world/

        if [ -f "sql/base/dev/world_database.sql" ]; 
        then
            "${MYSQL_PATH}" -u ${USER} -p${PASSWORD} temp_CI < sql/base/dev/world_database.sql

            for i in sql/updates/world/${UPDATEs[$index]}/*.sql
            do
                "${MYSQL_PATH}" -u ${USER} -p${PASSWORD} temp_CI < "${i}"
            done
        else
            for i in data/sql/base/db_world/*.sql
            do
                "${MYSQL_PATH}" -u ${USER} -p${PASSWORD} temp_CI < "${i}"
            done

            for i in data/sql/updates/db_world/*.sql
            do
                "${MYSQL_PATH}" -u ${USER} -p${PASSWORD} temp_CI < "${i}"
            done
        fi
    fi

    cd ../

    dotnet run --project ../DatabaseTester/DatabaseTester.csproj $HOST $PORT $USER $PASSWORD temp_CI ${COREs[$index]}

    "${MYSQL_PATH}" -u ${USER} -p${PASSWORD} -e 'DROP DATABASE temp_CI'

    rm -rf Repo

done