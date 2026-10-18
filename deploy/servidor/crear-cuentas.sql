-- CRV4 Mayorista 7.0 - cuentas de MySQL sobre la base existente crv4_datos.
-- La base NO se crea: es la misma que usa Sistema03. Este script solo crea dos cuentas nuevas
-- y no toca las cuentas ni los datos que ya existen.
-- Ejecutar UNA vez en la PC servidor con un usuario administrador de MySQL (por ejemplo root):
--   mysql -u root -p < crear-cuentas.sql
-- Antes de ejecutarlo, reemplazá las dos contraseñas de ejemplo. No guardes este archivo con las contraseñas reales.

-- Cuenta de la API: solo lee y escribe datos. No puede cambiar la estructura.
CREATE USER IF NOT EXISTS 'crv4_api'@'localhost' IDENTIFIED BY 'CAMBIAR_CLAVE_API';
GRANT SELECT, INSERT, UPDATE, DELETE ON crv4_datos.* TO 'crv4_api'@'localhost';

-- Cuenta de migraciones: cambia la estructura. Solo la usa Aplicar-Migraciones.ps1.
CREATE USER IF NOT EXISTS 'crv4_migraciones'@'localhost' IDENTIFIED BY 'CAMBIAR_CLAVE_MIGRACIONES';
GRANT ALL PRIVILEGES ON crv4_datos.* TO 'crv4_migraciones'@'localhost';
-- Necesario para que mysqldump haga el respaldo con --single-transaction y --routines.
GRANT PROCESS ON *.* TO 'crv4_migraciones'@'localhost';

FLUSH PRIVILEGES;
