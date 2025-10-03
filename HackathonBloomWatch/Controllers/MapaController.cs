using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;
using HackathonBloomWatch.Data;
using HackathonBloomWatch.Models;
using System.Text.Json;

namespace HackathonBloomWatch.Controllers
{
    public class MapaController : Controller
    {
        private readonly HackathonBWContext _context;

        public MapaController(HackathonBWContext context)
        {
            _context = context;
        }

        // GET: SigdracController
        public IActionResult Index()
        {
            ViewBag.Provincias = UbigeoPeru.Provincias.OrderBy(p => p).Select(d => new SelectListItem
            {
                Text = d,
                Value = d
            });

            // últimas 5 campañas
            ViewBag.Campanias = _context.Campania.OrderByDescending(c => c.AnioCampania).Take(5).Select(c => new SelectListItem
            {
                Text = c.AnioCampania,
                Value = c.IdCampania.ToString()
            }).ToList();

            return View();
        }

        public async Task<IActionResult> GetCampaniaGeoJson(string? idCampania, string provincia = "", string distrito = "", string localidad = "")
        {
            Campania campania = new();

            if (string.IsNullOrEmpty(idCampania))
            {
                campania = await _context.Campania
                .OrderByDescending(c => c.IdCampania)
                .FirstOrDefaultAsync();
            }
            else
            {
                campania = await _context.Campania.FindAsync(int.Parse(idCampania));
            }

            var campaniaDetalles = await _context.CampaniaDetalle.Include(cd => cd.EspeciePlanta).Include(cd => cd.MacizoForestal).Where(cd => cd.IdCampania == campania.IdCampania && cd.MacizoForestal.Provincia.Contains(provincia) && cd.MacizoForestal.Distrito.Contains(distrito) && cd.MacizoForestal.Localidad.Contains(localidad)).ToListAsync();

            var geoJsonWriter = new GeoJsonWriter();

            var geoJsonData = new
            {
                type = "FeatureCollection",
                features = campaniaDetalles.Select(cd =>
                {
                    var geometry = cd.MacizoForestal.Geometria;

                    // Asegúrate de tener el SRID correcto, aunque esté bien por precaución
                    if (geometry != null && geometry.SRID != 4326)
                    {
                        geometry.SRID = 4326;
                    }

                    // Serializa la geometría a JSON (string)
                    var geoJson = geometry != null ? geoJsonWriter.Write(geometry) : null;

                    return new
                    {
                        type = "Feature",
                        properties = new
                        {
                            cd.IdCampaniaDetalle,
                            cd.TipoActividad,
                            cd.EstadoActividad,
                            FechaActividad = cd.FechaActividad?.ToString("dd/MM/yyyy"),
                            cd.CantidadElementos,
                            cd.MacizoForestal.Provincia,
                            cd.MacizoForestal.Distrito,
                            cd.MacizoForestal.Localidad,
                            cd.MacizoForestal.AreaHectareas,
                            cd.EspeciePlanta.NombreEspecie,
                            cd.EspeciePlanta.NombreComun,
                            ImagenEspecie = cd.EspeciePlanta.ImagenEspecie != null ? string.Format("data:image/png;base64,{0}", Convert.ToBase64String(cd.EspeciePlanta.ImagenEspecie)) : "",
                        },
                        geometry = geoJson != null ? JsonSerializer.Deserialize<JsonElement>(geoJson) : default
                    };
                }),
                length = campaniaDetalles.Count
            };

            return Json(geoJsonData);
        }

        public JsonResult GetDistritos(string provincia)
        {
            var distritos = UbigeoPeru.Distritos[provincia].OrderBy(d => d);
            return Json(distritos);
        }

        public JsonResult GetLocalidades(string distrito)
        {
            var localidades = UbigeoPeru.Localidades[distrito].OrderBy(l => l);
            return Json(localidades);
        }

        public async Task<IActionResult> GetEstadisticaLugar(int? idCampania, string? nivelLugar, string? nombreLugar)
        {
            // Construir consulta base
            var query = _context.CampaniaDetalle.Include(cd => cd.MacizoForestal).Include(cd => cd.EspeciePlanta).Where(cd => cd.IdCampania == idCampania);

            // Aplicar filtro por lugar si corresponde
            if (nivelLugar == "Provincia")
            {
                query = query.Where(cd => EF.Functions.Collate(cd.MacizoForestal.Provincia, "Latin1_General_CI_AI") == nombreLugar);
            }
            else if (nivelLugar == "Distrito")
            {
                query = query.Where(cd => EF.Functions.Collate(cd.MacizoForestal.Distrito, "Latin1_General_CI_AI") == nombreLugar);
            }

            // Ejecutar consulta
            var campaniaDetalles = await query.ToListAsync();

            // Separar por tipo de actividad una sola vez
            var plantaciones = campaniaDetalles.Where(cd => cd.EstadoActividad == "Plantación").ToList();

            // Agrupar por especie y contar
            var especiesPlantaciones = plantaciones.GroupBy(cd => cd.EspeciePlanta.NombreComun).Select(g => new { Especie = g.Key, Cantidad = g.Sum(cd => cd.CantidadElementos) }).OrderByDescending(g => g.Cantidad).ToList();

            // Preparar datos para Chart.js
            var labelEspeciesPlantaciones = especiesPlantaciones.Select(ep => ep.Especie).ToList();
            var dataPlantaciones = especiesPlantaciones.Select(ep => ep.Cantidad).ToList();

            // Construir objeto de estadística
            var dataEstadisticas = new
            {
                TotalMacizosPlantacion = plantaciones.Count.ToString("N0"),
                CantidadElementosPlantacion = plantaciones.Sum(cd => cd.CantidadElementos)?.ToString("N0"),
                AreaPlantaciones = plantaciones.Sum(cd => cd.MacizoForestal.AreaHectareas)?.ToString("N2"),
                labelEspeciesPlantaciones,
                dataPlantaciones,
            };

            return Json(dataEstadisticas);
        }

    }
}
