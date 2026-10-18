using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Crv4.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Crv4v7Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // crv4_datos ya existe y la sigue usando Sistema03: esta migración solo AGREGA.
            // No toca columnas ni datos existentes, salvo sumar permisos que falten y dárselos al rol Administrador.
            migrationBuilder.Sql("""
                ALTER TABLE usuarios_sistema
                  ADD COLUMN IntentosFallidos int NOT NULL DEFAULT 0,
                  ADD COLUMN BloqueadoHasta datetime NULL;
                """);

            migrationBuilder.CreateTable(
                name: "numeradores",
                columns: table => new
                {
                    Clave = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    UltimoNumero = table.Column<long>(type: "bigint", nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_numeradores", x => x.Clave);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sesiones",
                columns: table => new
                {
                    IdSesion = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiraEn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevocadaEn = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReemplazadaPorId = table.Column<long>(type: "bigint", nullable: true),
                    Ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true),
                    Dispositivo = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesiones", x => x.IdSesion);
                    table.ForeignKey(
                        name: "FK_sesiones_usuarios_sistema_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "usuarios_sistema",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "numeradores",
                columns: new[] { "Clave", "Descripcion", "UltimoNumero" },
                values: new object[,]
                {
                    { "clientes", "Número de cliente (clientes.IdCliente)", 0L },
                    { "ventas", "Número de venta (facturas.IdFactura)", 0L }
                });

            // Permisos del catálogo (Modulos/Seguridad/Permisos.cs). Los que ya existen quedan como están.
            migrationBuilder.Sql("""
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
                """);

            migrationBuilder.Sql("""
                INSERT IGNORE INTO roles (NombreRol, Descripcion)
                VALUES ('Administrador', 'Acceso total al sistema');
                """);

            migrationBuilder.Sql("""
                INSERT IGNORE INTO roles_permisos (IdRol, IdPermiso)
                SELECT r.IdRol, p.IdPermiso FROM roles r CROSS JOIN permisos p
                WHERE r.NombreRol = 'Administrador';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_IdUsuario_RevocadaEn",
                table: "sesiones",
                columns: new[] { "IdUsuario", "RevocadaEn" });

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_TokenHash",
                table: "sesiones",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "numeradores");

            migrationBuilder.DropTable(
                name: "sesiones");

            // Los permisos y el rol Administrador se dejan: Sistema03 puede estar usándolos.
            migrationBuilder.Sql("""
                ALTER TABLE usuarios_sistema
                  DROP COLUMN IntentosFallidos,
                  DROP COLUMN BloqueadoHasta;
                """);
        }
    }
}
