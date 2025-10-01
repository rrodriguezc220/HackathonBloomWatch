using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using HackathonBloomWatch.ViewModels;
using HackathonBloomWatch.Data;
using HackathonBloomWatch.Models;
using System.Globalization;

namespace HackathonBloomWatch.Controllers
{
    public class CampaniasController : Controller
    {
        private readonly HackathonBWContext _context;

        public CampaniasController(HackathonBWContext context)
        {
            _context = context;
        }

        public async Task<CampaniaPaginadoViewModel> GetCampaniaPaginado(string? busquedaAnio, int paginaActual, int campaniasPorPagina)
        {
            IQueryable<Campania> query = _context.Campania;

            if (!string.IsNullOrEmpty(busquedaAnio))
            {
                query = query.Where(p => p.AnioCampania.Contains(busquedaAnio));
            }

            int totalCampanias = await query.CountAsync();

            int totalPaginas = (int)Math.Ceiling((double)totalCampanias / campaniasPorPagina);

            if (paginaActual < 1)
            {
                paginaActual = 1;
            }
            else if (paginaActual > totalPaginas)
            {
                paginaActual = totalPaginas;
            }

            List<Campania> campanias = new();
            if (totalCampanias > 0)
            {
                campanias = await query
                    .OrderByDescending(p => p.AnioCampania)
                    .Skip(campaniasPorPagina * (paginaActual - 1))
                    .Take(campaniasPorPagina)
                    .ToListAsync();
            }

            var model = new CampaniaPaginadoViewModel
            {
                Campanias = campanias,
                PaginaActual = paginaActual,
                TotalPaginas = totalPaginas,
                BusquedaAnio = busquedaAnio
            };
            return model;
        }

        // GET: Admin/Campanias
        public async Task<IActionResult> Index(string? busquedaAnio, int paginaActual = 1)
        {
            int campaniasPorPagina = 10;

            if (string.IsNullOrEmpty(busquedaAnio))
            {
                busquedaAnio = "";
            }

            var model = await GetCampaniaPaginado(busquedaAnio, paginaActual, campaniasPorPagina);

            return View(model);
        }

        // GET Importación Masiva
        public IActionResult ImportacionMasiva()
        {
            return View();
        }

        // Procesamiento del archivo KML y previsualización de datos
        [HttpPost]
        public async Task<IActionResult> ProcesarArchivo(string geojsonData, Campania campania)
        {
            if (string.IsNullOrEmpty(geojsonData))
            {
                return BadRequest("El archivo no contiene datos válidos.");
            }

            var datosProcesados = await ProcesarGeoJSON(geojsonData, campania);

            return View(datosProcesados);
        }

