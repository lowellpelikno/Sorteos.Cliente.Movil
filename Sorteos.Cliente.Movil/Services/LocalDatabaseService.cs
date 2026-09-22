using System.Globalization;
using SQLite;
using Sorteos.Cliente.Movil.Models;

namespace Sorteos.Cliente.Movil.Services;

public class LocalDatabaseService(string dbPath, IAsignacionNumerosPlantaService asignacionService) : ILocalDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private List<EstatusSorteoItem>? _estatusSorteoCahce;
    private List<EstatusApartadoItem>? _estatusApartadoCache;

    public LocalDatabaseService(IAsignacionNumerosPlantaService asignacionService)
        : this(Path.Combine(FileSystem.AppDataDirectory, "sorteos_local.db3"), asignacionService)
    {
    }

    public async Task InitAsync()
    {
        if (_database != null) return;

        await _semaphore.WaitAsync();
        try
        {
            if (_database != null) return;

            // Asegurar existencia del directorio contenedor
            string? directorio = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directorio) && !Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            if (File.Exists(dbPath))
            {
                FileInfo fileInfo = new(dbPath);
                if (fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                }
            }

            SQLiteOpenFlags flags = SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite;
            SQLiteAsyncConnection connection = new(dbPath, flags);

            // Hardening de conexion (OWASP MASVS-STORAGE)
            try
            {
                // WAL permite lecturas y escrituras simultaneas sin bloquear hilos
                await connection.ExecuteAsync("PRAGMA journal_mode = WAL;");
                await connection.ExecuteAsync("PRAGMA synchronous = NORMAL;");
                await connection.ExecuteAsync("PRAGMA secure_delete = ON;");
                await connection.ExecuteAsync("PRAGMA temp_store = MEMORY;");
                await connection.ExecuteAsync("PRAGMA foreign_keys = ON;");
                await connection.ExecuteAsync("PRAGMA busy_timeout = 5000;");
            }
            catch (SQLiteException)
            {
                // Tolerancia en plataformas con configuraciones SQLite especificas
            }

            // Gestion de versionado de esquema para migraciones incrementales
            try
            {
                int versionActual = await connection.ExecuteScalarAsync<int>("PRAGMA user_version;");
                if (versionActual == 0)
                {
                    await connection.ExecuteAsync("PRAGMA user_version = 1;");
                }
            }
            catch (SQLiteException)
            {
                // Tolerancia en plataformas con configuraciones SQLite especificas
            }

            // Creacion de tablas de dominio
            await connection.CreateTableAsync<DiaSemanaEntity>();
            await connection.CreateTableAsync<SorteoPlantilla>();
            await connection.CreateTableAsync<PremioLocal>();
            await connection.CreateTableAsync<SorteoPremioLocal>();
            await connection.CreateTableAsync<Clientes>();
            await connection.CreateTableAsync<NumeroPlanta>();
            await connection.CreateTableAsync<NumeroReservado>();
            await connection.CreateTableAsync<Numero>();
            await connection.CreateTableAsync<ReservaPremiada>();
            await connection.CreateTableAsync<EstatusSorteoItem>();
            await connection.CreateTableAsync<EstatusApartadoItem>();
            await connection.CreateTableAsync<FolioSecuencia>();

            // Siembra de semillas de datos iniciales
            await SembrarSemillasInternoAsync(connection);

            // Asignacion final atomica: visible unicamente al concluir toda la inicializacion
            _database = connection;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task SembrarSemillasInternoAsync(SQLiteAsyncConnection db)
    {
        // 1. Siembra de Dias de la semana
        int countDias = await db.Table<DiaSemanaEntity>().CountAsync();
        if (countDias == 0)
        {
            await SembrarDiasSemanaInternoConDbAsync(db);
        }

        // 2. Siembra de Estatus de Sorteo
        int countEstatusSorteo = await db.Table<EstatusSorteoItem>().CountAsync();
        if (countEstatusSorteo == 0)
        {
            List<EstatusSorteoItem> semillasSorteo = [
                new EstatusSorteoItem { Id = 1, Nombre = "Creado", Descripcion = "Sorteo creado y activo para venta de números" },
                new EstatusSorteoItem { Id = 2, Nombre = "En Juego", Descripcion = "Sorteo en curso de juego" },
                new EstatusSorteoItem { Id = 3, Nombre = "Finalizado", Descripcion = "Sorteo concluido con premiación registrada" }
            ];
            await db.InsertAllAsync(semillasSorteo);
        }

        // 3. Siembra de Estatus de Apartado
        int countEstatusApartado = await db.Table<EstatusApartadoItem>().CountAsync();
        if (countEstatusApartado == 0)
        {
            List<EstatusApartadoItem> semillasApartado = [
                new EstatusApartadoItem { Id = 1, Nombre = "Apartado", Descripcion = "Número apartado pendiente de confirmación de pago" },
                new EstatusApartadoItem { Id = 2, Nombre = "Pagado", Descripcion = "Número con pago liquidado" },
                new EstatusApartadoItem { Id = 3, Nombre = "Liberado", Descripcion = "Número liberado por cancelación o falta de pago" }
            ];
            await db.InsertAllAsync(semillasApartado);
        }

        // 4. Siembra de Premios Base iniciales si la tabla esta vacia
        int countPremios = await db.Table<PremioLocal>().CountAsync();
        if (countPremios == 0)
        {
            List<PremioLocal> premiosBase = [
                new PremioLocal { Lugar = 1, DescripcionPremio = "1000", EsMonetario = true, DescripcionLarga = "Primer Premio", Activo = true },
                new PremioLocal { Lugar = 2, DescripcionPremio = "500", EsMonetario = true, DescripcionLarga = "Segundo Premio", Activo = true },
                new PremioLocal { Lugar = 3, DescripcionPremio = "250", EsMonetario = true, DescripcionLarga = "Tercer Premio", Activo = true }
            ];
            await db.InsertAllAsync(premiosBase);
        }
    }

    public async Task CerrarConexionAsync()
    {
        if (_database != null)
        {
            await _database.CloseAsync();
            _database = null;
        }
    }

    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (_database == null)
        {
            await InitAsync();
        }
        return _database!;
    }



    #region Dias de la Semana y Estatus

    public async Task SembrarDiasSemanaAsync()
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        await SembrarDiasSemanaInternoConDbAsync(db);
    }

    private async Task SembrarDiasSemanaInternoConDbAsync(SQLiteAsyncConnection db)
    {
        DateTime hoy = DateTime.Today;
        int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime lunes = hoy.AddDays(-1 * diff).Date;

        string[] nombres = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"];
        string[] iniciales = ["L", "M", "M", "J", "V", "S", "D"];

        List<DiaSemanaEntity> dias = new(7);
        for (int i = 0; i < 7; i++)
        {
            dias.Add(new DiaSemanaEntity
            {
                IdDia = i + 1,
                NombreDia = nombres[i],
                InicialDia = iniciales[i],
                Fecha = lunes.AddDays(i)
            });
        }

        await db.RunInTransactionAsync(tran =>
        {
            tran.Execute("DELETE FROM DiaSemanaItem");
            tran.InsertAll(dias);
        });
    }

    public async Task<List<EstatusSorteoItem>> ObtenerEstatusSorteoAsync()
    {
        if (_estatusSorteoCahce != null)
            return _estatusSorteoCahce;

        SQLiteAsyncConnection db = await GetDatabaseAsync();
        _estatusSorteoCahce = await db.Table<EstatusSorteoItem>().OrderBy(e => e.Id).ToListAsync();
        return _estatusSorteoCahce;
    }

    public async Task<List<EstatusApartadoItem>> ObtenerEstatusApartadoAsync()
    {
        if (_estatusApartadoCache != null)
            return _estatusApartadoCache;

        SQLiteAsyncConnection db = await GetDatabaseAsync();
        _estatusApartadoCache = await db.Table<EstatusApartadoItem>().OrderBy(e => e.Id).ToListAsync();
        return _estatusApartadoCache;
    }

    public async Task<List<DiaSemanaItem>> ObtenerDiasSemanaAsync()
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        const string sql = @"
        SELECT 
            d.*,
            EXISTS(
                SELECT 1 
                FROM SorteoPlantilla s 
                WHERE s.IdDiasSorteo = d.IdDia AND s.Activo = 1
            ) AS TieneSorteo
        FROM DiaSemanaItem d
        ORDER BY d.IdDia ASC";

        return await db.QueryAsync<DiaSemanaItem>(sql);
    }

    public async Task GuardarDiasSemanaAsync(List<DiaSemanaItem> dias)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        await db.RunInTransactionAsync(tran =>
        {
            foreach (DiaSemanaItem dia in dias)
            {
                DiaSemanaEntity entity = new()
                {
                    Id = dia.Id,
                    IdDia = dia.IdDia,
                    NombreDia = dia.NombreDia,
                    InicialDia = dia.InicialDia,
                    Fecha = dia.Fecha.Date
                };

                if (entity.Id > 0)
                    tran.Update(entity);
                else
                    tran.Insert(entity);
            }
        });
    }

    public async Task<ResultadoTransicionSemanal> VerificarTransicionSemanalAsync()
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        DateTime hoy = DateTime.Today;
        int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime lunesActual = hoy.AddDays(-1 * diff).Date;

        List<DiaSemanaEntity> diasGuardados = await db.Table<DiaSemanaEntity>().OrderBy(d => d.IdDia).ToListAsync();
        if (diasGuardados.Count == 0)
        {
            await SembrarDiasSemanaInternoConDbAsync(db);
            return ResultadoTransicionSemanal.SemanaActualizadaSinPendientes;
        }

        DateTime lunesGuardado = diasGuardados[0].Fecha.Date;
        if (lunesActual <= lunesGuardado)
        {
            return ResultadoTransicionSemanal.SinCambioDeSemana;
        }

        // La semana guardada ha expirado (puede haber transcurrido una semana, varias semanas o meses).
        List<SorteoPlantilla> sorteosAnteriores = await db.Table<SorteoPlantilla>()
            .Where(s => s.FechaInicio < lunesActual && s.Activo)
            .ToListAsync();

        List<int> idsConcluidos = [];
        List<SorteoPlantilla> sorteosAbiertos = [];

        foreach (SorteoPlantilla s in sorteosAnteriores)
        {
            if (s.IdEstatusSorteo == 3) // Finalizado
            {
                idsConcluidos.Add(s.Id);
            }
            else
            {
                sorteosAbiertos.Add(s);
            }
        }

        // Depuración física atómica de sorteos concluidos de periodos anteriores
        if (idsConcluidos.Count > 0)
        {
            await db.RunInTransactionAsync(tran =>
            {
                foreach (int idSorteo in idsConcluidos)
                {
                    tran.Execute("DELETE FROM SorteoPremioLocal WHERE IdSorteo = ?", idSorteo);
                    tran.Execute("DELETE FROM Numero WHERE SorteoId = ?", idSorteo);
                    tran.Execute("DELETE FROM NumeroReservado WHERE SorteoId = ?", idSorteo);
                    tran.Execute("DELETE FROM ReservaPremiada WHERE IdSorteo = ?", idSorteo);
                    tran.Execute("DELETE FROM SorteoPlantilla WHERE Id = ?", idSorteo);
                }
            });
        }

        if (sorteosAbiertos.Count > 0)
        {
            // Existen sorteos pospuestos o abiertos de periodos anteriores
            return ResultadoTransicionSemanal.RequiereResolucionPendientes;
        }

        // No existen sorteos abiertos: regenerar la semana actual
        await RegenerarDiasSemanaActualInternoAsync(db, lunesActual);
        return ResultadoTransicionSemanal.SemanaActualizadaSinPendientes;
    }

    public async Task<List<DiaSemanaItem>> ObtenerDiasConSorteosPendientesAsync()
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        DateTime hoy = DateTime.Today;
        int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime lunesActual = hoy.AddDays(-1 * diff).Date;

        List<SorteoPlantilla> sorteosPendientes = await db.Table<SorteoPlantilla>()
            .Where(s => s.FechaInicio < lunesActual && s.Activo && s.IdEstatusSorteo != 3)
            .OrderBy(s => s.FechaInicio)
            .ToListAsync();

        if (sorteosPendientes.Count == 0)
        {
            return [];
        }

        List<DiaSemanaEntity> diasGuardados = await db.Table<DiaSemanaEntity>().ToListAsync();
        Dictionary<int, DiaSemanaEntity> dictDias = diasGuardados.ToDictionary(d => d.IdDia);

        List<DiaSemanaItem> resultado = [];
        HashSet<int> diasAgregados = [];

        foreach (SorteoPlantilla sorteo in sorteosPendientes)
        {
            if (diasAgregados.Add(sorteo.IdDiasSorteo))
            {
                if (dictDias.TryGetValue(sorteo.IdDiasSorteo, out DiaSemanaEntity? diaExistente))
                {
                    resultado.Add(new DiaSemanaItem
                    {
                        Id = diaExistente.Id,
                        IdDia = diaExistente.IdDia,
                        NombreDia = diaExistente.NombreDia,
                        InicialDia = diaExistente.InicialDia,
                        Fecha = diaExistente.Fecha,
                        TieneSorteo = true
                    });
                }
                else
                {
                    resultado.Add(new DiaSemanaItem
                    {
                        IdDia = sorteo.IdDiasSorteo,
                        NombreDia = sorteo.FechaInicio.ToString("dddd", new CultureInfo("es-MX")),
                        InicialDia = sorteo.FechaInicio.ToString("dddd", new CultureInfo("es-MX")).Substring(0, 1).ToUpper(),
                        Fecha = sorteo.FechaInicio,
                        TieneSorteo = true
                    });
                }
            }
        }

        return resultado;
    }

    public async Task RegenerarDiasSemanaActualAsync()
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        DateTime hoy = DateTime.Today;
        int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime lunesActual = hoy.AddDays(-1 * diff).Date;
        await RegenerarDiasSemanaActualInternoAsync(db, lunesActual);
    }

    private static async Task RegenerarDiasSemanaActualInternoAsync(SQLiteAsyncConnection db, DateTime lunesActual)
    {
        string[] nombres = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"];
        string[] iniciales = ["L", "M", "M", "J", "V", "S", "D"];

        List<DiaSemanaEntity> dias = new(7);
        for (int i = 0; i < 7; i++)
        {
            dias.Add(new DiaSemanaEntity
            {
                IdDia = i + 1,
                NombreDia = nombres[i],
                InicialDia = iniciales[i],
                Fecha = lunesActual.AddDays(i)
            });
        }

        await db.RunInTransactionAsync(tran =>
        {
            tran.Execute("DELETE FROM DiaSemanaItem");
            tran.InsertAll(dias);
        });
    }

    #endregion

    #region Premios

    public async Task<int> GuardarPremioAsync(PremioLocal premio)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        int resultado = 0;

        await db.RunInTransactionAsync(tran =>
        {
            // Validar si ya existe otro premio con el mismo monto y lugar
            // Excluimos premio.IdPremio para no autoinvalidar la edicion del mismo registro
            const string sqlValidar = @"
            SELECT COUNT(1) 
            FROM PremioLocal 
            WHERE DescripcionPremio = ? 
              AND Lugar = ? 
              AND IdPremio != ?";

            int duplicados = tran.ExecuteScalar<int>(sqlValidar, premio.DescripcionPremio, premio.Lugar, premio.IdPremio);

            if (duplicados > 0)
            {
                resultado = -1; // Duplicado detectado
                return;
            }

            if (premio.IdPremio > 0)
            {
                tran.Update(premio);
                resultado = premio.IdPremio;
            }
            else
            {
                tran.Insert(premio);
                resultado = premio.IdPremio; // sqlite-net auto-puebla el Id autoincremental
            }
        });

        return resultado;
    }

    public async Task<List<PremioLocal>> ObtenerPremiosAsync()
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();

        // Contamos las asignaciones directamente en el motor SQLite
        // Prioriza y ordena por valor numerico si es convertible, si no va al final o alfabetico
        const string sql = @"
        SELECT 
            p.*,
            COUNT(sp.IdSorteoPremio) AS CantidadSorteosAsignados
        FROM PremioLocal p
        LEFT JOIN SorteoPremioLocal sp ON p.IdPremio = sp.IdPremio
        GROUP BY p.IdPremio
        ORDER BY p.Lugar ASC,                 
                CAST(p.DescripcionPremio AS DECIMAL) DESC,
                p.DescripcionPremio COLLATE NOCASE ASC";

        return await db.QueryAsync<PremioLocal>(sql);
    }

    public async Task<PremioLocal?> ObtenerPremioPorIdAsync(int idPremio)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        return await db.Table<PremioLocal>()
            .Where(p => p.IdPremio == idPremio)
            .FirstOrDefaultAsync();
    }

    public async Task<int> EliminarPremioAsync(int idPremio)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();

        int filasEliminadas = 0;

        await db.RunInTransactionAsync(tran =>
        {
            // 1. Validar si tiene sorteos asignados dentro de la misma transaccion
            int asignado = tran.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM SorteoPremioLocal WHERE IdPremio = ?",
                idPremio);

            if (asignado > 0)
            {
                filasEliminadas = 0; // Protegido contra borrado
                return;
            }

            // 2. Borrado seguro
            filasEliminadas = tran.Execute("DELETE FROM PremioLocal WHERE IdPremio = ?", idPremio);
        });

        return filasEliminadas;
    }

    #endregion

    #region Sorteos y Relacion con Premios

    public async Task<string> ObtenerSiguienteFolioSorteoAsync()
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();

        FolioSecuencia? secuencia = await db.Table<FolioSecuencia>()
            .Where(f => f.Clave == "SORTEO")
            .FirstOrDefaultAsync();

        int ultimoSecuencia = secuencia?.UltimoFolio ?? 0;

        int maximoEnSorteos = 0;
        try
        {
            maximoEnSorteos = await db.ExecuteScalarAsync<int>(
                "SELECT COALESCE(MAX(CAST(NumeroDeSorteo AS INTEGER)), 0) FROM SorteoPlantilla WHERE NumeroDeSorteo IS NOT NULL AND TRIM(NumeroDeSorteo) != '';");
        }
        catch (SQLiteException)
        {
            SorteoPlantilla? ultimoSorteo = await db.Table<SorteoPlantilla>()
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (ultimoSorteo != null && int.TryParse(ultimoSorteo.NumeroDeSorteo, out int folioParsed))
            {
                maximoEnSorteos = folioParsed;
            }
        }

        int baseFolio = Math.Max(ultimoSecuencia, maximoEnSorteos);
        int siguiente = baseFolio + 1;

        return siguiente.ToString("D2");
    }

    public async Task ActualizarUltimoFolioSorteoAsync(int folio)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        await db.RunInTransactionAsync(tran =>
        {
            ActualizarUltimoFolioSorteoInterno(tran, folio);
        });
    }

    private static void ActualizarUltimoFolioSorteoInterno(SQLiteConnection tran, int folio)
    {
        FolioSecuencia? secuencia = tran.Table<FolioSecuencia>()
            .Where(f => f.Clave == "SORTEO")
            .FirstOrDefault();

        if (secuencia == null)
        {
            tran.Insert(new FolioSecuencia
            {
                Clave = "SORTEO",
                UltimoFolio = folio,
                FechaActualizacion = DateTime.Now
            });
        }
        else if (folio > secuencia.UltimoFolio)
        {
            secuencia.UltimoFolio = folio;
            secuencia.FechaActualizacion = DateTime.Now;
            tran.Update(secuencia);
        }
    }

    public async Task<int> GuardarSorteoAsync(SorteoPlantilla sorteo, List<int> idsPremiosSeleccionados)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();

        bool esNuevo = sorteo.Id == 0;

        await db.RunInTransactionAsync(tran =>
        {
            if (esNuevo)
            {
                tran.Insert(sorteo);

                // Generar los numeros del sorteo en la misma transaccion
                List<Numero> listaNumeros = new(sorteo.CantidadNumeros);
                for (int i = 1; i <= sorteo.CantidadNumeros; i++)
                {
                    int val = (i == sorteo.CantidadNumeros) ? 0 : i;
                    listaNumeros.Add(new Numero
                    {
                        SorteoId = sorteo.Id,
                        NumeroValor = val,
                        ReservaId = null,
                        Activo = true
                    });
                }
                tran.InsertAll(listaNumeros);
            }
            else
            {
                tran.Update(sorteo);
            }

            // Sincronizacion de tabla pivote N:M de premios
            tran.Execute("DELETE FROM SorteoPremioLocal WHERE IdSorteo = ?", sorteo.Id);

            if (idsPremiosSeleccionados != null && idsPremiosSeleccionados.Count > 0)
            {
                IEnumerable<SorteoPremioLocal> pivotes = idsPremiosSeleccionados
                    .Distinct()
                    .Select(idPremio => new SorteoPremioLocal
                    {
                        IdSorteo = sorteo.Id,
                        IdPremio = idPremio
                    });

                tran.InsertAll(pivotes);
            }

            // Sincronizar secuencia de folio si es valor numerico
            if (!string.IsNullOrWhiteSpace(sorteo.NumeroDeSorteo))
            {
                if (int.TryParse(sorteo.NumeroDeSorteo, out int folioNum))
                {
                    ActualizarUltimoFolioSorteoInterno(tran, folioNum);
                }
                else
                {
                    string digitos = new(sorteo.NumeroDeSorteo.Where(char.IsDigit).ToArray());
                    if (int.TryParse(digitos, out int folioExtraido) && folioExtraido > 0)
                    {
                        ActualizarUltimoFolioSorteoInterno(tran, folioExtraido);
                    }
                }
            }
        });

        if (esNuevo)
        {
            await AplicarNumerosPlantaASorteoAsync(sorteo.Id);
        }

        return sorteo.Id;
    }

    public async Task<List<SorteoPlantilla>> ObtenerSorteosAllAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<SorteoPlantilla>().OrderByDescending(s => s.Id).ToListAsync();
    }

    public async Task<List<SorteoPlantilla>> ObtenerSorteosPorDiaIdAsync(int idDia)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<SorteoPlantilla>()
            .Where(s => s.IdDiasSorteo == idDia && s.Activo)
            .OrderBy(s => s.Descripcion)
            .ToListAsync();
    }

    public async Task<SorteoPlantilla?> ObtenerSorteoPorIdAsync(int idSorteo)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<SorteoPlantilla>()
            .Where(s => s.Id == idSorteo)
            .FirstOrDefaultAsync();
    }

    public async Task<int> EliminarSorteoYPremiosAsync(int idSorteo)
    {
        var db = await GetDatabaseAsync();
        int filasEliminadas = 0;

        await db.RunInTransactionAsync(tran =>
        {
            tran.Execute("DELETE FROM SorteoPremioLocal WHERE IdSorteo = ?", idSorteo);
            tran.Execute("DELETE FROM Numero WHERE SorteoId = ?", idSorteo);
            tran.Execute("DELETE FROM NumeroReservado WHERE SorteoId = ?", idSorteo);
            tran.Execute("DELETE FROM ReservaPremiada WHERE IdSorteo = ?", idSorteo);
            filasEliminadas = tran.Execute("DELETE FROM SorteoPlantilla WHERE Id = ?", idSorteo);
        });
        return filasEliminadas;
    }

    public async Task<List<PremioPorSorteo>> ObtenerPremiosPorSorteoIdAsync(int idSorteo)
    {
        var db = await GetDatabaseAsync();
        string sql = @"
                SELECT 
                    sp.IdSorteoPremio,
                    sp.IdSorteo,
                    p.IdPremio,
                    p.Lugar,
                    p.DescripcionPremio,
                    p.DescripcionLarga,
                    rp.NumeroGanador,
                    COALESCE(nr.Nombre, 'Desierto / No vendido') AS NombreGanador
                FROM SorteoPremioLocal sp
                INNER JOIN PremioLocal p ON sp.IdPremio = p.IdPremio
                LEFT JOIN ReservaPremiada rp ON rp.IdSorteo = sp.IdSorteo AND rp.IdPremio = p.IdPremio
                LEFT JOIN NumeroReservado nr ON rp.IdNumerosReservados = nr.Id
                WHERE sp.IdSorteo = ?
                ORDER BY p.Lugar ASC";

        return await db.QueryAsync<PremioPorSorteo>(sql, idSorteo);
    }

    #endregion

    #region Clientes y Numeros de Planta

    public async Task<int> GuardarClienteAsync(Clientes cliente)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        if (cliente.Id > 0)
        {
            await db.UpdateAsync(cliente);
            return cliente.Id;
        }

        await db.InsertAsync(cliente);
        return cliente.Id;
    }

    public async Task<List<Clientes>> ObtenerClientesAsync()
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        // GROUP_CONCAT concatena los numeros separados por coma directamente en SQLite
        const string sql = @"
        SELECT 
            c.*,
            COALESCE(
                GROUP_CONCAT(
                    CASE 
                        WHEN np.Numero < 10 THEN '0' || np.Numero 
                        ELSE CAST(np.Numero AS TEXT) 
                    END, 
                    ', '
                ), 
                ''
            ) AS NumerosPlantaResumen
        FROM Clientes c
        LEFT JOIN NumeroPlanta np ON c.Id = np.IdCliente AND np.Activo = 1
        GROUP BY c.Id
        ORDER BY c.Nombre ASC";
        return await db.QueryAsync<Clientes>(sql);
    }

    public async Task<List<Clientes>> BuscarClientesAsync(string texto, int limite = 10)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();

        if (string.IsNullOrWhiteSpace(texto))
        {
            const string sqlSinFiltro = @"
            SELECT 
                c.*,
                COALESCE(
                    GROUP_CONCAT(
                        CASE 
                            WHEN np.Numero < 10 THEN '0' || np.Numero 
                            ELSE CAST(np.Numero AS TEXT) 
                        END, 
                        ', '
                    ), 
                    ''
                ) AS NumerosPlantaResumen
            FROM Clientes c
            LEFT JOIN NumeroPlanta np ON c.Id = np.IdCliente AND np.Activo = 1
            WHERE c.Activo = 1
            GROUP BY c.Id
            ORDER BY c.Nombre ASC
            LIMIT ?";

            return await db.QueryAsync<Clientes>(sqlSinFiltro, limite);
        }

        string pattern = $"%{texto}%";
        const string sqlConFiltro = @"
        SELECT 
            c.*,
            COALESCE(
                GROUP_CONCAT(
                    CASE 
                        WHEN np.Numero < 10 THEN '0' || np.Numero 
                        ELSE CAST(np.Numero AS TEXT) 
                    END, 
                    ', '
                ), 
                ''
            ) AS NumerosPlantaResumen
        FROM Clientes c
        LEFT JOIN NumeroPlanta np ON c.Id = np.IdCliente AND np.Activo = 1
        WHERE c.Activo = 1 AND (c.Nombre LIKE ? OR c.ApellidoPaterno LIKE ? OR c.Telefono LIKE ?)
        GROUP BY c.Id
        ORDER BY c.Nombre ASC
        LIMIT ?";

        return await db.QueryAsync<Clientes>(sqlConFiltro, pattern, pattern, pattern, limite);
    }

    public async Task<Clientes?> ObtenerClientePorIdAsync(int idCliente)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        return await db.Table<Clientes>().Where(c => c.Id == idCliente).FirstOrDefaultAsync();
    }

    public async Task<int> EliminarClienteAsync(int idCliente)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        int filasAfectadas = 0;

        await db.RunInTransactionAsync(tran =>
        {
            tran.Execute("DELETE FROM NumeroPlanta WHERE IdCliente = ?", idCliente);
            filasAfectadas = tran.Execute("DELETE FROM Clientes WHERE Id = ?", idCliente);
        });

        return filasAfectadas;
    }

    public async Task<List<NumeroPlanta>> ObtenerNumerosPlantaPorClienteAsync(int idCliente)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        return await db.Table<NumeroPlanta>()
            .Where(np => np.IdCliente == idCliente && np.Activo)
            .OrderBy(np => np.Numero)
            .ToListAsync();
    }

    public async Task<List<NumeroPlanta>> ObtenerTodosNumerosPlantaAsync()
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        return await db.Table<NumeroPlanta>().Where(np => np.Activo).ToListAsync();
    }

    public async Task<bool> ExisteNumeroPlantaAsync(int numero, int idClienteExcluir = 0)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        return await db.Table<NumeroPlanta>()
            .Where(np => np.Numero == numero && np.Activo && np.IdCliente != idClienteExcluir)
            .CountAsync() > 0;
    }

    public async Task GuardarNumerosPlantaClienteAsync(int idCliente, List<int> numeros)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        string resumen = (numeros != null && numeros.Count > 0)
            ? string.Join(", ", numeros.Distinct().OrderBy(n => n).Select(n => n < 10 ? $"0{n}" : $"{n}"))
            : string.Empty;

        await db.RunInTransactionAsync(tran =>
        {
            // 1. Eliminar asignaciones previas
            tran.Execute("DELETE FROM NumeroPlanta WHERE IdCliente = ?", idCliente);

            // 2. Insertar las nuevas si existen
            if (numeros != null && numeros.Count > 0)
            {
                var nuevos = numeros.Distinct().Select(n => new NumeroPlanta
                {
                    IdCliente = idCliente,
                    Numero = n,
                    AceptaCombo = true,
                    Activo = true
                });
                tran.InsertAll(nuevos);
            }

            // 3. Mantener sincronizado el resumen en la tabla Clientes
            tran.Execute("UPDATE Clientes SET NumerosPlantaResumen = ? WHERE Id = ?", resumen, idCliente);
        });
    }

    public async Task<int> EliminarNumeroPlantaAsync(int idNumeroPlanta)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        return await db.DeleteAsync<NumeroPlanta>(idNumeroPlanta);
    }

    #endregion

    #region Reservas y Cuadricula

    public async Task InicializarNumerosSorteoAsync(int sorteoId, int cantidadNumeros)
    {
        var db = await GetDatabaseAsync();
        var existentes = await db.Table<Numero>().Where(n => n.SorteoId == sorteoId).CountAsync();
        if (existentes > 0) return;

        List<Numero> lista = [];
        for (int i = 1; i <= cantidadNumeros; i++)
        {
            int val = (i == cantidadNumeros) ? 0 : i;
            lista.Add(new Numero
            {
                SorteoId = sorteoId,
                NumeroValor = val,
                ReservaId = null,
                Activo = true
            });
        }

        await db.InsertAllAsync(lista);
    }

    public async Task<List<NumeroGridItem>> ObtenerCuadriculaNumerosSorteoAsync(int idSorteo)
    {
        var db = await GetDatabaseAsync();

        const string sql = @"
        SELECT 
            n.NumeroValor,
            PRINTF('%02d', n.NumeroValor) AS NumeroFormateado,
            n.ReservaId,
            nr.ClienteId,
            nr.Nombre AS NombreCliente,
            CASE 
                WHEN rp.NumeroGanador IS NOT NULL THEN 3 -- EstadoNumeroSorteo.Ganador
                WHEN nr.IdEstatus = 2 THEN 2             -- EstadoNumeroSorteo.Pagado
                WHEN nr.IdEstatus = 1 THEN 1             -- EstadoNumeroSorteo.Apartado
                ELSE 0                                   -- EstadoNumeroSorteo.Disponible
            END AS Estado,
            CASE 
                WHEN rp.NumeroGanador IS NOT NULL THEN 'Ganador'
                ELSE NULL 
            END AS BadgeTexto
        FROM Numero n
        LEFT JOIN NumeroReservado nr ON n.ReservaId = nr.Id AND nr.Activo = 1
        LEFT JOIN ReservaPremiada rp ON n.SorteoId = rp.IdSorteo AND n.NumeroValor = rp.NumeroGanador
        WHERE n.SorteoId = ?
        ORDER BY CASE WHEN n.NumeroValor = 0 THEN 99999 ELSE n.NumeroValor END ASC";

        return await db.QueryAsync<NumeroGridItem>(sql, idSorteo);
    }

    public async Task<int> ApartarNumeroAsync(int idSorteo, int idCliente, int numero, decimal costo, string formaPago)
    {
        return await ApartarNumerosMasivoAsync(idSorteo, idCliente, [numero], costo, formaPago);
    }

    public async Task<int> ApartarNumerosMasivoAsync(int idSorteo, int idCliente, List<int> numeros, decimal costo, string formaPago)
    {
        var db = await GetDatabaseAsync();
        

        int reservaId = 0;

        await db.RunInTransactionAsync(tran =>
        {
            var cliente = tran.Find<Clientes>(idCliente);
            string nombreCliente = cliente != null ? cliente.NombreCompleto : "Cliente General";
            var reserva = new NumeroReservado
            {
                SorteoId = idSorteo,
                ClienteId = idCliente,
                IdEstatus = 1,
                Nombre = nombreCliente,
                Costo = costo,
                FormaDePago = formaPago,
                FechaApartado = DateTime.Now,
                Activo = true
            };

            tran.Insert(reserva);
            reservaId = reserva.Id;

            // Actualizacion en lote dentro de la misma transaccion
            foreach (var numVal in numeros)
            {
                tran.Execute("UPDATE Numero SET ReservaId = ? WHERE SorteoId = ? AND NumeroValor = ?",reservaId, idSorteo, numVal);
            }
        });

        return reservaId;
    }

    // Regla de negocio: una llamada a este metodo crea UNA reserva (NumeroReservado).
    // Si el operador aparta numeros del mismo cliente en momentos distintos, se generan
    // registros independientes que pueden liberarse o pagarse por separado sin afectar los demas.
    public async Task<int> ApartarNumerosMasivoAsync(
        int idSorteo, int idCliente, List<int> numeros, decimal costo, string formaPago, string nombre)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        int reservaId = 0;

        await db.RunInTransactionAsync(tran =>
        {
            NumeroReservado reserva = new NumeroReservado
            {
                SorteoId    = idSorteo,
                ClienteId   = idCliente,
                IdEstatus   = 1,
                Nombre      = string.IsNullOrWhiteSpace(nombre) ? "Sin nombre" : nombre.Trim(),
                Costo       = costo,
                FormaDePago = formaPago,
                FechaApartado = DateTime.Now,
                Activo      = true
            };

            tran.Insert(reserva);
            reservaId = reserva.Id;

            for (int i = 0; i < numeros.Count; i++)
            {
                tran.Execute(
                    "UPDATE Numero SET ReservaId = ? WHERE SorteoId = ? AND NumeroValor = ?",
                    reservaId, idSorteo, numeros[i]);
            }
        });

        return reservaId;
    }

    public async Task<int> RegistrarPagoNumerosDirectoAsync(int idSorteo, int idCliente, List<int> numeros, decimal costo, string formaPago, string nombre, string comprobanteUrl = "", int idEstatus = 2)
    {
        var db = await GetDatabaseAsync();
        int reservaId = 0;

        await db.RunInTransactionAsync(tran =>
        {
            NumeroReservado reserva = new NumeroReservado
            {
                SorteoId       = idSorteo,
                ClienteId      = idCliente,
                IdEstatus      = idEstatus,
                Nombre         = string.IsNullOrWhiteSpace(nombre) ? "Sin nombre" : nombre.Trim(),
                Costo          = costo,
                FormaDePago    = formaPago,
                ComprobanteUrl = comprobanteUrl,
                FechaApartado  = DateTime.Now,
                Activo         = true
            };

            tran.Insert(reserva);
            reservaId = reserva.Id;

            for (int i = 0; i < numeros.Count; i++)
            {
                tran.Execute(
                    "UPDATE Numero SET ReservaId = ? WHERE SorteoId = ? AND NumeroValor = ?",
                    reservaId, idSorteo, numeros[i]);
            }
        });

        return reservaId;
    }

    public async Task<int> LiberarNumeroAsync(int idSorteo, int numero)
    {
        var db = await GetDatabaseAsync();
        string sql = "UPDATE Numero SET ReservaId = NULL WHERE SorteoId = ? AND NumeroValor = ?";
        return await db.ExecuteAsync(sql, idSorteo, numero);
    }

    public async Task<int> LiberarReservaCompletaAsync(int idReserva)
    {
        var db = await GetDatabaseAsync();
        int filas = 0;

        await db.RunInTransactionAsync(tran =>
        {
            tran.Execute("UPDATE Numero SET ReservaId = NULL WHERE ReservaId = ?", idReserva);
            filas = tran.Execute("UPDATE NumeroReservado SET IdEstatus = 3, Activo = 0 WHERE Id = ?", idReserva);
        });

        return filas;
    }

    public async Task<int> AlternarEstatusPagoAsync(int idReserva)
    {
        var db = await GetDatabaseAsync();
        var reserva = await db.Table<NumeroReservado>().Where(r => r.Id == idReserva).FirstOrDefaultAsync();
        if (reserva == null) return 0;

        reserva.IdEstatus = reserva.IdEstatus == 2 ? 1 : 2; // Conmuta Pagado/Apartado
        return await db.UpdateAsync(reserva);
    }

    public async Task<int> CambiarEstatusReservaAsync(int idReserva, int idEstatus)
    {
        var db = await GetDatabaseAsync();
        string sql = "UPDATE NumeroReservado SET IdEstatus = ? WHERE Id = ?";
        return await db.ExecuteAsync(sql, idEstatus, idReserva);
    }

    public async Task<int> ActualizarPagoReservaAsync(int idReserva, int idEstatus, string formaPago, string comprobanteUrl)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        const string sql = "UPDATE NumeroReservado SET IdEstatus = ?, FormaDePago = ?, ComprobanteUrl = ? WHERE Id = ?";
        return await db.ExecuteAsync(sql, idEstatus, formaPago, comprobanteUrl, idReserva);
    }

    public async Task<int> RegistrarPagoEfectivoMasivoAsync(List<int> reservaIds)
    {
        if (reservaIds == null || reservaIds.Count == 0) return 0;

        SQLiteAsyncConnection db = await GetDatabaseAsync();
        string idsParam = string.Join(",", reservaIds);
        string sql = $"UPDATE NumeroReservado SET IdEstatus = 2, FormaDePago = 'Efectivo' WHERE Id IN ({idsParam})";
        return await db.ExecuteAsync(sql);
    }

    public async Task AplicarNumerosPlantaASorteoAsync(int idSorteo)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        SorteoPlantilla? sorteo = await ObtenerSorteoPorIdAsync(idSorteo);
        if (sorteo == null) return;

        List<NumeroPlanta> todosPlanta = await ObtenerTodosNumerosPlantaAsync();
        List<Clientes> clientes = await ObtenerClientesAsync();

        if (todosPlanta == null || todosPlanta.Count == 0) return;

        HashSet<int> numerosValidosSorteo = [];
        for (int i = 1; i <= sorteo.CantidadNumeros; i++)
        {
            numerosValidosSorteo.Add((i == sorteo.CantidadNumeros) ? 0 : i);
        }

        Dictionary<int, string> dictClientes = clientes?.ToDictionary(c => c.Id, c => c.NombreCompleto) ?? [];

        Dictionary<int, List<int>> plantasPorCliente = todosPlanta
            .Where(p => p.Activo && numerosValidosSorteo.Contains(p.Numero))
            .GroupBy(p => p.IdCliente)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Numero).Distinct().ToList());

        if (plantasPorCliente.Count == 0) return;

        // 1. Obtener numeros ocupados en una sola consulta
        List<Numero> ocupados = await db.Table<Numero>()
            .Where(n => n.SorteoId == idSorteo && n.ReservaId != null)
            .ToListAsync();

        HashSet<int> setOcupados = ocupados.Select(o => o.NumeroValor).ToHashSet();

        // 2. Persistir todas las reservas en una unica transaccion
        await db.RunInTransactionAsync(tran =>
        {
            if (sorteo.Oportunidades <= 1)
            {
                foreach (KeyValuePair<int, List<int>> kvp in plantasPorCliente)
                {
                    int idCli = kvp.Key;
                    string nombre = dictClientes.GetValueOrDefault(idCli, $"Cliente #{idCli}");

                    foreach (int numVal in kvp.Value)
                    {
                        if (setOcupados.Contains(numVal)) continue;

                        NumeroReservado reserva = new()
                        {
                            SorteoId = idSorteo,
                            ClienteId = idCli,
                            IdEstatus = 1,
                            Nombre = nombre,
                            Costo = sorteo.Costo,
                            FormaDePago = "Número de Planta",
                            FechaApartado = DateTime.Now,
                            Activo = true
                        };

                        tran.Insert(reserva);
                        tran.Execute("UPDATE Numero SET ReservaId = ? WHERE SorteoId = ? AND NumeroValor = ?",
                                     reserva.Id, idSorteo, numVal);
                        setOcupados.Add(numVal);
                    }
                }
            }
            else
            {
                List<List<int>> combosTeoricos = asignacionService.GenerarCombosTeoricos(
                    sorteo.CantidadNumeros, sorteo.Oportunidades);

                foreach (List<int> combo in combosTeoricos)
                {
                    Dictionary<int, List<int>> clientesEnEsteCombo = [];

                    foreach (KeyValuePair<int, List<int>> kvp in plantasPorCliente)
                    {
                        int idCli = kvp.Key;
                        List<int> coincidencias = kvp.Value.Intersect(combo).ToList();
                        if (coincidencias.Count > 0)
                        {
                            clientesEnEsteCombo[idCli] = coincidencias;
                        }
                    }

                    if (clientesEnEsteCombo.Count > 1)
                    {
                        // Colision / Conflicto de intereses: 2 o mas clientes en el mismo combo.
                        // El grupo deja de existir como combo y se disuelve en numeros sueltos individuales.
                        foreach (KeyValuePair<int, List<int>> cKvp in clientesEnEsteCombo)
                        {
                            int idCli = cKvp.Key;
                            string nombre = dictClientes.GetValueOrDefault(idCli, $"Cliente #{idCli}");

                            foreach (int numVal in cKvp.Value)
                            {
                                if (setOcupados.Contains(numVal)) continue;

                                NumeroReservado reserva = new()
                                {
                                    SorteoId = idSorteo,
                                    ClienteId = idCli,
                                    IdEstatus = 1,
                                    Nombre = nombre,
                                    Costo = sorteo.Costo,
                                    FormaDePago = "Número de Planta",
                                    FechaApartado = DateTime.Now,
                                    Activo = true
                                };

                                tran.Insert(reserva);
                                tran.Execute("UPDATE Numero SET ReservaId = ? WHERE SorteoId = ? AND NumeroValor = ?",
                                             reserva.Id, idSorteo, numVal);
                                setOcupados.Add(numVal);
                            }
                        }
                    }
                    else if (clientesEnEsteCombo.Count == 1)
                    {
                        // Cliente unico en el combo: se le otorga el combo completo
                        KeyValuePair<int, List<int>> unico = clientesEnEsteCombo.First();
                        int idCli = unico.Key;
                        string nombre = dictClientes.GetValueOrDefault(idCli, $"Cliente #{idCli}");

                        List<int> comboLibre = combo.Where(n => !setOcupados.Contains(n)).ToList();
                        if (comboLibre.Count > 0)
                        {
                            NumeroReservado reserva = new()
                            {
                                SorteoId = idSorteo,
                                ClienteId = idCli,
                                IdEstatus = 1,
                                Nombre = nombre,
                                Costo = sorteo.Costo,
                                FormaDePago = "Número de Planta",
                                FechaApartado = DateTime.Now,
                                Activo = true
                            };

                            tran.Insert(reserva);
                            foreach (int numVal in comboLibre)
                            {
                                tran.Execute("UPDATE Numero SET ReservaId = ? WHERE SorteoId = ? AND NumeroValor = ?",
                                             reserva.Id, idSorteo, numVal);
                                setOcupados.Add(numVal);
                            }
                        }
                    }
                }
            }
        });
    }

    #endregion

    #region Premiacion

    public async Task<int> RegistrarPremioGanadorAsync(int idSorteo, int idPremio, int idReserva, int numeroGanador)
    {
        var db = await GetDatabaseAsync();
        int idGenerado = 0;

        await db.RunInTransactionAsync(tran =>
        {
            tran.Execute("DELETE FROM ReservaPremiada WHERE IdSorteo = ? AND IdPremio = ?", idSorteo, idPremio);

            var registro = new ReservaPremiada
            {
                IdSorteo = idSorteo,
                IdPremio = idPremio,
                IdNumerosReservados = idReserva,
                NumeroGanador = numeroGanador,
                FechaRegistro = DateTime.Now
            };

            tran.Insert(registro);
            idGenerado = registro.Id;
        });

        return idGenerado;
    }

    public async Task<List<ReservaPremiada>> ObtenerPremiosGanadoresPorSorteoAsync(int idSorteo)
    {
        var db = await GetDatabaseAsync();

        const string sql = @"
        SELECT 
            rp.*,
            p.DescripcionLarga AS DescripcionPremio,
            COALESCE(nr.Nombre, 'Desierto / No vendido') AS NombreGanador,
            c.Telefono AS TelefonoGanador
        FROM ReservaPremiada rp
        INNER JOIN PremioLocal p ON rp.IdPremio = p.IdPremio
        LEFT JOIN NumeroReservado nr ON rp.IdNumerosReservados = nr.Id
        LEFT JOIN Clientes c ON nr.ClienteId = c.Id
        WHERE rp.IdSorteo = ?
        ORDER BY p.Lugar ASC";

        return await db.QueryAsync<ReservaPremiada>(sql, idSorteo);
    }

    public async Task<int> QuitarPremioGanadorAsync(int idReservaPremiada)
    {
        var db = await GetDatabaseAsync();
        return await db.DeleteAsync<ReservaPremiada>(idReservaPremiada);
    }

    public async Task<DetalleNumeroGanadorDto?> ObtenerDetalleNumeroGanadorAsync(int idSorteo, int numero)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();

        const string sql = @"
        SELECT 
            nr.Id AS ReservaId,
            nr.Nombre AS NombreCliente,
            c.Telefono,
            COALESCE(nr.IdEstatus, 0) AS IdEstatus
        FROM Numero n
        LEFT JOIN NumeroReservado nr ON n.ReservaId = nr.Id AND nr.Activo = 1
        LEFT JOIN Clientes c ON nr.ClienteId = c.Id
        WHERE n.SorteoId = ? AND n.NumeroValor = ?
        LIMIT 1";

        List<DetalleNumeroGanadorDto> resultado = await db.QueryAsync<DetalleNumeroGanadorDto>(sql, idSorteo, numero);
        return resultado.Count > 0 ? resultado[0] : null;
    }

    public async Task<int> CerrarSorteoConPremiosAsync(int idSorteo, List<ReservaPremiada> premiosGanadores)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        int filasModificadas = 0;

        await db.RunInTransactionAsync(tran =>
        {
            tran.Execute("DELETE FROM ReservaPremiada WHERE IdSorteo = ?", idSorteo);
            if (premiosGanadores != null && premiosGanadores.Count > 0)
            {
                tran.InsertAll(premiosGanadores);
            }
            filasModificadas = tran.Execute("UPDATE SorteoPlantilla SET IdEstatusSorteo = 3 WHERE Id = ?", idSorteo);
        });

        return filasModificadas;
    }

    #endregion

    public async Task<List<NumeroGridItem>> ObtenerNumerosPaginadosAsync(
        int idSorteo, string estado, int cursor, int limite, string textoBusqueda)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();

        SorteoPlantilla? sorteo = await ObtenerSorteoPorIdAsync(idSorteo);
        if (sorteo == null) return [];

        // Caso 1: Sorteo tradicional de 1 sola oportunidad por boleto
        if (sorteo.Oportunidades <= 1)
        {
            const string sqlIndividual = @"
            SELECT
                n.NumeroValor,
                PRINTF('%02d', n.NumeroValor) AS NumeroFormateado,
                n.ReservaId,
                nr.ClienteId,
                nr.Nombre AS NombreCliente,
                CASE
                    WHEN rp.NumeroGanador IS NOT NULL THEN 3
                    WHEN nr.IdEstatus = 2 THEN 2
                    WHEN nr.IdEstatus = 1 THEN 1
                    ELSE 0
                END AS Estado,
                CASE WHEN rp.NumeroGanador IS NOT NULL THEN 'Ganador' ELSE NULL END AS BadgeTexto
            FROM Numero n
            LEFT JOIN NumeroReservado nr ON n.ReservaId = nr.Id AND nr.Activo = 1
            LEFT JOIN ReservaPremiada rp ON n.SorteoId = rp.IdSorteo AND n.NumeroValor = rp.NumeroGanador
            WHERE n.SorteoId = ?
              AND (
                  (? = 'Disponibles' AND nr.Id IS NULL AND rp.NumeroGanador IS NULL)
               OR (? = 'Apartados'   AND nr.Id IS NOT NULL AND nr.IdEstatus = 1)
               OR (? = 'Pagados'     AND nr.Id IS NOT NULL AND nr.IdEstatus = 2)
               OR (? = 'Ganadores'   AND rp.NumeroGanador IS NOT NULL)
              )
              AND CASE WHEN n.NumeroValor = 0 THEN 99999 ELSE n.NumeroValor END > ?
              AND (? = '' OR (
                  PRINTF('%02d', n.NumeroValor) LIKE '%' || ? || '%'
               OR COALESCE(nr.Nombre, '') LIKE '%' || ? || '%'
              ))
            ORDER BY CASE WHEN n.NumeroValor = 0 THEN 99999 ELSE n.NumeroValor END ASC
            LIMIT ?";

            List<NumeroGridItem> resultadoIndividual = await db.QueryAsync<NumeroGridItem>(
                sqlIndividual,
                idSorteo,
                estado, estado, estado, estado,
                cursor,
                textoBusqueda, textoBusqueda, textoBusqueda,
                limite);

            for (int i = 0; i < resultadoIndividual.Count; i++)
            {
                resultadoIndividual[i].NumerosCombo = [resultadoIndividual[i].NumeroValor];
                resultadoIndividual[i].EsCombo = false;
            }

            return resultadoIndividual;
        }

        // Caso 2: Sorteo con múltiples oportunidades agrupadas en boletos
        const string sqlTodos = @"
        SELECT
            n.NumeroValor,
            PRINTF('%02d', n.NumeroValor) AS NumeroFormateado,
            n.ReservaId,
            nr.ClienteId,
            nr.Nombre AS NombreCliente,
            CASE
                WHEN rp.NumeroGanador IS NOT NULL THEN 3
                WHEN nr.IdEstatus = 2 THEN 2
                WHEN nr.IdEstatus = 1 THEN 1
                ELSE 0
            END AS Estado,
            CASE WHEN rp.NumeroGanador IS NOT NULL THEN 'Ganador' ELSE NULL END AS BadgeTexto
        FROM Numero n
        LEFT JOIN NumeroReservado nr ON n.ReservaId = nr.Id AND nr.Activo = 1
        LEFT JOIN ReservaPremiada rp ON n.SorteoId = rp.IdSorteo AND n.NumeroValor = rp.NumeroGanador
        WHERE n.SorteoId = ?";

        List<NumeroGridItem> todosNumeros = await db.QueryAsync<NumeroGridItem>(sqlTodos, idSorteo);
        Dictionary<int, NumeroGridItem> mapaNumeros = todosNumeros.ToDictionary(n => n.NumeroValor);

        List<List<int>> combosTeoricos = asignacionService.GenerarCombosTeoricos(
            sorteo.CantidadNumeros, sorteo.Oportunidades);

        List<NumeroGridItem> boletosAgrupados = [];
        string busquedaLimpia = (textoBusqueda ?? string.Empty).Trim();

        foreach (List<int> combo in combosTeoricos)
        {
            if (combo.Count == 0) continue;

            List<NumeroGridItem> itemsEnCombo = [];
            for (int i = 0; i < combo.Count; i++)
            {
                if (mapaNumeros.TryGetValue(combo[i], out NumeroGridItem? it))
                {
                    itemsEnCombo.Add(it);
                }
            }

            if (itemsEnCombo.Count == 0) continue;

            // Determinar si el combo se mantiene integro o si ha mutado (disuelto en numeros sueltos)
            // Esta integro si:
            // a) Todos estan disponibles (ReservaId == null)
            // b) Todos estan reservados y comparten exactamente el mismo ReservaId (ReservaId != null)
            bool todosLibres = itemsEnCombo.All(x => !x.ReservaId.HasValue);
            int? primerReserva = itemsEnCombo[0].ReservaId;
            bool todosMismaReserva = primerReserva.HasValue && itemsEnCombo.All(x => x.ReservaId == primerReserva);

            bool comboIntegro = todosLibres || todosMismaReserva;

            if (comboIntegro)
            {
                // Combo integro (Disponible o Apartado/Pagado en conjunto)
                EstadoNumeroSorteo estadoConsolidado = EstadoNumeroSorteo.Disponible;
                string badgeTexto = string.Empty;
                int? reservaId = null;
                int? clienteId = null;
                string nombreCliente = string.Empty;

                for (int i = 0; i < itemsEnCombo.Count; i++)
                {
                    NumeroGridItem numItem = itemsEnCombo[i];
                    if (numItem.Estado == EstadoNumeroSorteo.Ganador)
                    {
                        estadoConsolidado = EstadoNumeroSorteo.Ganador;
                        badgeTexto = "Ganador";
                    }
                    else if (numItem.Estado == EstadoNumeroSorteo.Pagado && estadoConsolidado != EstadoNumeroSorteo.Ganador)
                    {
                        estadoConsolidado = EstadoNumeroSorteo.Pagado;
                    }
                    else if (numItem.Estado == EstadoNumeroSorteo.Apartado && estadoConsolidado == EstadoNumeroSorteo.Disponible)
                    {
                        estadoConsolidado = EstadoNumeroSorteo.Apartado;
                    }

                    if (!reservaId.HasValue && numItem.ReservaId.HasValue)
                    {
                        reservaId = numItem.ReservaId;
                        clienteId = numItem.ClienteId;
                        nombreCliente = numItem.NombreCliente;
                    }
                }

                // Evaluar filtro de estado
                bool cumpleFiltroEstado = estado switch
                {
                    "Disponibles" => estadoConsolidado == EstadoNumeroSorteo.Disponible,
                    "Apartados"   => estadoConsolidado == EstadoNumeroSorteo.Apartado,
                    "Pagados"     => estadoConsolidado == EstadoNumeroSorteo.Pagado,
                    "Ganadores"   => estadoConsolidado == EstadoNumeroSorteo.Ganador,
                    _             => true
                };

                if (!cumpleFiltroEstado) continue;

                int numeroRepresentativo = combo[0];
                int valorParaOrden = numeroRepresentativo == 0 ? 99999 : numeroRepresentativo;
                if (valorParaOrden <= cursor) continue;

                string numeroFormateado = string.Join(" - ", combo.Select(v => $"{v:D2}"));

                if (!string.IsNullOrWhiteSpace(busquedaLimpia))
                {
                    bool coincideNumero = numeroFormateado.Contains(busquedaLimpia, StringComparison.OrdinalIgnoreCase);
                    bool coincideComboItem = combo.Exists(v =>
                        $"{v:D2}".Contains(busquedaLimpia, StringComparison.OrdinalIgnoreCase) ||
                        v.ToString().Contains(busquedaLimpia, StringComparison.OrdinalIgnoreCase));
                    bool coincideCliente = !string.IsNullOrWhiteSpace(nombreCliente) &&
                        nombreCliente.Contains(busquedaLimpia, StringComparison.OrdinalIgnoreCase);

                    if (!coincideNumero && !coincideComboItem && !coincideCliente)
                        continue;
                }

                boletosAgrupados.Add(new NumeroGridItem
                {
                    NumeroValor = numeroRepresentativo,
                    NumeroFormateado = numeroFormateado,
                    ReservaId = reservaId,
                    ClienteId = clienteId,
                    NombreCliente = nombreCliente,
                    Estado = estadoConsolidado,
                    EsCombo = combo.Count > 1,
                    TextoCombo = combo.Count == 1 ? "1 oportunidad" : $"{combo.Count} oportunidades",
                    NumerosCombo = [.. combo],
                    BadgeTexto = badgeTexto
                });
            }
            else
            {
                // El grupo dejo de existir debido a colision o mutacion.
                // Cada numero se emite de manera individual / suelta.
                for (int i = 0; i < itemsEnCombo.Count; i++)
                {
                    NumeroGridItem itemSuelto = itemsEnCombo[i];

                    bool cumpleFiltroEstado = estado switch
                    {
                        "Disponibles" => itemSuelto.Estado == EstadoNumeroSorteo.Disponible,
                        "Apartados"   => itemSuelto.Estado == EstadoNumeroSorteo.Apartado,
                        "Pagados"     => itemSuelto.Estado == EstadoNumeroSorteo.Pagado,
                        "Ganadores"   => itemSuelto.Estado == EstadoNumeroSorteo.Ganador,
                        _             => true
                    };

                    if (!cumpleFiltroEstado) continue;

                    int valorParaOrden = itemSuelto.NumeroValor == 0 ? 99999 : itemSuelto.NumeroValor;
                    if (valorParaOrden <= cursor) continue;

                    string formatted = $"{itemSuelto.NumeroValor:D2}";

                    if (!string.IsNullOrWhiteSpace(busquedaLimpia))
                    {
                        bool coincideNumero = formatted.Contains(busquedaLimpia, StringComparison.OrdinalIgnoreCase) ||
                                              itemSuelto.NumeroValor.ToString().Contains(busquedaLimpia, StringComparison.OrdinalIgnoreCase);
                        bool coincideCliente = !string.IsNullOrWhiteSpace(itemSuelto.NombreCliente) &&
                            itemSuelto.NombreCliente.Contains(busquedaLimpia, StringComparison.OrdinalIgnoreCase);

                        if (!coincideNumero && !coincideCliente)
                            continue;
                    }

                    boletosAgrupados.Add(new NumeroGridItem
                    {
                        NumeroValor = itemSuelto.NumeroValor,
                        NumeroFormateado = formatted,
                        ReservaId = itemSuelto.ReservaId,
                        ClienteId = itemSuelto.ClienteId,
                        NombreCliente = itemSuelto.NombreCliente,
                        Estado = itemSuelto.Estado,
                        EsCombo = false,
                        TextoCombo = string.Empty,
                        NumerosCombo = [itemSuelto.NumeroValor],
                        BadgeTexto = itemSuelto.BadgeTexto
                    });
                }
            }
        }

        return boletosAgrupados
            .OrderBy(b => b.NumeroValor == 0 ? 99999 : b.NumeroValor)
            .Take(limite)
            .ToList();
    }

    public async Task<List<string>> ObtenerNumerosDisponiblesFormateadosAsync(int idSorteo)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();

        SorteoPlantilla? sorteo = await ObtenerSorteoPorIdAsync(idSorteo);
        if (sorteo == null) return [];

        if (sorteo.Oportunidades <= 1)
        {
            const string sql = @"
            SELECT PRINTF('%02d', n.NumeroValor) AS NumeroFormateado
            FROM Numero n
            LEFT JOIN NumeroReservado nr ON n.ReservaId = nr.Id AND nr.Activo = 1
            LEFT JOIN ReservaPremiada rp ON n.SorteoId = rp.IdSorteo AND n.NumeroValor = rp.NumeroGanador
            WHERE n.SorteoId = ?
              AND nr.Id IS NULL
              AND rp.NumeroGanador IS NULL
            ORDER BY CASE WHEN n.NumeroValor = 0 THEN 99999 ELSE n.NumeroValor END ASC";

            List<NumeroGridItem> items = await db.QueryAsync<NumeroGridItem>(sql, idSorteo);
            List<string> resultado = [];
            for (int i = 0; i < items.Count; i++)
            {
                resultado.Add(items[i].NumeroFormateado);
            }
            return resultado;
        }

        const string sqlTodos = @"
        SELECT
            n.NumeroValor,
            PRINTF('%02d', n.NumeroValor) AS NumeroFormateado,
            n.ReservaId,
            CASE
                WHEN rp.NumeroGanador IS NOT NULL THEN 3
                WHEN nr.IdEstatus = 2 THEN 2
                WHEN nr.IdEstatus = 1 THEN 1
                ELSE 0
            END AS Estado
        FROM Numero n
        LEFT JOIN NumeroReservado nr ON n.ReservaId = nr.Id AND nr.Activo = 1
        LEFT JOIN ReservaPremiada rp ON n.SorteoId = rp.IdSorteo AND n.NumeroValor = rp.NumeroGanador
        WHERE n.SorteoId = ?";

        List<NumeroGridItem> todosNumeros = await db.QueryAsync<NumeroGridItem>(sqlTodos, idSorteo);
        Dictionary<int, NumeroGridItem> mapaNumeros = todosNumeros.ToDictionary(n => n.NumeroValor);

        List<List<int>> combosTeoricos = asignacionService.GenerarCombosTeoricos(
            sorteo.CantidadNumeros, sorteo.Oportunidades);

        List<(int orden, string texto)> listaOrdenada = [];

        foreach (List<int> combo in combosTeoricos)
        {
            if (combo.Count == 0) continue;

            List<NumeroGridItem> itemsEnCombo = [];
            for (int i = 0; i < combo.Count; i++)
            {
                if (mapaNumeros.TryGetValue(combo[i], out NumeroGridItem? it))
                {
                    itemsEnCombo.Add(it);
                }
            }

            if (itemsEnCombo.Count == 0) continue;

            bool todosLibres = itemsEnCombo.All(x => !x.ReservaId.HasValue);
            int? primerReserva = itemsEnCombo[0].ReservaId;
            bool todosMismaReserva = primerReserva.HasValue && itemsEnCombo.All(x => x.ReservaId == primerReserva);
            bool comboIntegro = todosLibres || todosMismaReserva;

            if (comboIntegro)
            {
                if (todosLibres)
                {
                    int orden = combo[0] == 0 ? 99999 : combo[0];
                    string formato = string.Join("-", combo.Select(v => $"{v:D2}"));
                    listaOrdenada.Add((orden, formato));
                }
            }
            else
            {
                for (int i = 0; i < itemsEnCombo.Count; i++)
                {
                    NumeroGridItem itemSuelto = itemsEnCombo[i];
                    if (itemSuelto.Estado == EstadoNumeroSorteo.Disponible)
                    {
                        int orden = itemSuelto.NumeroValor == 0 ? 99999 : itemSuelto.NumeroValor;
                        listaOrdenada.Add((orden, itemSuelto.NumeroFormateado));
                    }
                }
            }
        }

        return listaOrdenada.OrderBy(x => x.orden).Select(x => x.texto).ToList();
    }

    public async Task<MetricasSorteoDto> ObtenerMetricasSorteoAsync(int idSorteo)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();

        SorteoPlantilla? sorteo = await ObtenerSorteoPorIdAsync(idSorteo);
        if (sorteo == null) return new MetricasSorteoDto();

        if (sorteo.Oportunidades <= 1)
        {
            const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM Numero WHERE SorteoId = ?) AS TotalNumeros,
                (SELECT COUNT(*) FROM Numero n2
                    LEFT JOIN NumeroReservado nr2 ON n2.ReservaId = nr2.Id AND nr2.Activo = 1
                    LEFT JOIN ReservaPremiada rp2 ON n2.SorteoId = rp2.IdSorteo AND n2.NumeroValor = rp2.NumeroGanador
                    WHERE n2.SorteoId = ? AND nr2.Id IS NULL AND rp2.NumeroGanador IS NULL
                ) AS TotalDisponibles,
                (SELECT COUNT(*) FROM NumeroReservado WHERE SorteoId = ? AND IdEstatus = 1 AND Activo = 1) AS TotalApartados,
                (SELECT COUNT(*) FROM NumeroReservado WHERE SorteoId = ? AND IdEstatus = 2 AND Activo = 1) AS TotalPagados,
                (SELECT COUNT(*) FROM ReservaPremiada WHERE IdSorteo = ?) AS TotalGanadores";

            List<MetricasSorteoDto> resultado = await db.QueryAsync<MetricasSorteoDto>(
                sql, idSorteo, idSorteo, idSorteo, idSorteo, idSorteo);

            return resultado.Count > 0 ? resultado[0] : new MetricasSorteoDto();
        }

        const string sqlTodos = @"
        SELECT
            n.NumeroValor,
            n.ReservaId,
            COALESCE(nr.IdEstatus, 0) AS IdEstatus,
            CASE WHEN rp.NumeroGanador IS NOT NULL THEN 1 ELSE 0 END AS EsGanador
        FROM Numero n
        LEFT JOIN NumeroReservado nr ON n.ReservaId = nr.Id AND nr.Activo = 1
        LEFT JOIN ReservaPremiada rp ON n.SorteoId = rp.IdSorteo AND n.NumeroValor = rp.NumeroGanador
        WHERE n.SorteoId = ?";

        List<RawNumeroEstado> todos = await db.QueryAsync<RawNumeroEstado>(sqlTodos, idSorteo);
        Dictionary<int, RawNumeroEstado> mapa = todos.ToDictionary(n => n.NumeroValor);

        List<List<int>> combosTeoricos = asignacionService.GenerarCombosTeoricos(
            sorteo.CantidadNumeros, sorteo.Oportunidades);

        int disponibles = 0;
        int apartados = 0;
        int pagados = 0;
        int ganadores = 0;

        foreach (List<int> combo in combosTeoricos)
        {
            if (combo.Count == 0) continue;

            List<RawNumeroEstado> itemsEnCombo = [];
            for (int i = 0; i < combo.Count; i++)
            {
                if (mapa.TryGetValue(combo[i], out RawNumeroEstado? it))
                {
                    itemsEnCombo.Add(it);
                }
            }

            if (itemsEnCombo.Count == 0) continue;

            bool todosLibres = true;
            for (int i = 0; i < itemsEnCombo.Count; i++)
            {
                if (itemsEnCombo[i].ReservaId.HasValue || itemsEnCombo[i].EsGanador)
                {
                    todosLibres = false;
                    break;
                }
            }

            int? primerReserva = itemsEnCombo[0].ReservaId;
            bool todosMismaReserva = primerReserva.HasValue;
            if (todosMismaReserva)
            {
                for (int i = 0; i < itemsEnCombo.Count; i++)
                {
                    if (itemsEnCombo[i].ReservaId != primerReserva)
                    {
                        todosMismaReserva = false;
                        break;
                    }
                }
            }

            bool comboIntegro = todosLibres || todosMismaReserva;

            if (comboIntegro)
            {
                bool tieneGanador = false;
                for (int i = 0; i < itemsEnCombo.Count; i++)
                {
                    if (itemsEnCombo[i].EsGanador)
                    {
                        tieneGanador = true;
                        break;
                    }
                }

                if (tieneGanador)
                {
                    ganadores++;
                }
                else if (primerReserva.HasValue)
                {
                    int estatus = itemsEnCombo[0].IdEstatus;
                    if (estatus == 2) pagados++;
                    else apartados++;
                }
                else
                {
                    disponibles++;
                }
            }
            else
            {
                for (int i = 0; i < itemsEnCombo.Count; i++)
                {
                    RawNumeroEstado item = itemsEnCombo[i];
                    if (item.EsGanador)
                    {
                        ganadores++;
                    }
                    else if (item.ReservaId.HasValue)
                    {
                        if (item.IdEstatus == 2) pagados++;
                        else apartados++;
                    }
                    else
                    {
                        disponibles++;
                    }
                }
            }
        }

        int total = disponibles + apartados + pagados + ganadores;
        return new MetricasSorteoDto
        {
            TotalNumeros = total,
            TotalDisponibles = disponibles,
            TotalApartados = apartados,
            TotalPagados = pagados,
            TotalGanadores = ganadores
        };
    }

    public async Task<List<ClienteConApartadosDto>> ObtenerClientesConApartadosAsync(int idSorteo)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();

        const string sql = @"
        SELECT
            nr.Id AS ReservaId,
            nr.IdEstatus,
            COALESCE(TRIM(nr.Nombre), 'Sin nombre') AS NombreCliente,
            GROUP_CONCAT(PRINTF('%02d', n.NumeroValor), ', ') AS NumerosTexto,
            COUNT(*) AS TotalNumeros,
            MIN(n.NumeroValor) AS PrimerNumero,
            COALESCE(nr.FormaDePago, 'Efectivo') AS FormaDePago,
            COALESCE(nr.ComprobanteUrl, '') AS ComprobanteUrl
        FROM NumeroReservado nr
        INNER JOIN Numero n ON n.ReservaId = nr.Id AND n.SorteoId = ?
        WHERE nr.SorteoId = ? AND nr.IdEstatus = 1 AND nr.Activo = 1
        GROUP BY nr.Id
        ORDER BY nr.Nombre ASC";

        return await db.QueryAsync<ClienteConApartadosDto>(sql, idSorteo, idSorteo);
    }

    public async Task<List<ClienteConApartadosDto>> ObtenerClientesPorEstatusAsync(int idSorteo, int idEstatus)
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();

        const string sql = @"
        SELECT
            nr.Id AS ReservaId,
            nr.IdEstatus,
            COALESCE(TRIM(nr.Nombre), 'Sin nombre') AS NombreCliente,
            GROUP_CONCAT(PRINTF('%02d', n.NumeroValor), ', ') AS NumerosTexto,
            COUNT(*) AS TotalNumeros,
            MIN(n.NumeroValor) AS PrimerNumero,
            COALESCE(nr.FormaDePago, 'Efectivo') AS FormaDePago,
            COALESCE(nr.ComprobanteUrl, '') AS ComprobanteUrl,
            MAX(CASE WHEN rp.Id IS NOT NULL THEN 1 ELSE 0 END) AS EsGanador,
            COALESCE(MAX(rp.NumeroGanador), 0) AS NumeroGanador,
            COALESCE(MAX(p.Lugar || 'º Lugar'), '') AS PremioGanadoTexto
        FROM NumeroReservado nr
        INNER JOIN Numero n ON n.ReservaId = nr.Id AND n.SorteoId = ?
        LEFT JOIN ReservaPremiada rp ON rp.IdSorteo = nr.SorteoId AND (rp.IdNumerosReservados = nr.Id OR rp.NumeroGanador = n.NumeroValor)
        LEFT JOIN PremioLocal p ON rp.IdPremio = p.IdPremio
        WHERE nr.SorteoId = ? AND nr.IdEstatus = ? AND nr.Activo = 1
        GROUP BY nr.Id
        ORDER BY EsGanador DESC, nr.Nombre ASC";

        return await db.QueryAsync<ClienteConApartadosDto>(sql, idSorteo, idSorteo, idEstatus);
    }

    #region Dashboard Ejecutivo

    public async Task<DashboardResumenDto> ObtenerResumenDashboardAsync()
    {
        SQLiteAsyncConnection db = await GetDatabaseAsync();
        DashboardResumenDto resumen = new();

        // 1. Totales de catalogos con SQL directo
        resumen.TotalClientes = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Clientes WHERE Activo = 1");
        resumen.TotalPremios = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PremioLocal WHERE Activo = 1");
        resumen.TotalSorteosActivos = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SorteoPlantilla WHERE Activo = 1 AND IdEstatusSorteo != 3");

        // 2. Dia actual segun calendario de la semana (1 = Lunes, ..., 7 = Domingo)
        DateTime hoy = DateTime.Today;
        int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
        int idDiaHoy = diff + 1;

        resumen.SorteosHoy = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM SorteoPlantilla WHERE Activo = 1 AND IdDiasSorteo = ?", idDiaHoy);

        // 3. Metricas financieras y apartados pendientes
        const string sqlFinanzas = @"
            SELECT
                COALESCE(SUM(CASE WHEN IdEstatus = 2 THEN Costo ELSE 0 END), 0) AS MontoPagado,
                COALESCE(SUM(CASE WHEN IdEstatus = 1 THEN Costo ELSE 0 END), 0) AS MontoApartado,
                COALESCE(SUM(CASE WHEN IdEstatus = 1 THEN 1 ELSE 0 END), 0) AS ApartadosPendientes
            FROM NumeroReservado
            WHERE Activo = 1";

        List<MetricasFinancierasDashboardRaw> finanzas = await db.QueryAsync<MetricasFinancierasDashboardRaw>(sqlFinanzas);
        if (finanzas.Count > 0)
        {
            resumen.TotalMontoPagado = finanzas[0].MontoPagado;
            resumen.TotalMontoApartado = finanzas[0].MontoApartado;
            resumen.TotalApartadosPendientes = finanzas[0].ApartadosPendientes;
        }

        // 4. Inventario de boletos globales
        resumen.TotalBoletosEmitidos = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Numero WHERE Activo = 1");
        const string sqlBoletosVendidos = @"
            SELECT COUNT(*) 
            FROM Numero n
            INNER JOIN NumeroReservado nr ON n.ReservaId = nr.Id
            WHERE nr.Activo = 1 AND nr.IdEstatus IN (1, 2)";
        resumen.TotalBoletosVendidos = await db.ExecuteScalarAsync<int>(sqlBoletosVendidos);

        // 5. Determinar Sorteo Destacado
        List<SorteoPlantilla> sorteosActivos = await db.QueryAsync<SorteoPlantilla>(
            "SELECT * FROM SorteoPlantilla WHERE Activo = 1 AND IdEstatusSorteo != 3");

        SorteoPlantilla? destacado = sorteosActivos.FirstOrDefault(s => s.IdDiasSorteo == idDiaHoy)
            ?? sorteosActivos.Where(s => s.IdDiasSorteo > idDiaHoy).OrderBy(s => s.IdDiasSorteo).FirstOrDefault()
            ?? sorteosActivos.OrderBy(s => s.IdDiasSorteo).FirstOrDefault();

        if (destacado == null && sorteosActivos.Count == 0)
        {
            destacado = await db.Table<SorteoPlantilla>().OrderByDescending(s => s.Id).FirstOrDefaultAsync();
        }

        if (destacado != null)
        {
            resumen.TieneSorteoDestacado = true;
            resumen.SorteoDestacadoId = destacado.Id;
            resumen.SorteoDestacadoNumero = destacado.NumeroDeSorteo;
            resumen.SorteoDestacadoDescripcion = destacado.Descripcion;
            resumen.SorteoDestacadoCosto = destacado.Costo;
            resumen.SorteoDestacadoFechaFin = destacado.FechaFin;
            resumen.SorteoDestacadoBoletosTotal = destacado.CantidadEmisiones;

            MetricasSorteoDto metricasDestacado = await ObtenerMetricasSorteoAsync(destacado.Id);
            resumen.SorteoDestacadoBoletosPagados = metricasDestacado.TotalPagados;
            resumen.SorteoDestacadoBoletosApartados = metricasDestacado.TotalApartados;

            List<PremioPorSorteo> premios = await ObtenerPremiosPorSorteoIdAsync(destacado.Id);
            resumen.SorteoDestacadoPremiosResumen = premios.Count > 0
                ? string.Join(" • ", premios.Select(p => $"{p.Lugar}º {p.DescripcionPremio}"))
                : "Sin premios configurados";
        }

        return resumen;
    }

    private sealed class RawNumeroEstado
    {
        public int NumeroValor { get; set; }
        public int? ReservaId { get; set; }
        public int IdEstatus { get; set; }
        public bool EsGanador { get; set; }
    }

    private sealed class MetricasFinancierasDashboardRaw
    {
        public decimal MontoPagado { get; set; }
        public decimal MontoApartado { get; set; }
        public int ApartadosPendientes { get; set; }
    }

    #endregion
}
