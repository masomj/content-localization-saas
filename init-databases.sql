SELECT 'CREATE DATABASE content_localization'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'content_localization')\gexec

SELECT 'CREATE DATABASE keycloak'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'keycloak')\gexec