        public async Task<DatosKmlViewModel> ProcesarGeoJSON(string geojsonData, Campania campania)
        {
            var datosKml = new DatosKmlViewModel
            {
                Campania = campania,
                Especies = new List<EspeciePlantaKml>(),
                Macizos = new List<MacizoKml>(),
                CampaniaDetalles = new List<CampaniaDetalleKml>()
            };

            var especiesProcesadas = new Dictionary<string, EspeciePlantaKml>();
            var macizosProcesados = new Dictionary<string, MacizoKml>();

            var reader = new GeoJsonReader();
            var featureCollection = reader.Read<FeatureCollection>(geojsonData);

            foreach (var feature in featureCollection)
            {
                var nombreEspecie = feature.Attributes["Especie"]?.ToString();
                var departamento = feature.Attributes["Departam"]?.ToString();
                var provincia = feature.Attributes["Provincia"]?.ToString();
                var distrito = feature.Attributes["Distrito"]?.ToString();
                var localidad = feature.Attributes["Localidad"]?.ToString();
                var tipoActividad = "Reforestación";
                var estadoActividad = feature.Attributes.Exists("F_Plantac") ? "Plantación" : "Hoyada";
                var fechaActividad = ParsearFecha((feature.Attributes.Exists("F_Plantac") ? feature.Attributes["F_Plantac"] : feature.Attributes["F_Hoyacion"])?.ToString());
                var coordenadaEste = decimal.TryParse(feature.Attributes["Este_X"]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal este) ? este : 0m;
                var coordenadaNorte = decimal.TryParse(feature.Attributes["Norte_Y"]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal norte) ? norte : 0m;

                decimal areaHectareas = decimal.TryParse(feature.Attributes["Área_ha"]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal area) ? area : 0m;
                var cantidadElementos = Convert.ToInt32(feature.Attributes.Exists("N__Plant") ? feature.Attributes["N__Plant"] : feature.Attributes["N__Hoyos"]);
                var valorMacizoForestal = decimal.TryParse(feature.Attributes["Macizo_f"]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal valorMacizo) ? valorMacizo : 0m;
                var agroforestal = decimal.TryParse(feature.Attributes["Agroforest"]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal agrofores) ? agrofores : 0m;
                var geometry = feature.Geometry;

                // Verificar si la especie ya ha sido agregada para evitar duplicados
                var claveEspecie = nombreEspecie?.Trim().ToLower();

                if (!especiesProcesadas.ContainsKey(claveEspecie))
                {
                    var especie = await _context.EspeciePlanta.FirstOrDefaultAsync(e => e.NombreEspecie.Trim().ToLower() == claveEspecie);

                    var nuevaEspecie = new EspeciePlantaKml
                    {
                        IdEspeciePlanta = especie?.IdEspeciePlanta ?? 0,
                        NombreEspecie = nombreEspecie,
                        ImagenEspecie = especie?.ImagenEspecie,
                        ClaveEspecie = claveEspecie,
                        ExisteEspecie = especie != null
                    };

                    especiesProcesadas[claveEspecie] = nuevaEspecie;

                    datosKml.Especies.Add(nuevaEspecie);
                }


                var claveMacizo = $"{coordenadaEste}-{coordenadaNorte}".Trim();

                if (!macizosProcesados.ContainsKey(claveMacizo))
                {
                    var macizo = await _context.MacizoForestal.FirstOrDefaultAsync(p => p.CoordenadaEste == coordenadaEste && p.CoordenadaNorte == coordenadaNorte);

                    var nuevoMacizo = new MacizoKml
                    {
                        IdMacizoForestal = macizo?.IdMacizoForestal ?? 0,
                        Departamento = departamento,
                        Provincia = provincia,
                        Distrito = distrito,
                        Localidad = localidad,
                        AreaHectareas = areaHectareas,
                        CoordenadaEste = coordenadaEste,
                        CoordenadaNorte = coordenadaNorte,
                        GeoJson = geometry != null ? new GeoJsonWriter().Write(geometry) : null,
                        ClaveMacizo = claveMacizo,
                        ExisteMacizo = macizo != null
                    };
                    macizosProcesados[claveMacizo] = nuevoMacizo;
                    datosKml.Macizos.Add(nuevoMacizo);
                }

                datosKml.CampaniaDetalles.Add(new CampaniaDetalleKml
                {
                    IdCampania = datosKml.Campania.IdCampania,
                    ClaveEspecie = claveEspecie,
                    ClaveMacizo = claveMacizo,
                    IdEspeciePlanta = especiesProcesadas[claveEspecie].IdEspeciePlanta,
                    IdMacizoForestal = macizosProcesados[claveMacizo].IdMacizoForestal,
                    TipoActividad = tipoActividad,
                    EstadoActividad = estadoActividad,
                    FechaActividad = fechaActividad,
                    CantidadElementos = cantidadElementos,
                    ValorMacizoForestal = valorMacizoForestal,
                    Agroforestal = agroforestal
                });
            }

            return datosKml;
        }

