using ClosedXML.Excel;
using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Utils;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CostManagement.Infraestructura.Repository.Services
{
    public class ExcelExportService : IExcelExportService
    {
        private readonly IDbContextFactory<CostManagementDbContext> _objContextFactory;
        private readonly IDbContextFactory<SongDbContext> _objSongFactory;
        private readonly ILogger<ExcelExportService> _objLogger;
        private readonly IOptions<ParametrosConfig> _objConfig;
        private static readonly HashSet<string> _hshColumnasCostoPorLibra = new(StringComparer.OrdinalIgnoreCase) { "Costo X Libra", "Costo Venta Unitario", "Costo X Libra Lote" };
        public ExcelExportService(
                ILogger<ExcelExportService> objLogger,
                IOptions<ParametrosConfig> objConfig,
                IDbContextFactory<CostManagementDbContext> objContextFactory,
                IDbContextFactory<CostosDbContext> objCostosFactory,
                IDbContextFactory<SongDbContext> objSongFactory
            )
        {
            _objLogger = objLogger;
            _objContextFactory = objContextFactory;
            _objSongFactory = objSongFactory;
            _objConfig = objConfig;
        }

        public async Task<DataGeneralResult> ObtenerReporteExcel(DataGeneralRequest dataGeneralRequest)
        {

            DataGeneralResult result = new DataGeneralResult { Success = false };
            SqlDbType type;
            result.Success = false;

            try
            {
                await using var context = _objContextFactory.CreateDbContext();
                using var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                // Construir los parámetros nombrados para el batch
                var paramNames = dataGeneralRequest.modelParam
                    .Where(item => item.name != "i_empresa")
                    .Select(item => $"@{item.name} = @{item.name}")
                    .ToList();

                command.CommandText = $"EXEC {dataGeneralRequest.sp} {string.Join(", ", paramNames)}";
                command.CommandType = CommandType.Text;

                foreach (modelParamSQL item in dataGeneralRequest.modelParam)
                {
                    if (item.name == "i_empresa") continue;
                    var objType = (SqlDbType)Enum.Parse(typeof(SqlDbType), item.type, true);
                    command.Parameters.Add(new SqlParameter(item.name, objType)
                    {
                        Value = item.value == null ? DBNull.Value : (object)item.value.ToString()
                    });
                }

                var reader = await command.ExecuteReaderAsync();
                List<DataTable> resultList = new List<DataTable>();

                try
                {
                    do
                    {
                        if (reader.HasRows)
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(reader);
                            resultList.Add(dataTable);

                            if (reader.IsClosed) break;
                        }
                    } while (await reader.NextResultAsync());
                }
                catch (Exception ObjException)
                {
                    connection.Close();
                    ManejoLog<ExcelExportService>.Error(_objLogger, nameof(ExcelExportService), nameof(ObtenerReporteExcel), ObjException);
                    throw;
                }

                if (resultList.Any())
                {
                    result = await DataGeneralExcel(dataGeneralRequest, resultList.First());
                }

                return result;
            }
            catch (Exception ObjException)
            {
                ManejoLog<ExcelExportService>.Error(_objLogger, nameof(ExcelExportService), nameof(ObtenerReporteExcel), ObjException);
                throw;
            }
        }

        public Task<DataGeneralResult> DataDiariosCierreExcel(
    DataGeneralRequest request,
    DiariosCierreDto cierre)
        {
            try
            {
                using var workbook =
                    new XLWorkbook();

                CrearResumenDiarios(
                    workbook,
                    request,
                    cierre);

                CrearTransferenciasDiarios(
                    workbook,
                    cierre);

                CrearRetornosDiarios(
                    workbook,
                    cierre);

                using var stream =
                    new MemoryStream();

                workbook.SaveAs(stream);

                return Task.FromResult(
                    new DataGeneralResult
                    {
                        Success = true,
                        Data = stream.ToArray()
                    });
            }
            catch (Exception ex)
            {
                ManejoLog<ExcelExportService>.Error(
                    _objLogger,
                    nameof(ExcelExportService),
                    nameof(DataDiariosCierreExcel),
                    ex);

                return Task.FromResult(
                    new DataGeneralResult
                    {
                        Success = false,
                        Message = ex.Message
                    });
            }
        }


        private static void AplicarTituloPrincipal(
    IXLRange range)
        {
            range.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#196AA5");

            range.Style.Font.FontColor =
                XLColor.White;

            range.Style.Font.Bold = true;

            range.Style.Font.FontSize = 13;

            range.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Left;
        }


        private static void AplicarTituloSecundario(
            IXLRange range)
        {
            range.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#EAF4FB");

            range.Style.Font.Bold = true;

            range.Style.Font.FontColor =
                XLColor.FromHtml("#0A416C");
        }


        private static void AplicarHeader(
            IXLRange range)
        {
            range.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#F0F2F4");

            range.Style.Font.Bold = true;

            range.Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            range.Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;
        }

        private static void CrearRetornosDiarios(
    XLWorkbook workbook,
    DiariosCierreDto cierre)
        {
            IXLWorksheet ws =
                workbook.Worksheets
                    .Add("Retornos");

            int fila = 1;

            ws.Cell(fila, 1).Value =
                "RETORNO DE CONTENEDORES";

            ws.Range(fila, 1, fila, 7)
                .Merge();

            AplicarTituloPrincipal(
                ws.Range(fila, 1, fila, 7));

            fila += 2;

            if (cierre.objRetornos.lstDiarios.Count == 0)
            {
                ws.Cell(fila, 1).Value =
                    cierre.objRetornos.strMensaje ??
                    "No existen retornos para el período.";

                ws.Range(fila, 1, fila, 7)
                    .Merge();

                return;
            }

            foreach (
                DiarioRetornoCierreDto diario
                in cierre.objRetornos.lstDiarios)
            {
                ws.Cell(fila, 1).Value =
                    diario.strTitulo;

                ws.Range(fila, 1, fila, 7)
                    .Merge();

                AplicarTituloSecundario(
                    ws.Range(fila, 1, fila, 7));

                fila++;

                if (!string.IsNullOrWhiteSpace(
                    diario.strNumeroDocumento))
                {
                    ws.Cell(fila, 1).Value = "Documento";
                    ws.Cell(fila, 2).Value =
                        diario.strNumeroDocumento;
                }

                if (!string.IsNullOrWhiteSpace(
                    diario.strReferencia))
                {
                    ws.Cell(fila, 4).Value = "Referencia";
                    ws.Cell(fila, 5).Value =
                        diario.strReferencia;
                }

                fila++;

                string[] columnas =
                {
            "Código",
            "Cuenta Contable",
            "Clave",
            "Descripción",
            "DEBE ($)",
            "HABER ($)",
            "Detalle"
        };

                for (int i = 0; i < columnas.Length; i++)
                {
                    ws.Cell(fila, i + 1).Value =
                        columnas[i];
                }

                AplicarHeader(
                    ws.Range(fila, 1, fila, 7));

                fila++;

                List<DiarioRetornoFilaDto> filas =
                    diario.lstFilas
                        .OrderBy(x =>
                            x.dcDebe > 0m
                                ? 0
                                : x.dcHaber > 0m
                                    ? 1
                                    : 2)
                        .ThenBy(x => x.intOrden)
                        .ToList();

                foreach (
                    DiarioRetornoFilaDto item
                    in filas)
                {
                    ws.Cell(fila, 1).Value =
                        item.strCodigo ?? string.Empty;

                    ws.Cell(fila, 2).Value =
                        item.strCuentaContable;

                    ws.Cell(fila, 3).Value =
                        item.strClave ?? string.Empty;

                    ws.Cell(fila, 4).Value =
                        item.strDescripcion;

                    ws.Cell(fila, 5).Value =
                        item.dcDebe;

                    ws.Cell(fila, 6).Value =
                        item.dcHaber;

                    ws.Cell(fila, 7).Value =
                        item.strDetalle ?? string.Empty;

                    ws.Range(fila, 5, fila, 6)
                        .Style.NumberFormat.Format =
                            "$ #,##0.00";

                    fila++;
                }

                decimal debe =
                    diario.lstFilas.Sum(x =>
                        x.dcDebe);

                decimal haber =
                    diario.lstFilas.Sum(x =>
                        x.dcHaber);

                decimal diferencia =
                    Math.Round(
                        debe - haber,
                        2,
                        MidpointRounding.AwayFromZero);

                ws.Cell(fila, 4).Value =
                    "TOTAL";

                ws.Cell(fila, 5).Value =
                    debe;

                ws.Cell(fila, 6).Value =
                    haber;

                ws.Range(fila, 4, fila, 6)
                    .Style.Font.SetBold();

                fila++;

                ws.Cell(fila, 4).Value =
                    diferencia == 0m
                        ? "CUADRADO"
                        : "DESCUADRADO";

                ws.Cell(fila, 5).Value =
                    "DIFERENCIA";

                ws.Cell(fila, 6).Value =
                    diferencia;

                fila++;

                if (!string.IsNullOrWhiteSpace(
                    diario.strGlosa))
                {
                    ws.Cell(fila, 1).Value =
                        $"Glosa: {diario.strGlosa}";

                    ws.Range(fila, 1, fila, 7)
                        .Merge();

                    fila++;
                }

                fila += 2;
            }

            ws.Column(1).Width = 16;
            ws.Column(2).Width = 20;
            ws.Column(3).Width = 12;
            ws.Column(4).Width = 45;
            ws.Column(5).Width = 18;
            ws.Column(6).Width = 18;
            ws.Column(7).Width = 45;
        }
        private static int PintarDiarioTransferencia(
    IXLWorksheet ws,
    int fila,
    DiarioTransferenciaCierreDto diario)
        {
            /*
             * Encabezado del asiento.
             */
            ws.Cell(fila, 1).Value =
                $"{diario.strCodigo}  {diario.strTitulo}";

            ws.Range(fila, 1, fila, 6)
                .Merge();

            var titulo =
                ws.Range(fila, 1, fila, 6);

            titulo.Style
                .Fill.SetBackgroundColor(
                    XLColor.FromHtml("#EAF4FB"))
                .Font.SetBold()
                .Font.SetFontColor(
                    XLColor.FromHtml("#0A416C"));

            titulo.Style.Border
                .OutsideBorder =
                    XLBorderStyleValues.Thin;

            fila++;

            if (!string.IsNullOrWhiteSpace(
                diario.strGlosa))
            {
                ws.Cell(fila, 1).Value =
                    diario.strGlosa;

                ws.Range(fila, 1, fila, 6)
                    .Merge();

                ws.Cell(fila, 1)
                    .Style.Font.Italic = true;

                fila++;
            }

            /*
             * Encabezados.
             */
            int filaHeader = fila;

            string[] columnas =
            {
        "Cálculo",
        "Código",
        "Clave",
        "Nombre de Cuenta",
        "DEBE ($)",
        "HABER ($)"
    };

            for (int i = 0; i < columnas.Length; i++)
            {
                ws.Cell(
                    filaHeader,
                    i + 1).Value =
                        columnas[i];
            }

            AplicarHeader(
                ws.Range(
                    filaHeader,
                    1,
                    filaHeader,
                    6));

            fila++;

            /*
             * IMPORTANTE:
             * primero todo el DEBE,
             * después todo el HABER,
             * finalmente las líneas en cero.
             */
            List<DiarioTransferenciaFilaDto> filas =
                diario.lstFilas
                    .OrderBy(x =>
                        x.dcDebe > 0m
                            ? 0
                            : x.dcHaber > 0m
                                ? 1
                                : 2)
                    .ThenBy(x => x.intOrden)
                    .ToList();

            foreach (
                DiarioTransferenciaFilaDto item
                in filas)
            {
                if (item.dcCalculo.HasValue)
                {
                    ws.Cell(fila, 1).Value =
                        item.dcCalculo.Value;

                    ws.Cell(fila, 1)
                        .Style.NumberFormat.Format =
                            "0.00%";
                }

                ws.Cell(fila, 2).Value =
                    item.strCodigo ?? string.Empty;

                ws.Cell(fila, 3).Value =
                    item.strClave ?? string.Empty;

                ws.Cell(fila, 4).Value =
                    item.strNombreCuenta;

                ws.Cell(fila, 5).Value =
                    item.dcDebe;

                ws.Cell(fila, 6).Value =
                    item.dcHaber;

                ws.Cell(fila, 5)
                    .Style.NumberFormat.Format =
                        "$ #,##0.00";

                ws.Cell(fila, 6)
                    .Style.NumberFormat.Format =
                        "$ #,##0.00";

                fila++;
            }

            decimal debe =
                diario.lstFilas.Sum(x =>
                    x.dcDebe);

            decimal haber =
                diario.lstFilas.Sum(x =>
                    x.dcHaber);

            decimal diferencia =
                Math.Round(
                    debe - haber,
                    2,
                    MidpointRounding.AwayFromZero);

            /*
             * TOTAL.
             */
            ws.Cell(fila, 4).Value = "TOTAL";
            ws.Cell(fila, 5).Value = debe;
            ws.Cell(fila, 6).Value = haber;

            ws.Range(fila, 4, fila, 6)
                .Style.Font.SetBold();

            ws.Range(fila, 5, fila, 6)
                .Style.NumberFormat.Format =
                    "$ #,##0.00";

            fila++;

            /*
             * ESTADO / DIFERENCIA.
             */
            ws.Cell(fila, 4).Value =
                diferencia == 0m
                    ? "CUADRADO"
                    : "DESCUADRADO";

            ws.Cell(fila, 5).Value =
                "DIFERENCIA";

            ws.Cell(fila, 6).Value =
                diferencia;

            ws.Cell(fila, 6)
                .Style.NumberFormat.Format =
                    "$ #,##0.00";

            XLColor fondo =
                diferencia == 0m
                    ? XLColor.FromHtml("#EAF6EE")
                    : XLColor.FromHtml("#FFF0EF");

            XLColor fuente =
                diferencia == 0m
                    ? XLColor.FromHtml("#18783A")
                    : XLColor.FromHtml("#B42318");

            ws.Range(fila, 4, fila, 6)
                .Style.Fill
                .SetBackgroundColor(fondo);

            var rangoEstado =ws.Range(fila, 4, fila, 6);

            rangoEstado.Style.Fill.BackgroundColor = fondo;

            rangoEstado.Style.Font.FontColor = fuente;

            rangoEstado.Style.Font.Bold = true;

            return fila;
        }

        private static void CrearTransferenciasDiarios(
    XLWorkbook workbook,
    DiariosCierreDto cierre)
        {
            IXLWorksheet ws =
                workbook.Worksheets
                    .Add("Transferencias");

            int fila = 1;

            ws.Cell(fila, 1).Value =
                "TRANSFERENCIAS DE COSTOS DE PRODUCCIÓN";

            ws.Range(fila, 1, fila, 6)
                .Merge();

            AplicarTituloPrincipal(
                ws.Range(fila, 1, fila, 6));

            fila++;

            ws.Cell(fila, 1).Value = "Período";
            ws.Cell(fila, 2).Value =
                cierre.strPeriodoTexto;

            fila += 2;

            foreach (
                DiarioTransferenciaCierreDto diario
                in cierre.objTransferenciaGif.lstDiarios)
            {
                fila =
                    PintarDiarioTransferencia(
                        ws,
                        fila,
                        diario);

                fila += 2;
            }

            ws.SheetView.FreezeRows(1);

            ws.Column(1).Width = 14;
            ws.Column(2).Width = 18;
            ws.Column(3).Width = 12;
            ws.Column(4).Width = 55;
            ws.Column(5).Width = 18;
            ws.Column(6).Width = 18;

            ws.Columns(5, 6)
                .Style.NumberFormat.Format =
                    "$ #,##0.00";
        }
        private static void CrearResumenDiarios(
    XLWorkbook workbook,
    DataGeneralRequest request,
    DiariosCierreDto cierre)
        {
            IXLWorksheet ws =
                workbook.Worksheets.Add("Resumen");

            ws.Cell(1, 1).Value =
                string.IsNullOrWhiteSpace(request.title)
                    ? "DIARIOS DE CIERRE DE COSTOS"
                    : request.title;

            ws.Range(1, 1, 1, 4).Merge();

            ws.Cell(1, 1).Style
                .Font.SetBold()
                .Font.SetFontSize(15)
                .Font.SetFontColor(
                    XLColor.FromHtml("#196AA5"));

            ws.Cell(3, 1).Value = "Período";
            ws.Cell(3, 2).Value = cierre.strPeriodoTexto;

            ws.Cell(4, 1).Value = "Generación";
            ws.Cell(4, 2).Value = cierre.intIdGeneracion;

            ws.Cell(5, 1).Value = "Estado";
            ws.Cell(5, 2).Value = cierre.strEstado;

            ws.Cell(7, 1).Value =
                "Base de cálculo";

            ws.Cell(7, 2).Value =
                cierre.objTransferenciaGif.dcBaseCalculo;

            ws.Cell(8, 1).Value =
                "Gasto indirecto a transferir";

            ws.Cell(8, 2).Value =
                cierre.objTransferenciaGif.dcTotalGastoIndirecto;

            ws.Cell(10, 1).Value =
                "Diarios de transferencia";

            ws.Cell(10, 2).Value =
                cierre.objTransferenciaGif
                    .lstDiarios.Count;

            ws.Cell(11, 1).Value =
                "Diarios de retorno";

            ws.Cell(11, 2).Value =
                cierre.objRetornos
                    .lstDiarios.Count;

            ws.Range("A3:A11")
                .Style.Font.SetBold();

            ws.Range("B7:B8")
                .Style.NumberFormat.Format =
                    "$ #,##0.00";

            ws.Columns(1, 2)
                .AdjustToContents();
        }

        public async Task<DataGeneralResult> DataGeneralExcelHojas(DataGeneralRequest dataGeneralRequest, List<(string strNombreHoja, DataTable objTabla)> lstHojas)
        {
            try
            {
                using var objLibro = new XLWorkbook();
                foreach (var (strNombreHoja, objTabla) in lstHojas)
                {
                    var objRequest = new DataGeneralRequest { title = dataGeneralRequest.title, title2 = dataGeneralRequest.title2, sp = dataGeneralRequest.sp, modelParam = dataGeneralRequest.modelParam };
                    DataGeneralResult objResultado = await DataGeneralExcel(objRequest, objTabla);
                    if (!objResultado.Success) return objResultado;
                    using var objStream = new MemoryStream(objResultado.Data);
                    using var objLibroHoja = new XLWorkbook(objStream);
                    int intParte = 0;
                    foreach (var objHoja in objLibroHoja.Worksheets) { intParte++; objHoja.CopyTo(objLibro, intParte == 1 ? strNombreHoja : $"{strNombreHoja} {intParte}"); }
                }
                using var objSalida = new MemoryStream();
                objLibro.SaveAs(objSalida);
                return new DataGeneralResult { Success = true, Data = objSalida.ToArray() };
            }
            catch (Exception ex)
            {
                return new DataGeneralResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<DataGeneralResult> DataGeneralExcel(DataGeneralRequest dataGeneralRequest, DataTable dataTable, CostosUnitarios objCostUni = null)
        {
            DataGeneralResult result = new DataGeneralResult { Success = false };

            try
            {
                byte[] file = null;

                if (dataTable != null && dataTable.Rows.Count > 0)
                {
                    // Si no se especifican columnas, usar todas las del DataTable
                    if (dataGeneralRequest.columnas == null || dataGeneralRequest.columnas.Length == 0)
                    {
                        dataGeneralRequest.columnas = dataTable.Columns
                            .Cast<DataColumn>()
                            .Select(col => col.ColumnName)
                            .ToArray();
                    }

                    using var workbook = new XLWorkbook();

                    const int maxRowsPerSheet = 500_000;
                    int totalRows = dataTable.Rows.Count;
                    int totalSheets = (int)Math.Ceiling((double)totalRows / maxRowsPerSheet);
                    int totalColumnas = dataGeneralRequest.columnas.Length;

                    // Obtener los índices de las columnas una sola vez
                    var columnIndices = dataGeneralRequest.columnas
                        .Select(col => dataTable.Columns[col]?.Ordinal ?? -1)
                        .ToArray();

                    for (int sheetIndex = 0; sheetIndex < totalSheets; sheetIndex++)
                    {
                        var worksheet = workbook.Worksheets.Add($"Data{sheetIndex + 1}");

                        // Título
                        worksheet.Cell(1, 1).Value = dataGeneralRequest.title;
                        worksheet.Cell(1, 1).Style.Font.FontName = "Tahoma";
                        worksheet.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(13)
                            .Font.SetFontColor(XLColor.FromHtml("#FF196AA5"));

                        // Subtítulo
                        worksheet.Cell(2, 1).Value = dataGeneralRequest.title2;
                        worksheet.Cell(2, 1).Style.Font.FontName = "Tahoma";
                        worksheet.Cell(2, 1).Style.Font.SetFontSize(10);

                       
                        // Fila 4: costos unitarios (ARRIBA del nombre)
                        int filaCostosUnitarios = 4;
                        bool hayCostosUnitarios = objCostUni != null;

                        if (hayCostosUnitarios)
                        {
                            var mapaCostos = MapearCostosPorColumna(objCostUni, dataGeneralRequest.columnas);
                            for (int col = 0; col < totalColumnas; col++)
                            {
                                string nombreCol = dataGeneralRequest.columnas[col];
                                if (mapaCostos.TryGetValue(nombreCol, out decimal valorUnitario))
                                {
                                    var celda = worksheet.Cell(filaCostosUnitarios, col + 1);
                                    celda.Value = (double)valorUnitario;
                                    celda.Style.NumberFormat.Format = "_(\"$\"* #,##0.0000_);_(\"$\"* (#,##0.0000);_(\"$\"* \"-\"????_);_(@_)";
                                    celda.Style.Font.SetBold();
                                }
                            }
                        }

                        // Fila 5: nombres (encabezado)
                        int filaInicio = hayCostosUnitarios ? 5 : 4;

                        for (int col = 0; col < totalColumnas; col++)
                        {
                            worksheet.Cell(filaInicio, col + 1).Value = dataGeneralRequest.columnas[col];
                            worksheet.Cell(filaInicio, col + 1).Style.Font.SetBold();
                        }

                        worksheet.SheetView.FreezeRows(filaInicio);
                        // --- LÓGICA GENÉRICA PARA SÚPER-ENCABEZADOS (FILA 3) ---
                        int filaGrupos = 3;
                        // LÓGICA ANÓNIMA PARA GRUPOS (FILA 3)
                        var tipoDto = dataTable.ExtendedProperties["SourceType"] as Type;
                        if (tipoDto != null)
                        {
                            var propsConGrupo = tipoDto.GetProperties()
                                .Where(p => p.PropertyType.IsClass && p.PropertyType != typeof(string) && p.GetCustomAttribute<ColumnAttribute>() != null);

                            foreach (var pPadre in propsConGrupo)
                            {
                                var nombreGrupo = pPadre.GetCustomAttribute<ColumnAttribute>().Name;
                                var colsHijas = pPadre.PropertyType.GetProperties().Select(sp => sp.GetCustomAttribute<ColumnAttribute>()?.Name ?? sp.Name).ToList();

                                int inicio = 0, fin = 0;
                                for (int i = 0; i < dataGeneralRequest.columnas.Length; i++)
                                {
                                    if (colsHijas.Contains(dataGeneralRequest.columnas[i]))
                                    {
                                        if (inicio == 0) inicio = i + 1;
                                        fin = i + 1;
                                    }
                                }

                                if (inicio > 0)
                                {
                                    var range = worksheet.Range(3, inicio, 3, fin);
                                    range.Merge().Value = nombreGrupo;
                                    range.Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                                    range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                }
                            }
                        }

                        // Rango de filas para esta hoja
                        int rowStart = sheetIndex * maxRowsPerSheet;
                        int rowEnd = Math.Min(rowStart + maxRowsPerSheet, totalRows);

                        // Llenar datos
                        bool blCostoVentaUni = dataGeneralRequest.sp?.StartsWith("costo-venta-uni", StringComparison.OrdinalIgnoreCase) == true;
                        for (int i = rowStart; i < rowEnd; i++)
                        {
                            var row = dataTable.Rows[i];
                            for (int j = 0; j < totalColumnas; j++)
                            {
                                if (columnIndices[j] >= 0)
                                {
                                    var cellValue = row[columnIndices[j]];

                                    if (cellValue != null && cellValue != DBNull.Value)
                                    {
                                        //if (cellValue is bool boolValue)
                                        //{
                                        //    worksheet.Cell(filaInicio + 1 + (i - rowStart), j + 1).Value = boolValue;
                                        //}
                                        //else
                                        //{
                                        //    worksheet.Cell(filaInicio + 1 + (i - rowStart), j + 1).Value = cellValue.ToString();
                                        //}
                                        var cell = worksheet.Cell(filaInicio + 1 + (i - rowStart), j + 1);
                                        bool esPorcentaje = dataGeneralRequest.columnas[j].Contains("%");
                                        switch (cellValue)
                                        {
                                            case bool b:
                                                cell.Value = b;
                                                break;

                                            case DateTime dt:
                                                cell.Value = dt;
                                                cell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                                                break;

                                            case DateOnly d:
                                                cell.Value = d.ToDateTime(TimeOnly.MinValue);
                                                cell.Style.DateFormat.Format = "yyyy-MM-dd";
                                                break;

                                            case int or long or short or byte:
                                                cell.Value = Convert.ToInt64(cellValue);
                                                break;

                                            case decimal or double or float:
                                                var number = Convert.ToDouble(cellValue);
                                                if (esPorcentaje)
                                                {
                                                    double factor = Math.Pow(10, 4);
                                                    number = Math.Truncate(number * factor) / factor;

                                                    cell.Value = number;
                                                    cell.Style.NumberFormat.Format = "0.00%";
                                                }
                                                else
                                                {
                                                    cell.Value = number;
                                                    cell.Style.NumberFormat.Format = blCostoVentaUni && _hshColumnasCostoPorLibra.Contains(dataGeneralRequest.columnas[j]) ? "#,##0.0000" : "#,##0.00";
                                                }
                                                break;

                                            default:
                                                cell.Value = cellValue.ToString(); 
                                                break;
                                        }
                                    }
                                }
                            }
                        }

                        // Autoajuste columnas
                        for (int c = 1; c <= totalColumnas; c++)
                        {
                            worksheet.Column(c).AdjustToContents(filaInicio, filaInicio + Math.Min(1000, rowEnd - rowStart));
                        }
                    }

                    using var stream = new MemoryStream();
                    workbook.SaveAs(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    file = stream.ToArray();

                    return new DataGeneralResult
                    {
                        Success = true,
                        Data = file
                    };
                }
                else
                {
                    return new DataGeneralResult
                    {
                        Success = false,
                        Message = "No existen datos"
                    };
                }
            }
            catch (Exception ex)
            {
                return new DataGeneralResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        private static Dictionary<string, decimal> MapearCostosPorColumna(CostosUnitarios costos, string[] columnasDestino)
        {
            var dict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (costos == null) return dict;

            // Set de nombres válidos según lo que el Excel realmente va a pintar
            var columnasValidas = new HashSet<string>(columnasDestino, StringComparer.OrdinalIgnoreCase);

            foreach (var prop in typeof(CostosUnitarios).GetProperties())
            {
                if (prop.PropertyType != typeof(decimal)) continue;

                var valor = (decimal)prop.GetValue(costos);

                // 1º intento: [Column]
                var nombreColumn = prop.GetCustomAttribute<ColumnAttribute>()?.Name;
                // 2º intento: [JsonProperty]
                var nombreJson = prop.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName;

                // Elegimos el que realmente coincida con una columna del Excel.
                // Prioriza Column; si no matchea, prueba JsonProperty.
                string clave = null;
                if (nombreColumn != null && columnasValidas.Contains(nombreColumn))
                    clave = nombreColumn;
                else if (nombreJson != null && columnasValidas.Contains(nombreJson))
                    clave = nombreJson;

                if (clave != null)
                    dict[clave] = valor;
            }
            return dict;
        }

        public byte[] ExportarLiquidacionesAExcel(List<LiquidacionResultado> liquidaciones)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Liquidaciones");

                // Crear encabezados
                CrearEncabezados(worksheet);

                // Llenar datos
                LlenarDatos(worksheet, liquidaciones);

                // Aplicar formato
                AplicarFormato(worksheet);

                // Convertir a bytes
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public List<InvValDataDto> LeerExcelInvVal(Stream archivoStream)
        {
            var listaResultado = new List<InvValDataDto>();

            using (var workbook = new XLWorkbook(archivoStream))
            {
                // Accedemos específicamente a la tercera hoja por su nombre
                if (!workbook.Worksheets.TryGetWorksheet("libras", out var worksheet))
                {
                    throw new Exception("No se encontró la hoja llamada 'libras'");
                }


                // Seleccionamos el rango usado y saltamos la primera fila (encabezados)
                var filas = worksheet.RangeUsed().RowsUsed().Skip(1);

                foreach (var fila in filas)
                {
                    // --- VALIDACIÓN PARA OMITIR SUMATORIAS ---
                    // 1. Verificamos si la celda de Lote o CAM está vacía
                    // 2. O si alguna celda contiene la palabra "TOTAL" o "SUMA"
                    string valorCeldaCam = fila.Cell("A").GetValue<string>().Trim().ToUpper();

                    if (string.IsNullOrEmpty(valorCeldaCam))
                    {
                        continue; // Salta esta fila y pasa a la siguiente
                    }
                    // Usamos métodos TryGet para evitar el error de conversión que mencionaste
                    var dto = new InvValDataDto
                    {
                        strCam = fila.Cell("A").GetValue<string>(),
                        strBodDescri = fila.Cell("B").GetValue<string>(),
                        strProd = fila.Cell("C").GetValue<string>(),
                        strProDesesp = fila.Cell("E").GetValue<string>(),
                        strNomTal = fila.Cell("F").GetValue<string>(),

                        // Estos métodos Try evitan el error "Cannot convert to Int32"
                        intLote = GetIntSafe(fila.Cell("G")),

                        strClpNomCom = fila.Cell("H").GetValue<string>(),
                        dtFecha = GetDateSafe(fila.Cell("I")),
                        strClpGrupo = fila.Cell("J").GetValue<string>(),

                        dcLibras = GetDoubleSafe(fila.Cell("K")),
                        dcMaster = GetDoubleSafe(fila.Cell("L")),

                        strClas01 = fila.Cell("M").GetValue<string>(),
                        strProClas05 = fila.Cell("N").GetValue<string>(),
                        strGrupo = fila.Cell("O").GetValue<string>(),
                        strCodigoTalla = fila.Cell("P").GetValue<string>(),

                        dcCosto = GetDoubleSafe(fila.Cell("Q")),
                        dcTotal = GetDoubleSafe(fila.Cell("R"))
                    };

                    listaResultado.Add(dto);
                }
            }
            return listaResultado;
        }

        #region Métodos de Seguridad para Conversión

        private int GetIntSafe(IXLCell cell)
        {
            if (cell.IsEmpty()) return 0;
            // Si la celda tiene un error o no es número, devolvemos 0 en lugar de romper el programa
            return cell.TryGetValue(out int val) ? val : 0;
        }

        private double GetDoubleSafe(IXLCell cell)
        {
            if (cell.IsEmpty()) return 0.0;
            return cell.TryGetValue(out double val) ? val : 0.0;
        }

        private DateTime GetDateSafe(IXLCell cell)
        {
            if (cell.IsEmpty()) return DateTime.MinValue;
            return cell.TryGetValue(out DateTime val) ? val : DateTime.MinValue;
        }

        #endregion
        private void CrearEncabezados(IXLWorksheet worksheet)
        {
            // Definir encabezados
            worksheet.Cell(1, 1).Value = "Tipo Liquidación";
            worksheet.Cell(1, 2).Value = "Lote";
            worksheet.Cell(1, 3).Value = "Mes";
            worksheet.Cell(1, 4).Value = "Fecha Lote";
            worksheet.Cell(1, 5).Value = "Planta";
            worksheet.Cell(1, 6).Value = "Proveedor";
            worksheet.Cell(1, 7).Value = "Piscina";
            worksheet.Cell(1, 8).Value = "Masters";
            worksheet.Cell(1, 9).Value = "Libras";
            worksheet.Cell(1, 10).Value = "Tipo Producto";
            worksheet.Cell(1, 11).Value = "Fecha Liquidación";
            worksheet.Cell(1, 12).Value = "Hora Liquidación";
            worksheet.Cell(1, 13).Value = "Código Producto";
            worksheet.Cell(1, 14).Value = "Descripción";
            worksheet.Cell(1, 15).Value = "Talla";
            worksheet.Cell(1, 16).Value = "Certificado";
            worksheet.Cell(1, 17).Value = "Bodega";
            worksheet.Cell(1, 18).Value = "Grupo";
            worksheet.Cell(1, 19).Value = "País";
            worksheet.Cell(1, 20).Value = "Clasificación 02";
            worksheet.Cell(1, 21).Value = "Clase Pago";
            worksheet.Cell(1, 22).Value = "Clasificadora Lid";
            worksheet.Cell(1, 23).Value = "Clasificadora";
            worksheet.Cell(1, 24).Value = "Fecha Turno";
            worksheet.Cell(1, 25).Value = "Turno";
            worksheet.Cell(1, 26).Value = "Inicio Liquidación";
            worksheet.Cell(1, 27).Value = "Fin Liquidación";
            worksheet.Cell(1, 28).Value = "Horas Liquidación";
            worksheet.Cell(1, 29).Value = "Precio Compra";
            worksheet.Cell(1, 30).Value = "Total Dólares";

            // Aplicar estilo a los encabezados
            var headerRange = worksheet.Range("A1:AD1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private void LlenarDatos(IXLWorksheet worksheet, List<LiquidacionResultado> liquidaciones)
        {
            int fila = 2;

            foreach (var liq in liquidaciones)
            {
                worksheet.Cell(fila, 1).Value = liq.strTipoLiq ?? "";
                worksheet.Cell(fila, 2).Value = liq.intLote;
                worksheet.Cell(fila, 3).Value = liq.intMes ?? 0;

                // Fecha Lote
                if (liq.dtFechaLote.HasValue)
                {
                    worksheet.Cell(fila, 4).Value = liq.dtFechaLote.Value.ToDateTime(TimeOnly.MinValue);
                    worksheet.Cell(fila, 4).Style.DateFormat.Format = "dd/MM/yyyy";
                }

                worksheet.Cell(fila, 5).Value = liq.strPlanta ?? "";
                worksheet.Cell(fila, 6).Value = liq.strProveedor ?? "";
                worksheet.Cell(fila, 7).Value = liq.strRloPiscin ?? "";

                // Masters
                worksheet.Cell(fila, 8).Value = liq.dcMasters ?? 0;
                worksheet.Cell(fila, 8).Style.NumberFormat.Format = "#,##0.00";

                // Libras
                worksheet.Cell(fila, 9).Value = liq.dcLibras ;
                worksheet.Cell(fila, 9).Style.NumberFormat.Format = "#,##0.00";

                worksheet.Cell(fila, 10).Value = liq.strTipPro ?? "";

                // Fecha Liquidación
                if (liq.dtFechaLiq.HasValue)
                {
                    worksheet.Cell(fila, 11).Value = liq.dtFechaLiq.Value.ToDateTime(TimeOnly.MinValue);
                    worksheet.Cell(fila, 11).Style.DateFormat.Format = "dd/MM/yyyy";
                }

                worksheet.Cell(fila, 12).Value = liq.intHoraLiq ?? 0;
                worksheet.Cell(fila, 13).Value = liq.intCodProd ?? 0;
                worksheet.Cell(fila, 14).Value = liq.strDescripcion ?? "";
                worksheet.Cell(fila, 15).Value = liq.strTalla ?? "";
                worksheet.Cell(fila, 16).Value = liq.strCertificado ?? "";
                worksheet.Cell(fila, 17).Value = liq.strBodDescri ?? "";
                worksheet.Cell(fila, 18).Value = liq.strClpGrupo ?? "";
                worksheet.Cell(fila, 19).Value = liq.strPaiDescri ?? "";
                worksheet.Cell(fila, 20).Value = liq.strProClas02 ?? "";
                worksheet.Cell(fila, 21).Value = liq.strProClasePago ?? "";
                worksheet.Cell(fila, 22).Value = liq.strLidClasificadora ?? "";
                worksheet.Cell(fila, 23).Value = liq.strClasificadora ?? "";

                // Fecha Turno
                if (liq.dtFechaTurno.HasValue)
                {
                    worksheet.Cell(fila, 24).Value = liq.dtFechaTurno.Value.ToDateTime(TimeOnly.MinValue);
                    worksheet.Cell(fila, 24).Style.DateFormat.Format = "dd/MM/yyyy";
                }

                worksheet.Cell(fila, 25).Value = liq.strTurno ?? "";

                // Inicio Liquidación
                worksheet.Cell(fila, 26).Value = liq.dtInicioLiquidacion;
                worksheet.Cell(fila, 26).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

                // Fin Liquidación
                if (liq.dtFinLiquidacion.HasValue)
                {
                    worksheet.Cell(fila, 27).Value = liq.dtFinLiquidacion.Value;
                    worksheet.Cell(fila, 27).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                }

                // Horas Liquidación
                worksheet.Cell(fila, 28).Value = liq.dcHorasLiquidacion ?? 0;
                worksheet.Cell(fila, 28).Style.NumberFormat.Format = "#,##0.00";

                // Precio Compra
                worksheet.Cell(fila, 29).Value = liq.dcPrecioCompra ?? 0;
                worksheet.Cell(fila, 29).Style.NumberFormat.Format = "#,##0.0000";

                // Total Dólares
                worksheet.Cell(fila, 30).Value = liq.dcTotalDol ?? 0;
                worksheet.Cell(fila, 30).Style.NumberFormat.Format = "#,##0.0000";

                fila++;
            }
        }

        private void AplicarFormato(IXLWorksheet worksheet)
        {
            // Ajustar ancho de columnas automáticamente
            worksheet.Columns().AdjustToContents();

            // Aplicar bordes a todas las celdas con datos
            var rangoConDatos = worksheet.RangeUsed();
            if (rangoConDatos != null)
            {
                rangoConDatos.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rangoConDatos.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            // Congelar la primera fila (encabezados)
            worksheet.SheetView.FreezeRows(1);
        }
    }
}

