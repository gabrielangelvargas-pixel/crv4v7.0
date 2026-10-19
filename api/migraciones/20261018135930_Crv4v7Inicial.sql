-- Primera migración de la 7.0 (la misma que aplicaba la versión .NET).
-- Agrega el bloqueo por intentos fallidos, los numeradores y las sesiones. No toca datos existentes,
-- salvo sumar los permisos que falten y dárselos todos al rol Administrador.

ALTER TABLE usuarios_sistema
  ADD COLUMN IntentosFallidos int NOT NULL DEFAULT 0,
  ADD COLUMN BloqueadoHasta datetime NULL;

CREATE TABLE numeradores (
  Clave varchar(40) NOT NULL,
  UltimoNumero bigint NOT NULL,
  Descripcion varchar(200) NULL,
  CONSTRAINT PK_numeradores PRIMARY KEY (Clave)
) CHARACTER SET utf8mb4;

CREATE TABLE sesiones (
  IdSesion bigint NOT NULL AUTO_INCREMENT,
  IdUsuario int NOT NULL,
  TokenHash char(64) NOT NULL,
  CreadaEn datetime(6) NOT NULL,
  ExpiraEn datetime(6) NOT NULL,
  RevocadaEn datetime(6) NULL,
  ReemplazadaPorId bigint NULL,
  Ip varchar(45) NULL,
  Dispositivo varchar(250) NULL,
  CONSTRAINT PK_sesiones PRIMARY KEY (IdSesion),
  CONSTRAINT FK_sesiones_usuarios_sistema_IdUsuario FOREIGN KEY (IdUsuario) REFERENCES usuarios_sistema (IdUsuario) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

CREATE INDEX IX_sesiones_IdUsuario_RevocadaEn ON sesiones (IdUsuario, RevocadaEn);

CREATE UNIQUE INDEX IX_sesiones_TokenHash ON sesiones (TokenHash);

INSERT INTO numeradores (Clave, Descripcion, UltimoNumero) VALUES
  ('clientes', 'Número de cliente (clientes.IdCliente)', 0),
  ('ventas', 'Número de venta (facturas.IdFactura)', 0);

INSERT IGNORE INTO permisos (Codigo, Descripcion) VALUES
  ('clientes.ver', 'Ver clientes'),
  ('clientes.crear', 'Crear clientes'),
  ('clientes.editar', 'Modificar clientes'),
  ('clientes.eliminar', 'Eliminar clientes'),
  ('productos.ver', 'Ver productos'),
  ('productos.crear', 'Crear productos'),
  ('productos.editar', 'Modificar productos'),
  ('productos.eliminar', 'Eliminar productos'),
  ('rubros.ver', 'Ver rubros'),
  ('rubros.crear', 'Crear rubros'),
  ('rubros.editar', 'Modificar rubros'),
  ('rubros.eliminar', 'Eliminar rubros'),
  ('ventas.ver', 'Ver ventas'),
  ('ventas.crear', 'Cargar ventas'),
  ('ventas.editar', 'Modificar ventas'),
  ('ventas.anular', 'Anular ventas'),
  ('compras.ver', 'Ver compras'),
  ('compras.crear', 'Cargar compras'),
  ('compras.editar', 'Modificar compras'),
  ('stock.ver', 'Ver stock'),
  ('stock.ajustar', 'Ajustar stock'),
  ('pedidos.ver', 'Ver pedidos'),
  ('pedidos.preparar', 'Preparar pedidos'),
  ('pedidos.convertir_venta', 'Convertir pedidos en venta'),
  ('reportes.ver', 'Ver estadísticas y control de ventas'),
  ('usuarios.administrar', 'Administrar usuarios, roles y permisos'),
  ('configuracion.administrar', 'Administrar la configuración');

INSERT IGNORE INTO roles (NombreRol, Descripcion) VALUES ('Administrador', 'Acceso total al sistema');

INSERT IGNORE INTO roles_permisos (IdRol, IdPermiso)
SELECT r.IdRol, p.IdPermiso FROM roles r CROSS JOIN permisos p WHERE r.NombreRol = 'Administrador';