        private DateTime? ParsearFecha(string fechaTexto)
        {
            if (string.IsNullOrEmpty(fechaTexto))
                return null; // Devuelve null si la fecha está vacía

            // Si hay un rango de fechas "26/12/2024-20/01/2025", tomamos la primera parte
            var partes = fechaTexto.Split('-');
            string fechaSolo = partes[0].Trim(); // Extrae la primera fecha

            // Definir los formatos posibles
            string[] formatos = { "d/M/yyyy", "dd/MM/yyyy" };

            // Intenta convertir con múltiples formatos
            if (DateTime.TryParseExact(fechaSolo, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaConvertida))
                return fechaConvertida;

            return null; // Si la fecha es inválida, devuelve null en lugar de una fecha incorrecta
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarInformacion([FromBody] DatosKmlViewModel datosKml)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            // lógica para guardar en DB
            try
            {
                // Validaciones opcionales
                if (datosKml == null)
                {
                    return Json(new { success = false, message = "Datos no válidos." });
                }
                // 1. Guardar Campania
                var campania = datosKml.Campania;
                _context.Campania.Add(campania);
                await _context.SaveChangesAsync();
                int idCampania = campania.IdCampania;

                // 2. Guardar Especies nuevas y actualizar Ids
                foreach (var especie in datosKml.Especies)
                {
                    if (!especie.ExisteEspecie)
                    {
                        var nueva = new EspeciePlanta
                        {
                            NombreEspecie = especie.NombreEspecie,
                            ImagenEspecie = especie.ImagenEspecie
                        };

                        _context.EspeciePlanta.Add(nueva);
                        await _context.SaveChangesAsync();

                        especie.IdEspeciePlanta = nueva.IdEspeciePlanta;
                    }
                }

                // 3. Guardar Macizos nuevos y actualizar Ids
                foreach (var macizo in datosKml.Macizos)
                {
                    if (!macizo.ExisteMacizo)
                    {
                        var nuevo = new MacizoForestal
                        {
                            Departamento = macizo.Departamento,
                            Provincia = macizo.Provincia,
                            Distrito = macizo.Distrito,
                            Localidad = macizo.Localidad,
                            AreaHectareas = macizo.AreaHectareas,
                            CoordenadaEste = macizo.CoordenadaEste,
                            CoordenadaNorte = macizo.CoordenadaNorte,
                            Geometria = new GeoJsonReader().Read<Geometry>(macizo.GeoJson)
                        };

                        _context.MacizoForestal.Add(nuevo);
                        await _context.SaveChangesAsync();

                        macizo.IdMacizoForestal = nuevo.IdMacizoForestal;
                    }
                }

                // 4. Guardar CampaniaDetalles (ya tienen los Id correctos)
                foreach (var detalle in datosKml.CampaniaDetalles)
                {
                    // Evita rastrear referencias innecesarias
                    var nueva = new CampaniaDetalle
                    {
                        IdCampania = idCampania,
                        IdEspeciePlanta = datosKml.Especies.FirstOrDefault(e => e.ClaveEspecie == detalle.ClaveEspecie).IdEspeciePlanta,
                        IdMacizoForestal = datosKml.Macizos.FirstOrDefault(p => p.ClaveMacizo == detalle.ClaveMacizo).IdMacizoForestal,
                        TipoActividad = detalle.TipoActividad,
                        EstadoActividad = detalle.EstadoActividad,
                        FechaActividad = detalle.FechaActividad,
                        CantidadElementos = detalle.CantidadElementos,
                        ValorMacizoForestal = detalle.ValorMacizoForestal,
                        Agroforestal = detalle.Agroforestal
                    };

                    _context.CampaniaDetalle.Add(nueva);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Se guardó toda la información correctamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Ocurrió un error: " + ex.Message });
            }
        }

        // GET: Admin/Campanias/Detalle/5
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campania = await _context.Campania
                .Include(c => c.CampaniaDetalles)
                    .ThenInclude(cd => cd.EspeciePlanta)
                .Include(c => c.CampaniaDetalles)
                    .ThenInclude(cd => cd.MacizoForestal)
                .FirstOrDefaultAsync(c => c.IdCampania == id);

            if (campania == null)
            {
                return NotFound();
            }

            return View(campania);
        }

        // GET: Admin/Campanias/Crear
        public IActionResult Crear()
        {
            return View();
        }

        // POST: Admin/Campanias/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Campania campania)
        {
            if (ModelState.IsValid)
            {
                _context.Add(campania);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(campania);
        }

        // GET: Admin/Campanias/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campania = await _context.Campania.FindAsync(id);
            if (campania == null)
            {
                return NotFound();
            }
            return View(campania);
        }

        // POST: Admin/Campanias/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Campania campania)
        {
            if (id != campania.IdCampania)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(campania);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CampaniaExists(campania.IdCampania))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(campania);
        }

        // GET: Admin/Campanias/Eliminar/5
        public async Task<IActionResult> Eliminar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campania = await _context.Campania
                .FirstOrDefaultAsync(m => m.IdCampania == id);
            if (campania == null)
            {
                return NotFound();
            }

            return View(campania);
        }

        // POST: Admin/Campanias/Eliminar/5
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var campania = await _context.Campania.FindAsync(id);
            if (campania != null)
            {
                _context.Campania.Remove(campania);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CampaniaExists(int id)
        {
            return _context.Campania.Any(e => e.IdCampania == id);
        }
    }
}
