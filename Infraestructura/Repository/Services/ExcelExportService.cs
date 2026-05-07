using ClosedXML.Excel;
using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.Repository.Interface;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Drawing;
using System.Reflection;

namespace CostManagement.Infraestructura.Repository.Services
{
    public class ExcelExportService : IExcelExportService
    {
        public async Task<DataGeneralResult> DataGeneralExcel(DataGeneralRequest dataGeneralRequest, DataTable dataTable)
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

                        // Encabezados en fila 4
                        int filaInicio = 4;
                        worksheet.SheetView.FreezeRows(4);

                        for (int col = 0; col < totalColumnas; col++)
                        {
                            worksheet.Cell(filaInicio, col + 1).Value = dataGeneralRequest.columnas[col];
                            worksheet.Cell(filaInicio, col + 1).Style.Font.SetBold();
                        }
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
                                                    cell.Style.NumberFormat.Format = "#,##0.00";
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

