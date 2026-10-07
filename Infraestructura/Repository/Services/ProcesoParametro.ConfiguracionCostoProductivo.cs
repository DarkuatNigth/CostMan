using CostManagement.Aplicación.DTos;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Infraestructura.EF_Core;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CostManagement.Infraestructura.Repository.Services
{
    /// <summary>
    /// Parte del mismo ProcesoParametro existente.
    ///
    /// IMPORTANTE:
    /// - No usa SPs ni DbCommand para el mantenimiento.
    /// - Se asume que las tablas de configuración YA fueron incorporadas al
    ///   modelo de CostosDbContext mediante Scaffold-DbContext.
    /// - Toda lectura/escritura se hace con EF Core sobre ConnectionCostos.
    /// - Las consultas son deliberadamente simples y se termina de relacionar
    ///   la información en memoria, evitando LINQ complejo innecesario.
    /// </summary>
    public partial class ProcesoParametro
    {
        private static readonly HashSet<string> _hshFuentesIqfConfiguracion =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "50305010261", "50305010007", "50305010008", "50305010009",
                "50305010011", "50305010016", "50305010031", "50305010032",
                "50305010033", "50305010036", "50305010037", "50305010041",
                "50305010050", "50305010059", "50305010284", "50305010070",
                "50305010072", "50305010073", "50305010074", "50305010075",
                "50305010076", "50305010094", "50305010097", "50305010104",
                "50305010105", "50305010106", "50305010112", "50305010114",
                "50305010115", "50305010125", "50305010140", "50305010162",
                "50305010210", "50305010215", "50305010216", "50305010217",
                "50305010218", "50305010219", "50305010220", "50305010239",
                "50305010240", "50305010258", "50305010278", "50305010272",
                "50305010324", "50305010350", "50305010044"
            };

        public async Task<CostoProductivoMantenimientoDto>
            ConsultarMantenimientoCostoProductivo()
        {
            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async ctx =>
                    {
                        // Se leen tablas independientes. La composición se hace en
                        // memoria para que el SQL generado por EF siga siendo básico.
                        var etapas = await ctx.TbEtapacosto
                            .AsNoTracking()
                            .OrderBy(x => x.EcOrden)
                            .ThenBy(x => x.EcCodigo)
                            .ToListAsync();

                        var procesos = await ctx.TbProcesocosto
                            .AsNoTracking()
                            .OrderBy(x => x.PcOrden)
                            .ThenBy(x => x.PcCodigo)
                            .ToListAsync();

                        var tiposLote = await ctx.TbTipolote
                            .AsNoTracking()
                            .OrderBy(x => x.TlCodigo)
                            .ToListAsync();

                        var tiposProceso = await ctx.TbTipoOtrosProcesos
                            .AsNoTracking()
                            .OrderBy(x => x.ToNivel)
                            .ThenBy(x => x.ToCodigo)
                            .ToListAsync();

                        var matriz = await ctx.TbConfigTipoLiquidacionProcesoCosto
                            .AsNoTracking()
                            .OrderBy(x => x.CgToCodigo)
                            .ThenBy(x => x.CgPcCodigo)
                            .ToListAsync();

                        var cuentas = await ctx.TbConfigCuentaCosto
                            .AsNoTracking()
                            .OrderBy(x => x.CcEcCodigo)
                            .ThenBy(x => x.CcCenCodigo)
                            .ThenBy(x => x.CcSubCodigo)
                            .ThenBy(x => x.CcCtaNumero)
                            .ToListAsync();

                        Dictionary<string, string> dicEtapas = etapas
                            .GroupBy(x => NormalizarCfg(x.EcCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(
                                g => g.Key,
                                g => (g.First().EcNombre ?? string.Empty).Trim(),
                                StringComparer.OrdinalIgnoreCase);

                        Dictionary<string, string> dicTiposLote = tiposLote
                            .GroupBy(x => NormalizarCfg(x.TlCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(
                                g => g.Key,
                                g => (g.First().TlNombre ?? string.Empty).Trim(),
                                StringComparer.OrdinalIgnoreCase);

                        HashSet<string> procesosEnMatriz = matriz
                            .Select(x => NormalizarCfg(x.CgPcCodigo))
                            .Where(x => x.Length > 0)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        var resultado = new CostoProductivoMantenimientoDto
                        {
                            lstEtapas = etapas.Select(x => new EtapaCostoMantenimientoDto
                            {
                                strCodigo = NormalizarCfg(x.EcCodigo),
                                strNombre = (x.EcNombre ?? string.Empty).Trim(),
                                intOrden = x.EcOrden,
                                blActivo = EstadoActivo(x.EcEstado)
                            }).ToList(),

                            lstProcesos = procesos.Select(x =>
                            {
                                string etapaCodigo = NormalizarCfg(x.PcEcCodigo);
                                string etapaNombre;
                                dicEtapas.TryGetValue(etapaCodigo, out etapaNombre);

                                return new ProcesoCostoMantenimientoDto
                                {
                                    strCodigo = NormalizarCfg(x.PcCodigo),
                                    strNombre = (x.PcNombre ?? string.Empty).Trim(),
                                    strEtapaCodigo = etapaCodigo,
                                    strEtapa = etapaNombre ?? string.Empty,
                                    intOrden = x.PcOrden,
                                    blActivo = EstadoActivo(x.PcEstado),
                                    blEnMatriz = procesosEnMatriz.Contains(NormalizarCfg(x.PcCodigo))
                                };
                            }).ToList(),

                            lstTiposLote = tiposLote.Select(x => new TipoLoteMantenimientoDto
                            {
                                strCodigo = NormalizarCfg(x.TlCodigo),
                                strNombre = (x.TlNombre ?? string.Empty).Trim(),
                                blActivo = EstadoActivo(x.TlEstado)
                            }).ToList(),

                            lstTiposProceso = tiposProceso.Select(x =>
                            {
                                string tipoLoteCodigo = NormalizarCfg(x.ToTlCodigo);
                                string tipoLoteNombre;
                                dicTiposLote.TryGetValue(tipoLoteCodigo, out tipoLoteNombre);

                                return new TipoProcesoMantenimientoDto
                                {
                                    strCodigo = NormalizarCfg(x.ToCodigo),
                                    strNombre = (x.ToNombre ?? string.Empty).Trim(),
                                    strTipoLoteCodigo = tipoLoteCodigo,
                                    strTipoLote = tipoLoteNombre ?? string.Empty,
                                    intNivel = x.ToNivel,
                                    blActivo = EstadoActivo(x.ToEstado),
                                    blEsNuevo = false
                                };
                            }).ToList(),

                            lstMatriz = matriz.Select(x => new AplicacionCostoMantenimientoDto
                            {
                                strProcesoCodigo = NormalizarCfg(x.CgPcCodigo),
                                strTipoCodigo = NormalizarCfg(x.CgToCodigo),
                                strRegla = ReglaCostoMantenimientoDto.Normalizar(x.CgConfig),
                                blActivo = EstadoActivo(x.CgEstado)
                            }).ToList(),

                            lstCuentas = cuentas.Select(x =>
                            {
                                string etapaCodigo = NormalizarCfg(x.CcEcCodigo);
                                string etapaNombre;
                                dicEtapas.TryGetValue(etapaCodigo, out etapaNombre);

                                var dto = new CuentaCostoMantenimientoDto
                                {
                                    intId = x.CcId,
                                    intEmpresa = x.CcEmpCodigo,
                                    strEtapaCodigo = etapaCodigo,
                                    strEtapa = etapaNombre ?? string.Empty,
                                    strCentroCodigo = NormalizarCfg(x.CcCenCodigo),
                                    strSubcentroCodigo = NormalizarCfg(x.CcSubCodigo),
                                    strCuenta = NormalizarCfg(x.CcCtaNumero),
                                    blActivo = EstadoActivo(x.EcEstado),
                                    blEsNuevo = false
                                };

                                ResolverCuentaConfiguracion(dto);
                                return dto;
                            }).ToList()
                        };

                        return resultado;
                    },
                    intTimeout: 180);
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(ConsultarMantenimientoCostoProductivo),
                    ex);
                throw;
            }
        }

        /// <summary>
        /// Snapshot liviano que consume el motor. Se carga una sola vez por flujo
        /// de costeo y luego se consulta en memoria.
        /// </summary>
        public async Task<ConfiguracionCostoProductivoRuntimeDto>
            ConsultarConfiguracionRuntimeCostoProductivo()
        {
            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async ctx =>
                    {
                        var procesos = await ctx.TbProcesocosto
                            .AsNoTracking()
                            .Where(x => x.PcEstado == "AC")
                            .OrderBy(x => x.PcOrden)
                            .ToListAsync();

                        var tipos = await ctx.TbTipoOtrosProcesos
                            .AsNoTracking()
                            .Where(x => x.ToEstado == "AC")
                            .ToListAsync();

                        var matriz = await ctx.TbConfigTipoLiquidacionProcesoCosto
                            .AsNoTracking()
                            .Where(x => x.CgEstado == "AC")
                            .ToListAsync();

                        Dictionary<string, TbProcesocosto> dicProcesos = procesos
                            .GroupBy(x => NormalizarCfg(x.PcCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                        Dictionary<string, TbTipoOtrosProcesos> dicTipos = tipos
                            .GroupBy(x => NormalizarCfg(x.ToCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                        var aplicaciones = new List<ProcesoCostoAplicacionDto>();

                        foreach (var item in matriz)
                        {
                            string pc = NormalizarCfg(item.CgPcCodigo);
                            string tipo = NormalizarCfg(item.CgToCodigo);

                            TbProcesocosto proceso;
                            TbTipoOtrosProcesos tipoProceso;

                            if (!dicProcesos.TryGetValue(pc, out proceso))
                                continue;

                            if (!dicTipos.TryGetValue(tipo, out tipoProceso))
                                continue;

                            aplicaciones.Add(new ProcesoCostoAplicacionDto
                            {
                                strPcCodigo = pc,
                                strPcNombre = (proceso.PcNombre ?? string.Empty).Trim(),
                                dcTarifa = null,
                                strTipoProcesoCodigo = tipo,
                                strTipoProceso = (tipoProceso.ToNombre ?? string.Empty).Trim(),
                                strConfig = ReglaCostoMantenimientoDto.Normalizar(item.CgConfig)
                            });
                        }

                        return new ConfiguracionCostoProductivoRuntimeDto
                        {
                            lstProcesos = procesos.Select(x => new ProcesoCostoMantenimientoDto
                            {
                                strCodigo = NormalizarCfg(x.PcCodigo),
                                strNombre = (x.PcNombre ?? string.Empty).Trim(),
                                strEtapaCodigo = NormalizarCfg(x.PcEcCodigo),
                                intOrden = x.PcOrden,
                                blActivo = true,
                                blEnMatriz = matriz.Any(m =>
                                    string.Equals(
                                        NormalizarCfg(m.CgPcCodigo),
                                        NormalizarCfg(x.PcCodigo),
                                        StringComparison.OrdinalIgnoreCase))
                            }).ToList(),

                            lstAplicaciones = aplicaciones,

                            lstNiveles = tipos.Select(x => new TipoProcesoNivelRuntimeDto
                            {
                                strCodigo = NormalizarCfg(x.ToCodigo),
                                intNivel = x.ToNivel
                            }).ToList()
                        };
                    },
                    intTimeout: 180);
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(ConsultarConfiguracionRuntimeCostoProductivo),
                    ex);
                throw;
            }
        }

        public async Task<bool> GuardarMantenimientoCostoProductivo(
            GuardarCostoProductivoMantenimientoRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.strUsuario))
                throw new ArgumentException("El usuario es obligatorio.");

            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                    _objCostosFactory,
                    async ctx =>
                    {
                        string usuario = request.strUsuario.Trim();
                        string equipo = Environment.MachineName;
                        DateTime ahora = DateTime.Now;

                        // =====================================================
                        // 1. TIPOS DE PROCESO / NIVEL
                        // =====================================================
                        var existentesTipo = await ctx.TbTipoOtrosProcesos
                            .AsTracking()
                            .ToListAsync();

                        Dictionary<string, TbTipoOtrosProcesos> dicTipo = existentesTipo
                            .GroupBy(x => NormalizarCfg(x.ToCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                        foreach (TipoProcesoMantenimientoDto dto in request.lstTiposProceso ?? new List<TipoProcesoMantenimientoDto>())
                        {
                            ValidarTipoProcesoMantenimiento(dto);

                            string codigo = NormalizarCfg(dto.strCodigo);
                            TbTipoOtrosProcesos entidad;

                            if (dicTipo.TryGetValue(codigo, out entidad))
                            {
                                entidad.ToNombre = dto.strNombre.Trim();
                                entidad.ToTlCodigo = NormalizarCfg(dto.strTipoLoteCodigo);
                                entidad.ToNivel = checked((byte)dto.intNivel);
                                entidad.ToEstado = dto.blActivo ? "AC" : "AN";
                            }
                            else
                            {
                                entidad = new TbTipoOtrosProcesos
                                {
                                    ToCodigo = codigo,
                                    ToNombre = dto.strNombre.Trim(),
                                    ToTlCodigo = NormalizarCfg(dto.strTipoLoteCodigo),
                                    ToNivel = checked((byte)dto.intNivel),
                                    ToEstado = dto.blActivo ? "AC" : "AN",
                                    ToUsuarioCrea = usuario,
                                    ToFechaCrea = ahora,
                                    ToEquipoCrea = equipo
                                };

                                await ctx.TbTipoOtrosProcesos.AddAsync(entidad);
                                dicTipo[codigo] = entidad;
                            }
                        }

                        // Debe guardar primero los tipos nuevos porque la matriz tiene FK.
                        await ctx.SaveChangesAsync();

                        // =====================================================
                        // 2. MATRIZ PROCESO x TIPO LIQUIDACION
                        // =====================================================
                        var existentesMatriz = await ctx.TbConfigTipoLiquidacionProcesoCosto
                            .AsTracking()
                            .ToListAsync();

                        Dictionary<string, TbConfigTipoLiquidacionProcesoCosto> dicMatriz =
                            existentesMatriz
                                .GroupBy(
                                    x => ClaveMatriz(x.CgPcCodigo, x.CgToCodigo),
                                    StringComparer.OrdinalIgnoreCase)
                                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                        foreach (AplicacionCostoMantenimientoDto dto in request.lstMatriz ?? new List<AplicacionCostoMantenimientoDto>())
                        {
                            string pc = NormalizarCfg(dto.strProcesoCodigo);
                            string tipo = NormalizarCfg(dto.strTipoCodigo);
                            string regla = ReglaCostoMantenimientoDto.Normalizar(dto.strRegla);

                            if (pc.Length == 0 || tipo.Length == 0)
                                throw new InvalidOperationException("Proceso y tipo son obligatorios en la matriz.");

                            if (!ReglaCostoMantenimientoDto.EsValida(regla))
                                throw new InvalidOperationException(
                                    "La regla '" + regla + "' no es válida para " + pc + "/" + tipo + ".");

                            string clave = ClaveMatriz(pc, tipo);
                            TbConfigTipoLiquidacionProcesoCosto entidad;

                            if (dicMatriz.TryGetValue(clave, out entidad))
                            {
                                entidad.CgConfig = regla;
                                entidad.CgEstado = dto.blActivo ? "AC" : "AN";
                            }
                            else
                            {
                                entidad = new TbConfigTipoLiquidacionProcesoCosto
                                {
                                    CgPcCodigo = pc,
                                    CgToCodigo = tipo,
                                    CgConfig = regla,
                                    CgEstado = dto.blActivo ? "AC" : "AN",
                                    CgUsuarioCrea = usuario,
                                    CgFechaCrea = ahora,
                                    CgEquipoCrea = equipo
                                };

                                await ctx.TbConfigTipoLiquidacionProcesoCosto.AddAsync(entidad);
                                dicMatriz[clave] = entidad;
                            }
                        }

                        // =====================================================
                        // 3. CUENTAS CONTABLES
                        // =====================================================
                        var existentesCuenta = await ctx.TbConfigCuentaCosto
                            .AsTracking()
                            .ToListAsync();

                        Dictionary<short, TbConfigCuentaCosto> dicCuenta = existentesCuenta
                            .ToDictionary(x => x.CcId, x => x);

                        foreach (CuentaCostoMantenimientoDto dto in request.lstCuentas ?? new List<CuentaCostoMantenimientoDto>())
                        {
                            ValidarCuentaMantenimiento(dto);

                            TbConfigCuentaCosto entidad;

                            if (dto.intId > 0 && dicCuenta.TryGetValue((short)dto.intId, out entidad))
                            {
                                entidad.CcEmpCodigo = checked((short)dto.intEmpresa);
                                entidad.CcEcCodigo = NormalizarCfg(dto.strEtapaCodigo);
                                entidad.CcCenCodigo = NormalizarCfg(dto.strCentroCodigo);
                                entidad.CcSubCodigo = NormalizarCfg(dto.strSubcentroCodigo);
                                entidad.CcCtaNumero = NormalizarCfg(dto.strCuenta);
                                entidad.EcEstado = dto.blActivo ? "AC" : "AN";
                                entidad.EcUsuarioMod = usuario;
                                entidad.EcFechaMod = ahora;
                                entidad.EcEquipoMod = equipo;

                                if (!dto.blActivo)
                                {
                                    entidad.EcUsuarioEli = usuario;
                                    entidad.EcFechaEli = ahora;
                                    entidad.EcEquipoEli = equipo;
                                }
                                else
                                {
                                    entidad.EcUsuarioEli = null;
                                    entidad.EcFechaEli = null;
                                    entidad.EcEquipoEli = null;
                                }
                            }
                            else
                            {
                                entidad = new TbConfigCuentaCosto
                                {
                                    CcEmpCodigo = checked((short)dto.intEmpresa),
                                    CcEcCodigo = NormalizarCfg(dto.strEtapaCodigo),
                                    CcCenCodigo = NormalizarCfg(dto.strCentroCodigo),
                                    CcSubCodigo = NormalizarCfg(dto.strSubcentroCodigo),
                                    CcCtaNumero = NormalizarCfg(dto.strCuenta),
                                    EcEstado = dto.blActivo ? "AC" : "AN",
                                    EcUsuarioCrea = usuario,
                                    EcFechaCrea = ahora,
                                    EcEquipoCrea = equipo
                                };

                                await ctx.TbConfigCuentaCosto.AddAsync(entidad);
                            }
                        }

                        int afectados = await ctx.SaveChangesAsync();

                        _objLogger.LogInformation(
                            "[ConfiguracionCostoProductivo] Guardado EF completado. " +
                            "Tipos={Tipos}; Matriz={Matriz}; Cuentas={Cuentas}; SaveChanges={Afectados}; Usuario={Usuario}",
                            request.lstTiposProceso == null ? 0 : request.lstTiposProceso.Count,
                            request.lstMatriz == null ? 0 : request.lstMatriz.Count,
                            request.lstCuentas == null ? 0 : request.lstCuentas.Count,
                            afectados,
                            usuario);

                        // Si el request trae únicamente valores idénticos a la BD, EF
                        // puede devolver 0. La operación igualmente fue válida.
                        return true;
                    },
                    intTimeout: 180,
                    nivelAislamiento: IsolationLevel.ReadCommitted,
                    blRequiereCommit: true);
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(GuardarMantenimientoCostoProductivo),
                    ex);
                throw;
            }
        }

        private static void ValidarTipoProcesoMantenimiento(TipoProcesoMantenimientoDto dto)
        {
            if (dto == null)
                throw new InvalidOperationException("Existe un tipo de proceso nulo.");

            if (string.IsNullOrWhiteSpace(dto.strCodigo))
                throw new InvalidOperationException("El código del tipo de proceso es obligatorio.");

            if (string.IsNullOrWhiteSpace(dto.strNombre))
                throw new InvalidOperationException("El nombre del tipo de proceso es obligatorio.");

            if (string.IsNullOrWhiteSpace(dto.strTipoLoteCodigo))
                throw new InvalidOperationException("El tipo de lote es obligatorio.");

            if (dto.intNivel < 1 || dto.intNivel > 5)
                throw new InvalidOperationException("El nivel debe estar entre NV1 y NV5.");
        }

        private static void ValidarCuentaMantenimiento(CuentaCostoMantenimientoDto dto)
        {
            if (dto == null)
                throw new InvalidOperationException("Existe una cuenta nula.");

            if (dto.intEmpresa <= 0)
                throw new InvalidOperationException("La empresa debe ser mayor a cero.");

            if (string.IsNullOrWhiteSpace(dto.strEtapaCodigo) ||
                string.IsNullOrWhiteSpace(dto.strCentroCodigo) ||
                string.IsNullOrWhiteSpace(dto.strSubcentroCodigo) ||
                string.IsNullOrWhiteSpace(dto.strCuenta))
            {
                throw new InvalidOperationException(
                    "Etapa, centro, subcentro y cuenta son obligatorios.");
            }
        }

        private static string ClaveMatriz(string pc, string tipo)
        {
            return NormalizarCfg(pc) + "|" + NormalizarCfg(tipo);
        }

        private static string NormalizarCfg(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static bool EstadoActivo(string value)
        {
            return string.Equals(
                (value ?? string.Empty).Trim(),
                "AC",
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Solo previsualiza cómo la configuración será interpretada por el flujo
        /// actual de costo productivo. No persiste proceso/driver porque esas
        /// columnas no existen en tb_configCuentaCosto.
        /// </summary>
        private static void ResolverCuentaConfiguracion(CuentaCostoMantenimientoDto item)
        {
            string etapa = NormalizarCfg(item.strEtapaCodigo);
            string centro = NormalizarCfg(item.strCentroCodigo);
            string sub = NormalizarCfg(item.strSubcentroCodigo);
            string cuenta = NormalizarCfg(item.strCuenta);
            string sufijo = cuenta.Length >= 4 ? cuenta.Substring(cuenta.Length - 4) : cuenta;

            string proceso = string.Empty;

            if (etapa == "MP")
                proceso = "MPE";
            else if (etapa == "CP")
                proceso = "COP";
            else if (etapa == "PP")
            {
                if (centro.StartsWith("504", StringComparison.OrdinalIgnoreCase) || sub == "5030111")
                    proceso = "LOG";
                else if (sub == "5030101")
                    proceso = "REC";
                else if (sub == "5030142")
                    proceso = "COD";
                else
                    proceso = "CLA";
            }
            else if (etapa == "PS")
            {
                if (sub == "5030201") proceso = "DES";
                else if (sub == "5030304") proceso = "HID";
                else if (sub == "5030310") proceso = "COC";
                else if (sub == "5030307") proceso = "DSC";
                else proceso = "PEL";
            }
            else if (etapa == "PR")
            {
                proceso = sub == "5030113" ? "DEC" : "RET";
            }
            else if (etapa == "PC")
            {
                if (_hshFuentesIqfConfiguracion.Contains(cuenta))
                    proceso = "IQF";
                else if (centro.StartsWith("503", StringComparison.OrdinalIgnoreCase))
                    proceso = "BRI";
                else
                    proceso = "TUN";
            }
            else if (etapa == "CD")
            {
                proceso = EsSufijoFijo(sufijo) ? "CDF" : "CDV";
            }
            else if (etapa == "CI")
            {
                proceso = EsSufijoFijo(sufijo) ? "CIF" : "CIV";
            }

            // Regla especial ya existente en el pase: una cuenta física puede
            // alimentar dos procesos productivos.
            if (cuenta == "50301040104" && etapa == "PP")
                proceso = "CLA + CAJ";

            string driver = proceso;
            if (etapa == "MP")
            {
                driver = sub == "5040606" || sub == "5040904"
                    ? "CIV"
                    : "DIRECTO_CUENTA";
            }

            item.strProcesoResuelto = proceso;
            item.strDriverResuelto = driver;
            item.strModoMonto = proceso.IndexOf("IQF", StringComparison.OrdinalIgnoreCase) >= 0
                ? "DEBE_MES"
                : "NETO_MES";
            item.blRequiereCreditoRetiro =
                proceso.IndexOf("IQF", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool EsSufijoFijo(string sufijo)
        {
            return sufijo == "0033" ||
                   sufijo == "0097" ||
                   sufijo == "0239" ||
                   sufijo == "0240";
        }
    }
}
