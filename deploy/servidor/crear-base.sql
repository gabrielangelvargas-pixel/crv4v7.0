-- CRV4 Mayorista 7.0 - creación de la base y de las cuentas de MySQL.
-- Ejecutar UNA vez en la PC servidor con un usuario administrador de MySQL (por ejemplo root):
--   mysql -u root -p < crear-base.sql
-- Antes de ejecutarlo, reemplazá las dos contraseñas de ejemplo. No guardes este archivo con las contraseñas reales.

CREATE DATABASE IF NOT EXISTS crv4_v7 CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

-- Cuenta de la API: solo lee y escribe datos. No puede cambiar la estructura.
CREATE USER IF NOT EXISTS 'crv4_api'@'localhost' IDENTIFIED BY 'CAMBIAR_CLAVE_API';
GRANT SELECT, INSERT, UPDATE, DELETE ON crv4_v7.* TO 'crv4_api'@'localhost';

-- Cuenta de migraciones: cambia la estructura. Solo la usa Aplicar-Migraciones.ps1.
CREATE USER IF NOT EXISTS 'crv4_migraciones'@'localhost' IDENTIFIED BY 'CAMBIAR_CLAVE_MIGRACIONES';
GRANT ALL PRIVILEGES ON crv4_v7.* TO 'crv4_migraciones'@'localhost';
-- Necesario para que mysqldump haga el respaldo con --single-transaction y --routines.
GRANT PROCESS ON *.* TO 'crv4_migraciones'@'localhost';

FLUSH PRIVILEGES;
