using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;
using HackathonBloomWatch.ViewModels;
using HackathonBloomWatch.Data;
using HackathonBloomWatch.Models;

namespace HackathonBloomWatch.Controllers
{
    public class MacizosForestalesController : Controller
    {
        private readonly HackathonBWContext _context;

        public MacizosForestalesController(HackathonBWContext context)
        {
            _context = context;
        }

        public async Task<MacizoPaginadoViewModel> GetMacizoPaginado(string? busquedaProvincia, string? busquedaDistrito, string? busquedaLocalidad, int paginaActual, int macizosPorPagina)
        {
            IQueryable<MacizoForestal> query = _context.MacizoForestal;

            if (!string.IsNullOrEmpty(busquedaProvincia))
            {
                query = query.Where(p => p.Provincia.Contains(busquedaProvincia));
            }
            if (!string.IsNullOrEmpty(busquedaDistrito))
            {
                query = query.Where(p => p.Distrito.Contains(busquedaDistrito));
            }
            if (!string.IsNullOrEmpty(busquedaLocalidad))
            {
                query = query.Where(p => p.Localidad.Contains(busquedaLocalidad));
            }

            int totalParcelas = await query.CountAsync();

            int totalPaginas = (int)Math.Ceiling((double)totalParcelas / macizosPorPagina);

            if (paginaActual < 1)
            {
                paginaActual = 1;
            }
            else if (paginaActual > totalPaginas)
            {
                paginaActual = totalPaginas;
            }

            List<MacizoForestal> macizos = new();
            if (totalParcelas > 0)
            {
                macizos = await query
                    .OrderBy(p => p.Provincia)
                    .Skip(macizosPorPagina * (paginaActual - 1))
                    .Take(macizosPorPagina)
                    .ToListAsync();
            }

            var model = new MacizoPaginadoViewModel
            {
                Macizos = macizos,
                PaginaActual = paginaActual,
                TotalPaginas = totalPaginas,
                BusquedaProvincia = busquedaProvincia,
                BusquedaDistrito = busquedaDistrito,
                BusquedaLocalidad = busquedaLocalidad
            };
            return model;
        }

        // GET: Admin/MacizosForestales
        public async Task<IActionResult> Index(string? busquedaProvincia, string? busquedaDistrito, string? busquedaLocalidad, int paginaActual = 1)
        {
            int macizosPorPagina = 25;

            if (string.IsNullOrEmpty(busquedaProvincia))
            {
                busquedaProvincia = "";
            }
            else
            {
                ViewBag.Distritos = UbigeoPeru.Distritos[busquedaProvincia].OrderBy(p => p).Select(d => new SelectListItem
                {
                    Text = d,
                    Value = d
                });
            }
            if (string.IsNullOrEmpty(busquedaDistrito))
            {
                busquedaDistrito = "";
            }
            else
            {
                ViewBag.Localidades = UbigeoPeru.Localidades[busquedaDistrito].OrderBy(p => p).Select(d => new SelectListItem
                {
                    Text = d,
                    Value = d
                });
            }
            if (string.IsNullOrEmpty(busquedaLocalidad))
            {
                busquedaLocalidad = "";
            }

            var model = await GetMacizoPaginado(busquedaProvincia, busquedaDistrito, busquedaLocalidad, paginaActual, macizosPorPagina);

            ViewBag.Provincias = UbigeoPeru.Provincias.OrderBy(p => p).Select(d => new SelectListItem
            {
                Text = d,
                Value = d
            });

            return View(model);
        }

        // GET: Admin/MacizosForestales/Detalle/5
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var macizo = await _context.MacizoForestal
                .FirstOrDefaultAsync(m => m.IdMacizoForestal == id);
            if (macizo == null)
            {
                return NotFound();
            }

            var writer = new GeoJsonWriter();
            string geoJson = writer.Write(macizo.Geometria);

            ViewBag.GeoJson = geoJson;

            return View(macizo);
        }

        // GET: Admin/MacizosForestales/Crear
        public IActionResult Crear()
        {
            ViewBag.Provincias = UbigeoPeru.Provincias.OrderBy(p => p).Select(d => new SelectListItem
            {
                Text = d,
                Value = d
            });

            return View();
        }

        // POST: Admin/MacizosForestales/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(MacizoForestal macizo)
        {
            if (ModelState.IsValid)
            {
                _context.Add(macizo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(macizo);
        }

        // GET: Admin/MacizosForestales/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var macizo = await _context.MacizoForestal.FindAsync(id);
            if (macizo == null)
            {
                return NotFound();
            }

            var writer = new GeoJsonWriter();
            string geoJson = writer.Write(macizo.Geometria);

            ViewBag.GeoJson = geoJson;

            ViewBag.Provincias = UbigeoPeru.Provincias.OrderBy(p => p).Select(d => new SelectListItem
            {
                Text = d,
                Value = d
            });

            ViewBag.Distritos = UbigeoPeru.Distritos[macizo.Provincia].OrderBy(p => p).Select(d => new SelectListItem
            {
                Text = d,
                Value = d
            });

            ViewBag.Localidades = UbigeoPeru.Localidades[macizo.Distrito].OrderBy(p => p).Select(d => new SelectListItem
            {
                Text = d,
                Value = d
            });

            return View(macizo);
        }

        // POST: Admin/MacizosForestales/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, MacizoForestal macizo)
        {
            if (id != macizo.IdMacizoForestal)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(macizo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MacizoExists(macizo.IdMacizoForestal))
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
            return View(macizo);
        }

        // GET: Admin/MacizosForestales/Eliminar/5
        public async Task<IActionResult> Eliminar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var macizo = await _context.MacizoForestal.Include(m => m.CampaniaDetalles)
                .FirstOrDefaultAsync(m => m.IdMacizoForestal == id);
            if (macizo == null)
            {
                return NotFound();
            }

            var writer = new GeoJsonWriter();
            string geoJson = writer.Write(macizo.Geometria);

            ViewBag.GeoJson = geoJson;

            return View(macizo);
        }

        // POST: Admin/MacizosForestales/Eliminar/5
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var macizo = await _context.MacizoForestal.FindAsync(id);
            if (macizo != null)
            {
                _context.MacizoForestal.Remove(macizo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MacizoExists(int id)
        {
            return _context.MacizoForestal.Any(e => e.IdMacizoForestal == id);
        }
    }
}
