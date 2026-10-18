namespace Crv4.Api.Modulos.Seguridad;

/// <summary>
/// Catálogo único de permisos. Agregar uno nuevo acá y crear una migración:
/// la migración lo inserta en la tabla <c>permisos</c> y se lo asigna al rol Administrador.
/// </summary>
public static class Permisos
{
    public const string InicioVer = "inicio.ver";

    public const string ProductosVer = "productos.ver";
    public const string ProductosEditar = "productos.editar";

    public const string ClientesVer = "clientes.ver";
    public const string ClientesEditar = "clientes.editar";

    public const string VentasVer = "ventas.ver";
    public const string VentasCrear = "ventas.crear";
    public const string VentasEditar = "ventas.editar";
    public const string VentasAnular = "ventas.anular";

    public const string EstadisticasVer = "estadisticas.ver";
    public const string ControlVentasVer = "control_ventas.ver";

    public const string RubrosVer = "rubros.ver";
    public const string RubrosEditar = "rubros.editar";

    public const string SeguridadUsuarios = "seguridad.usuarios";
    public const string SeguridadRoles = "seguridad.roles";

    public static readonly IReadOnlyList<Permiso> Todos =
    [
        Nuevo(InicioVer, "Inicio", "Ver el panel de inicio"),
        Nuevo(ProductosVer, "Productos", "Ver productos"),
        Nuevo(ProductosEditar, "Productos", "Crear y modificar productos"),
        Nuevo(ClientesVer, "Clientes", "Ver clientes"),
        Nuevo(ClientesEditar, "Clientes", "Crear y modificar clientes"),
        Nuevo(VentasVer, "Ventas", "Ver ventas"),
        Nuevo(VentasCrear, "Ventas", "Cargar ventas"),
        Nuevo(VentasEditar, "Ventas", "Modificar ventas"),
        Nuevo(VentasAnular, "Ventas", "Anular ventas"),
        Nuevo(EstadisticasVer, "Estadísticas", "Ver estadísticas"),
        Nuevo(ControlVentasVer, "Control Ventas", "Ver el control de ventas"),
        Nuevo(RubrosVer, "Rubros", "Ver rubros"),
        Nuevo(RubrosEditar, "Rubros", "Crear y modificar rubros"),
        Nuevo(SeguridadUsuarios, "Seguridad", "Administrar usuarios"),
        Nuevo(SeguridadRoles, "Seguridad", "Administrar roles y permisos"),
    ];

    private static Permiso Nuevo(string codigo, string modulo, string descripcion) =>
        new() { Codigo = codigo, Modulo = modulo, Descripcion = descripcion };
}
