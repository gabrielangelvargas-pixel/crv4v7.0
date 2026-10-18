using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Crv4.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "numeradores",
                columns: table => new
                {
                    clave = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    ultimo_numero = table.Column<long>(type: "bigint", nullable: false),
                    descripcion = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_numeradores", x => x.clave);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "permisos",
                columns: table => new
                {
                    codigo = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    modulo = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permisos", x => x.codigo);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true),
                    es_sistema = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rubros",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    orden = table.Column<int>(type: "int", nullable: false),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    creado_en = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rubros", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "roles_permisos",
                columns: table => new
                {
                    rol_id = table.Column<int>(type: "int", nullable: false),
                    permiso_codigo = table.Column<string>(type: "varchar(60)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles_permisos", x => new { x.rol_id, x.permiso_codigo });
                    table.ForeignKey(
                        name: "fk_roles_permisos_permisos_permiso_codigo",
                        column: x => x.permiso_codigo,
                        principalTable: "permisos",
                        principalColumn: "codigo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_roles_permisos_roles_rol_id",
                        column: x => x.rol_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre_usuario = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    password_hash = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    rol_id = table.Column<int>(type: "int", nullable: false),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    debe_cambiar_password = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    intentos_fallidos = table.Column<int>(type: "int", nullable: false),
                    bloqueado_hasta = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    ultimo_ingreso = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_usuarios_roles_rol_id",
                        column: x => x.rol_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sesiones",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    token_hash = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false),
                    creada_en = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    expira_en = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    revocada_en = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    reemplazada_por_id = table.Column<long>(type: "bigint", nullable: true),
                    ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true),
                    dispositivo = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sesiones", x => x.id);
                    table.ForeignKey(
                        name: "fk_sesiones_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "numeradores",
                columns: new[] { "clave", "descripcion", "ultimo_numero" },
                values: new object[,]
                {
                    { "clientes", "Código de cliente", 0L },
                    { "ventas", "Número de venta", 0L }
                });

            migrationBuilder.InsertData(
                table: "permisos",
                columns: new[] { "codigo", "descripcion", "modulo" },
                values: new object[,]
                {
                    { "clientes.editar", "Crear y modificar clientes", "Clientes" },
                    { "clientes.ver", "Ver clientes", "Clientes" },
                    { "control_ventas.ver", "Ver el control de ventas", "Control Ventas" },
                    { "estadisticas.ver", "Ver estadísticas", "Estadísticas" },
                    { "inicio.ver", "Ver el panel de inicio", "Inicio" },
                    { "productos.editar", "Crear y modificar productos", "Productos" },
                    { "productos.ver", "Ver productos", "Productos" },
                    { "rubros.editar", "Crear y modificar rubros", "Rubros" },
                    { "rubros.ver", "Ver rubros", "Rubros" },
                    { "seguridad.roles", "Administrar roles y permisos", "Seguridad" },
                    { "seguridad.usuarios", "Administrar usuarios", "Seguridad" },
                    { "ventas.anular", "Anular ventas", "Ventas" },
                    { "ventas.crear", "Cargar ventas", "Ventas" },
                    { "ventas.editar", "Modificar ventas", "Ventas" },
                    { "ventas.ver", "Ver ventas", "Ventas" }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "descripcion", "es_sistema", "nombre" },
                values: new object[] { 1, "Acceso completo", true, "Administrador" });

            migrationBuilder.InsertData(
                table: "roles_permisos",
                columns: new[] { "permiso_codigo", "rol_id" },
                values: new object[,]
                {
                    { "clientes.editar", 1 },
                    { "clientes.ver", 1 },
                    { "control_ventas.ver", 1 },
                    { "estadisticas.ver", 1 },
                    { "inicio.ver", 1 },
                    { "productos.editar", 1 },
                    { "productos.ver", 1 },
                    { "rubros.editar", 1 },
                    { "rubros.ver", 1 },
                    { "seguridad.roles", 1 },
                    { "seguridad.usuarios", 1 },
                    { "ventas.anular", 1 },
                    { "ventas.crear", 1 },
                    { "ventas.editar", 1 },
                    { "ventas.ver", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_roles_nombre",
                table: "roles",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_permisos_permiso_codigo",
                table: "roles_permisos",
                column: "permiso_codigo");

            migrationBuilder.CreateIndex(
                name: "ix_rubros_nombre",
                table: "rubros",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sesiones_token_hash",
                table: "sesiones",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sesiones_usuario_id_revocada_en",
                table: "sesiones",
                columns: new[] { "usuario_id", "revocada_en" });

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_nombre_usuario",
                table: "usuarios",
                column: "nombre_usuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_rol_id",
                table: "usuarios",
                column: "rol_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "numeradores");

            migrationBuilder.DropTable(
                name: "roles_permisos");

            migrationBuilder.DropTable(
                name: "rubros");

            migrationBuilder.DropTable(
                name: "sesiones");

            migrationBuilder.DropTable(
                name: "permisos");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
