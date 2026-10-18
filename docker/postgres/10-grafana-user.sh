#!/bin/bash
# Grafana gets user read-only; table is made by Api migration,
# ALTER DEFAULT PRIVILEGES (permissions for tables will be made later)
set -e
psql -v ON_ERROR_STOP=1 -U "${POSTGRES_USER:-postgres}" -d "$POSTGRES_DB" <<-SQL
    CREATE ROLE grafana LOGIN PASSWORD '${GRAFANA_DB_PASSWORD}';
    GRANT CONNECT ON DATABASE ${POSTGRES_DB} TO grafana;
    GRANT USAGE ON SCHEMA public TO grafana;
    ALTER DEFAULT PRIVILEGES FOR ROLE ${POSTGRES_USER:-postgres} IN SCHEMA public GRANT SELECT ON TABLES TO grafana;
SQL
