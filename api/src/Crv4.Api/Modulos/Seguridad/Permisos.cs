namespace Crv4.Api.Modulos.Seguridad;

public record DefinicionPermiso(string Codigo, string Modulo, string Descripcion);

/// <summary>
/// Catálogo de permisos. Los códigos son los mismos que ya usa Sistema03 en la tabla permisos.
/// Para agregar uno: sumarlo acá y en web/src/auth/permisos.ts, y crear una migración que lo inserte
/// (ver la migración inicial: inserta los que falten y se los da al rol Administrador).
/// </summary>
public static class Permisos
{
    public const string ClientesVer = "clientes.ver";
    public const string ClientesCrear = "clientes.crear";
    public const string ClientesEditar = "clientes.editar";
    public const string ClientesEliminar = "clientes.eliminar";

    public const string ProductosVer = "productos.ver";
    public const string ProductosCrear = "productos.crear";
    public const string ProductosEditar = "productos.editar";
    public const string ProductosEliminar = "productos.eliminar";

    public const string RubrosVer = "rubros.ver";
    public const string RubrosCrear = "rubros.crear";
    public const string RubrosEditar = "rubros.editar";
    public const string RubrosEliminar = "rubros.eliminar";

    public const string VentasVer = "ventas.ver";
    public const string VentasCrear = "ventas.crear";
    public const string VentasEditar = "ventas.editar";
    public const string VentasAnular = "ventas.anular";

    public const string ComprasVer = "compras.ver";
    public const string ComprasCrear = "compras.crear";
    public const string ComprasEditar = "compras.editar";

    public const string StockVer = "stock.ver";
    public const string StockAjustar = "stock.ajustar";

    public const string PedidosVer = "pedidos.ver";
    public const string PedidosPreparar = "pedidos.preparar";
    public const string PedidosConvertirVenta = "pedidos.convertir_venta";

    /// <summary>Estadísticas y Control Ventas.</summary>
    public const string ReportesVer = "reportes.ver";

    /// <summary>Pantalla de Seguridad: usuarios, roles y permisos.</summary>
    public const string UsuariosAdministrar = "usuarios.administrar";

    public const string ConfiguracionAdministrar = "configuracion.administrar";

    public static readonly IReadOnlyList<DefinicionPermiso> Todos =
    [
        new(ClientesVer, "Clientes", "Ver clientes"),
        new(ClientesCrear, "Clientes", "Crear clientes"),
        new(ClientesEditar, "Clientes", "Modificar clientes"),
        new(ClientesEliminar, "Clientes", "Eliminar clientes"),
        new(ProductosVer, "Productos", "Ver productos"),
        new(ProductosCrear, "Productos", "Crear productos"),
        new(ProductosEditar, "Productos", "Modificar productos"),
        new(ProductosEliminar, "Productos", "Eliminar productos"),
        new(RubrosVer, "Rubros", "Ver rubros"),
        new(RubrosCrear, "Rubros", "Crear rubros"),
        new(RubrosEditar, "Rubros", "Modificar rubros"),
        new(RubrosEliminar, "Rubros", "Eliminar rubros"),
        new(VentasVer, "Ventas", "Ver ventas"),
        new(VentasCrear, "Ventas", "Cargar ventas"),
        new(VentasEditar, "Ventas", "Modificar ventas"),
        new(VentasAnular, "Ventas", "Anular ventas"),
        new(ComprasVer, "Compras", "Ver compras"),
        new(ComprasCrear, "Compras", "Cargar compras"),
        new(ComprasEditar, "Compras", "Modificar compras"),
        new(StockVer, "Stock", "Ver stock"),
        new(StockAjustar, "Stock", "Ajustar stock"),
        new(PedidosVer, "Pedidos", "Ver pedidos"),
        new(PedidosPreparar, "Pedidos", "Preparar pedidos"),
        new(PedidosConvertirVenta, "Pedidos", "Convertir pedidos en venta"),
        new(ReportesVer, "Reportes", "Ver estadísticas y control de ventas"),
        new(UsuariosAdministrar, "Seguridad", "Administrar usuarios, roles y permisos"),
        new(ConfiguracionAdministrar, "Configuración", "Administrar la configuración"),
    ];

    private static readonly Dictionary<string, string> Modulos = Todos.ToDictionary(p => p.Codigo, p => p.Modulo);

    /// <summary>Módulo para agrupar en pantalla. Los permisos que no están en el catálogo van a "Otros".</summary>
    public static string ModuloDe(string codigo) => Modulos.GetValueOrDefault(codigo, "Otros");
}
