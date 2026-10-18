-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)
--
-- Host: localhost    Database: crv4_datos
-- ------------------------------------------------------
-- Server version	8.0.44

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `bonosdescuento`
--

DROP TABLE IF EXISTS `bonosdescuento`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bonosdescuento` (
  `IdBono` int NOT NULL AUTO_INCREMENT,
  `CodigoBono` varchar(30) NOT NULL,
  `IdFacturaOrigen` int NOT NULL,
  `IdCliente` int DEFAULT NULL,
  `FechaEmision` datetime NOT NULL,
  `FechaVencimiento` datetime NOT NULL,
  `Porcentaje` decimal(8,2) NOT NULL,
  `TotalFactura` decimal(12,2) NOT NULL,
  `ImporteBono` decimal(12,2) NOT NULL,
  `Usado` tinyint(1) NOT NULL DEFAULT '0',
  `FechaUso` datetime DEFAULT NULL,
  `IdFacturaUso` int DEFAULT NULL,
  `Observaciones` text,
  PRIMARY KEY (`IdBono`),
  UNIQUE KEY `UK_BonosDescuento_Codigo` (`CodigoBono`),
  UNIQUE KEY `UK_BonosDescuento_FacturaOrigen` (`IdFacturaOrigen`),
  KEY `IX_BonosDescuento_Cliente` (`IdCliente`),
  KEY `IX_BonosDescuento_Vencimiento` (`FechaVencimiento`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `clientes`
--

DROP TABLE IF EXISTS `clientes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `clientes` (
  `IdCliente` int NOT NULL,
  `NombreCliente` varchar(150) NOT NULL,
  `Direccion` varchar(150) DEFAULT NULL,
  `IdZona` int NOT NULL,
  `IdFiscal` int NOT NULL,
  `NumeroFiscal` varchar(13) DEFAULT NULL,
  `NumCelular1` varchar(10) DEFAULT NULL,
  `NumCelular2` varchar(10) DEFAULT NULL,
  `NumFijo` varchar(15) DEFAULT NULL,
  `SaldoCuenta` decimal(10,2) DEFAULT '0.00',
  `FechaAlta` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdCliente`),
  KEY `FK_Clientes_fiscales_idx` (`IdFiscal`) /*!80000 INVISIBLE */,
  KEY `fk_clientes_zonas_idx` (`IdZona`),
  CONSTRAINT `fk_clientes_fiscales` FOREIGN KEY (`IdFiscal`) REFERENCES `fiscales` (`IdFiscal`) ON UPDATE CASCADE,
  CONSTRAINT `fk_clientes_zonas` FOREIGN KEY (`IdZona`) REFERENCES `zonas` (`IdZona`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `configuracionbonos`
--

DROP TABLE IF EXISTS `configuracionbonos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `configuracionbonos` (
  `IdConfig` int NOT NULL,
  `PorcentajeBono` decimal(8,2) NOT NULL DEFAULT '20.00',
  `DiasVencimiento` int NOT NULL DEFAULT '15',
  `ImpresoraTicket` varchar(255) DEFAULT NULL,
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`IdConfig`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `consultas`
--

DROP TABLE IF EXISTS `consultas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `consultas` (
  `IdConsulta` int NOT NULL AUTO_INCREMENT,
  PRIMARY KEY (`IdConsulta`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `consultasdetalles`
--

DROP TABLE IF EXISTS `consultasdetalles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `consultasdetalles` (
  `id` int NOT NULL AUTO_INCREMENT,
  `IdConsulta` int DEFAULT NULL,
  `IdProducto` varchar(255) DEFAULT NULL,
  `Descripcion` varchar(150) DEFAULT NULL,
  `PrecioVenta` double DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `Fk_consultas_detalle_idx` (`IdConsulta`),
  CONSTRAINT `Fk_consultas_detalle` FOREIGN KEY (`IdConsulta`) REFERENCES `consultas` (`IdConsulta`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `cuentascobro`
--

DROP TABLE IF EXISTS `cuentascobro`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cuentascobro` (
  `IdCuentaCobro` int NOT NULL AUTO_INCREMENT,
  `NombreCuenta` varchar(80) NOT NULL,
  `Titular` varchar(100) DEFAULT NULL,
  `Banco` varchar(100) DEFAULT NULL,
  `CBU` varchar(30) DEFAULT NULL,
  `Alias` varchar(80) DEFAULT NULL,
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  `Observaciones` text,
  PRIMARY KEY (`IdCuentaCobro`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `estados`
--

DROP TABLE IF EXISTS `estados`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `estados` (
  `IdEstado` int NOT NULL AUTO_INCREMENT,
  `NombreEstado` varchar(45) NOT NULL,
  PRIMARY KEY (`IdEstado`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `facturas`
--

DROP TABLE IF EXISTS `facturas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `facturas` (
  `IdFactura` int NOT NULL,
  `FechaFactura` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `IdEstado` int NOT NULL,
  `IdVendedor` int NOT NULL,
  `IdCliente` int NOT NULL,
  `DescuentoBono` decimal(12,2) NOT NULL DEFAULT '0.00',
  `SubtotalProveedoresBonificados` decimal(12,2) NOT NULL DEFAULT '0.00',
  `DescuentoEscalon1` decimal(12,2) NOT NULL DEFAULT '0.00',
  `DescuentoEscalon2` decimal(12,2) NOT NULL DEFAULT '0.00',
  `DescuentoEscalon3` decimal(12,2) NOT NULL DEFAULT '0.00',
  `DescuentoProveedores` decimal(12,2) NOT NULL DEFAULT '0.00',
  `IdBonoAplicado` int DEFAULT NULL,
  `FechaFinalizacion` datetime DEFAULT NULL,
  `FechaCobro` datetime DEFAULT NULL,
  PRIMARY KEY (`IdFactura`),
  KEY `fk_facturas_estados_idx` (`IdEstado`),
  KEY `fk_facturas_vendedores_idx` (`IdVendedor`),
  KEY `fk_facturas_clientes` (`IdCliente`),
  CONSTRAINT `fk_facturas_clientes` FOREIGN KEY (`IdCliente`) REFERENCES `clientes` (`IdCliente`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `fk_facturas_estados` FOREIGN KEY (`IdEstado`) REFERENCES `estados` (`IdEstado`) ON UPDATE CASCADE,
  CONSTRAINT `fk_facturas_vendedores` FOREIGN KEY (`IdVendedor`) REFERENCES `vendedores` (`IdVendedor`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `facturasdetalles`
--

DROP TABLE IF EXISTS `facturasdetalles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `facturasdetalles` (
  `id` int NOT NULL AUTO_INCREMENT,
  `IdFactura` int NOT NULL,
  `IdProducto` varchar(13) NOT NULL,
  `IdProveedorVenta` varchar(6) DEFAULT NULL,
  `Descripcion` varchar(150) DEFAULT NULL,
  `PrecioCosto` decimal(12,2) DEFAULT NULL,
  `PrecioVenta` decimal(12,2) DEFAULT NULL,
  `Cantidad` int DEFAULT NULL,
  `SubTotalCosto` decimal(12,2) GENERATED ALWAYS AS ((`PrecioCosto` * `Cantidad`)) STORED,
  `SubTotalVenta` decimal(12,2) GENERATED ALWAYS AS ((`PrecioVenta` * `Cantidad`)) STORED,
  `SubtotalBrutoVenta` decimal(12,2) DEFAULT NULL,
  `DescuentoProveedores` decimal(12,2) DEFAULT NULL,
  `SubtotalNetoVenta` decimal(12,2) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_facturas_detalles_idx` (`IdFactura`),
  CONSTRAINT `fk_facturas_detalles` FOREIGN KEY (`IdFactura`) REFERENCES `facturas` (`IdFactura`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `fiscales`
--

DROP TABLE IF EXISTS `fiscales`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `fiscales` (
  `IdFiscal` int NOT NULL,
  `NombreFiscal` varchar(20) NOT NULL,
  PRIMARY KEY (`IdFiscal`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `mediospago`
--

DROP TABLE IF EXISTS `mediospago`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `mediospago` (
  `IdMedioPago` int NOT NULL AUTO_INCREMENT,
  `NombreMedioPago` varchar(50) NOT NULL,
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  `RequiereCuentaDestino` tinyint(1) NOT NULL DEFAULT '0',
  `RequiereNumeroOperacion` tinyint(1) NOT NULL DEFAULT '0',
  `RequiereAutorizacion` tinyint(1) NOT NULL DEFAULT '0',
  `RequiereCuotas` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`IdMedioPago`),
  UNIQUE KEY `UK_medios_pago_nombre` (`NombreMedioPago`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `pagos`
--

DROP TABLE IF EXISTS `pagos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pagos` (
  `IdPago` int NOT NULL AUTO_INCREMENT,
  `FechaPago` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `IdCliente` int NOT NULL,
  `ImporteTotal` decimal(12,2) NOT NULL,
  `Observaciones` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`IdPago`),
  KEY `FK_pagos_clientes` (`IdCliente`),
  CONSTRAINT `FK_pagos_clientes` FOREIGN KEY (`IdCliente`) REFERENCES `clientes` (`IdCliente`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `pagosdetalle`
--

DROP TABLE IF EXISTS `pagosdetalle`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pagosdetalle` (
  `IdPagoDetalle` int NOT NULL AUTO_INCREMENT,
  `IdPago` int NOT NULL,
  `IdMedioPago` int NOT NULL,
  `IdCuentaCobro` int DEFAULT NULL,
  `Importe` decimal(12,2) NOT NULL,
  `NumeroOperacion` varchar(50) DEFAULT NULL,
  `Autorizacion` varchar(50) DEFAULT NULL,
  `Cuotas` int DEFAULT NULL,
  `TitularTarjeta` varchar(100) DEFAULT NULL,
  `Ultimos4` varchar(4) DEFAULT NULL,
  `Observaciones` text,
  `Anulado` tinyint(1) NOT NULL DEFAULT '0',
  `FechaAlta` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UsuarioAlta` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`IdPagoDetalle`),
  KEY `FK_pagosdetalle_medios` (`IdMedioPago`),
  KEY `FK_pagosdetalle_cuentas` (`IdCuentaCobro`),
  KEY `FK_pagosdetalle_pago` (`IdPago`),
  CONSTRAINT `FK_pagosdetalle_cuentas` FOREIGN KEY (`IdCuentaCobro`) REFERENCES `cuentascobro` (`IdCuentaCobro`),
  CONSTRAINT `FK_pagosdetalle_medios` FOREIGN KEY (`IdMedioPago`) REFERENCES `mediospago` (`IdMedioPago`),
  CONSTRAINT `FK_pagosdetalle_pago` FOREIGN KEY (`IdPago`) REFERENCES `pagos` (`IdPago`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `pagosfacturas`
--

DROP TABLE IF EXISTS `pagosfacturas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pagosfacturas` (
  `IdPagoFactura` int NOT NULL AUTO_INCREMENT,
  `IdPago` int NOT NULL,
  `IdFactura` int NOT NULL,
  `ImporteAplicado` decimal(12,2) NOT NULL,
  PRIMARY KEY (`IdPagoFactura`),
  UNIQUE KEY `UK_pago_factura` (`IdPago`,`IdFactura`),
  KEY `IX_pagosfacturas_factura` (`IdFactura`),
  CONSTRAINT `FK_pagosfacturas_factura` FOREIGN KEY (`IdFactura`) REFERENCES `facturas` (`IdFactura`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_pagosfacturas_pago` FOREIGN KEY (`IdPago`) REFERENCES `pagos` (`IdPago`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `permisos`
--

DROP TABLE IF EXISTS `permisos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `permisos` (
  `IdPermiso` int NOT NULL AUTO_INCREMENT,
  `Codigo` varchar(80) NOT NULL,
  `Descripcion` varchar(255) NOT NULL,
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  `FechaAlta` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdPermiso`),
  UNIQUE KEY `UK_permisos_Codigo` (`Codigo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `productos`
--

DROP TABLE IF EXISTS `productos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `productos` (
  `IdProveedor` varchar(2) NOT NULL DEFAULT '00',
  `RefProveedor` varchar(45) DEFAULT NULL,
  `IdRubro` varchar(6) NOT NULL DEFAULT '00',
  `IdProducto` varchar(13) NOT NULL,
  `Descripcion` varchar(100) NOT NULL,
  `precioCompra` decimal(12,2) NOT NULL DEFAULT '0.00',
  `Descuento` decimal(12,2) NOT NULL DEFAULT '0.00',
  `PrecioCosto` decimal(12,2) GENERATED ALWAYS AS ((`precioCompra` * (1 - (`Descuento` / 100)))) STORED,
  `PrecioVenta` decimal(12,2) DEFAULT NULL,
  `Utilidad` decimal(10,3) NOT NULL DEFAULT '0.000',
  `PrecioOferta` decimal(12,2) DEFAULT NULL,
  `FechaInicioOferta` datetime DEFAULT NULL,
  `FechaFinOferta` datetime DEFAULT NULL,
  `FechaAlta` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  `ImagenPrincipal` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`IdProducto`),
  KEY `fk_producto_proveedor_idx` (`IdProveedor`),
  KEY `IX_productos_Activo` (`Activo`),
  KEY `IX_productos_idRubro` (`IdRubro`),
  CONSTRAINT `FK_productos_proveedores` FOREIGN KEY (`IdProveedor`) REFERENCES `proveedores` (`IdProveedor`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_productos_rubros` FOREIGN KEY (`IdRubro`) REFERENCES `rubros` (`IdRubro`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `proveedores`
--

DROP TABLE IF EXISTS `proveedores`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `proveedores` (
  `IdProveedor` varchar(2) NOT NULL,
  `NombreProveedor` varchar(45) NOT NULL,
  PRIMARY KEY (`IdProveedor`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `roles`
--

DROP TABLE IF EXISTS `roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roles` (
  `IdRol` int NOT NULL AUTO_INCREMENT,
  `NombreRol` varchar(50) NOT NULL,
  `Descripcion` varchar(255) DEFAULT NULL,
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  `FechaAlta` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `FechaModificacion` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdRol`),
  UNIQUE KEY `UK_roles_NombreRol` (`NombreRol`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `roles_permisos`
--

DROP TABLE IF EXISTS `roles_permisos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roles_permisos` (
  `IdRol` int NOT NULL,
  `IdPermiso` int NOT NULL,
  `FechaAlta` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdRol`,`IdPermiso`),
  KEY `FK_roles_permisos_permisos` (`IdPermiso`),
  CONSTRAINT `FK_roles_permisos_permisos` FOREIGN KEY (`IdPermiso`) REFERENCES `permisos` (`IdPermiso`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_roles_permisos_roles` FOREIGN KEY (`IdRol`) REFERENCES `roles` (`IdRol`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `rubros`
--

DROP TABLE IF EXISTS `rubros`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `rubros` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `IdRubro` varchar(6) NOT NULL,
  `NombreRubro` varchar(45) NOT NULL,
  `Descripcion` varchar(255) DEFAULT NULL,
  `ImagenPrincipal` varchar(255) DEFAULT NULL,
  `IdPadre` int DEFAULT NULL,
  `Orden` int NOT NULL DEFAULT '0',
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  `FechaModificacion` datetime DEFAULT NULL,
  `IdCategoriaWeb` bigint DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `uq_rubros_IdRubro` (`IdRubro`),
  KEY `fk_rubros_padre` (`IdPadre`),
  CONSTRAINT `fk_rubros_padre` FOREIGN KEY (`IdPadre`) REFERENCES `rubros` (`Id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `usuarios_roles`
--

DROP TABLE IF EXISTS `usuarios_roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `usuarios_roles` (
  `IdUsuario` int NOT NULL,
  `IdRol` int NOT NULL,
  `FechaAlta` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdUsuario`,`IdRol`),
  KEY `FK_usuarios_roles_roles` (`IdRol`),
  CONSTRAINT `FK_usuarios_roles_roles` FOREIGN KEY (`IdRol`) REFERENCES `roles` (`IdRol`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_usuarios_roles_usuarios` FOREIGN KEY (`IdUsuario`) REFERENCES `usuarios_sistema` (`IdUsuario`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `usuarios_sistema`
--

DROP TABLE IF EXISTS `usuarios_sistema`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `usuarios_sistema` (
  `IdUsuario` int NOT NULL AUTO_INCREMENT,
  `NombreUsuario` varchar(50) NOT NULL,
  `PasswordHash` varchar(255) NOT NULL,
  `NombreCompleto` varchar(120) NOT NULL,
  `Email` varchar(150) DEFAULT NULL,
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  `DebeCambiarPassword` tinyint(1) NOT NULL DEFAULT '1',
  `UltimoLogin` datetime DEFAULT NULL,
  `FechaAlta` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `FechaModificacion` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdUsuario`),
  UNIQUE KEY `UK_usuarios_sistema_NombreUsuario` (`NombreUsuario`),
  UNIQUE KEY `UK_usuarios_sistema_Email` (`Email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `vendedores`
--

DROP TABLE IF EXISTS `vendedores`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vendedores` (
  `IdVendedor` int NOT NULL,
  `NombreVendedor` varchar(60) NOT NULL,
  PRIMARY KEY (`IdVendedor`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `zonas`
--

DROP TABLE IF EXISTS `zonas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `zonas` (
  `IdZona` int NOT NULL,
  `NombreZona` varchar(255) NOT NULL,
  PRIMARY KEY (`IdZona`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-10-18 10:48:44
