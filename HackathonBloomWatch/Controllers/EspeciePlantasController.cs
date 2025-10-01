using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HackathonBloomWatch.ViewModels;
using HackathonBloomWatch.Data;
using HackathonBloomWatch.Models;

namespace HackathonBloomWatch.Controllers
{
    public class EspeciePlantasController : Controller
    {
        private readonly HackathonBWContext _context;

        public EspeciePlantasController(HackathonBWContext context)
        {
            _context = context;
        }

        public async Task<EspeciePlantaPaginadoViewModel> GetEspeciePlantasPaginado(string? busquedaNombreEspecie, int paginaActual, int especiesPorPagina)
        {
            IQueryable<EspeciePlanta> query = _context.EspeciePlanta;

            if (!string.IsNullOrEmpty(busquedaNombreEspecie))
            {
                query = query.Where(p => p.NombreEspecie.Contains(busquedaNombreEspecie) || p.NombreComun.Contains(busquedaNombreEspecie));
            }

            int totalEspecies = await query.CountAsync();

            int totalPaginas = (int)Math.Ceiling((double)totalEspecies / especiesPorPagina);

            if (paginaActual < 1)
            {
                paginaActual = 1;
            }
            else if (paginaActual > totalPaginas)
            {
                paginaActual = totalPaginas;
            }

            List<EspeciePlanta> especies = new();
            if (totalEspecies > 0)
            {
                especies = await query
                    .OrderBy(p => p.NombreEspecie)
                    .Skip(especiesPorPagina * (paginaActual - 1))
                    .Take(especiesPorPagina)
                    .ToListAsync();
            }

            var model = new EspeciePlantaPaginadoViewModel
            {
                EspeciePlantas = especies,
                PaginaActual = paginaActual,
                TotalPaginas = totalPaginas,
                BusquedaNombreEspecie = busquedaNombreEspecie
            };
            return model;
        }

        // GET: Admin/EspeciePlantas
        public async Task<IActionResult> Index(string? busquedaNombreEspecie, int paginaActual = 1)
        {
            int especiesPorPagina = 10;

            if (string.IsNullOrEmpty(busquedaNombreEspecie))
            {
                busquedaNombreEspecie = "";
            }

            var model = await GetEspeciePlantasPaginado(busquedaNombreEspecie, paginaActual, especiesPorPagina);

            return View(model);
        }

        // GET: Admin/EspeciePlantas/Detalle/5
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var especiePlanta = await _context.EspeciePlanta
                .FirstOrDefaultAsync(m => m.IdEspeciePlanta == id);
            if (especiePlanta == null)
            {
                return NotFound();
            }

            byte[] imagenData = especiePlanta.ImagenEspecie;

            if (imagenData != null)
            {
                string imagenBase64 = Convert.ToBase64String(imagenData);
                ViewBag.Imagen = string.Format("data:image/png;base64,{0}", imagenBase64);
            }
            else
            {
                ViewBag.Imagen = null;
            }

            return View(especiePlanta);
        }

        // GET: Admin/EspeciePlantas/Crear
        public IActionResult Crear()
        {
            return View();
        }

        // POST: Admin/EspeciePlantas/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(EspeciePlanta especiePlanta)
        {
            ModelState.Remove("ImagenEspecie");

            if (ModelState.IsValid)
            {
                //var idEspeciePlanta = _context.EspeciePlanta.Any() ? _context.EspeciePlanta.Max(e => e.IdEspeciePlanta) + 1 : 1;

                //especiePlanta.IdEspeciePlanta = idEspeciePlanta;

                var imagen = HttpContext.Request.Form.Files;

                if (imagen.Count > 0)
                {
                    byte[] imagenData = null;
                    using (var fs = imagen[0].OpenReadStream())
                    using (var ms = new System.IO.MemoryStream())
                    {
                        fs.CopyTo(ms);
                        imagenData = ms.ToArray();
                    }
                    especiePlanta.ImagenEspecie = imagenData;
                }

                _context.Add(especiePlanta);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(especiePlanta);
        }

        // GET: Admin/EspeciePlantas/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var especiePlanta = await _context.EspeciePlanta.FindAsync(id);
            if (especiePlanta == null)
            {
                return NotFound();
            }

            byte[] imagenData = especiePlanta.ImagenEspecie;
            
            if (imagenData != null)
            {
                string imagenBase64 = Convert.ToBase64String(imagenData);
                ViewBag.Imagen = string.Format("data:image/png;base64,{0}", imagenBase64);
            }
            else
            {
                ViewBag.Imagen = null;
            }

            return View(especiePlanta);
        }

        // POST: Admin/EspeciePlantas/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, EspeciePlanta especiePlanta)
        {
            if (id != especiePlanta.IdEspeciePlanta)
            {
                return NotFound();
            }

            ModelState.Remove("ImagenEspecie");

            if (ModelState.IsValid)
            {
                try
                {
                    var imagen = HttpContext.Request.Form.Files;

                    if (imagen.Count > 0)
                    {
                        byte[] imagenData = null;
                        using (var fs = imagen[0].OpenReadStream())
                        using (var ms = new System.IO.MemoryStream())
                        {
                            fs.CopyTo(ms);
                            imagenData = ms.ToArray();
                        }
                        especiePlanta.ImagenEspecie = imagenData;
                    }
                    else
                    {
                        especiePlanta.ImagenEspecie = _context.EspeciePlanta.AsNoTracking().FirstOrDefault(p => p.IdEspeciePlanta == especiePlanta.IdEspeciePlanta).ImagenEspecie;
                    }

                    _context.Update(especiePlanta);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EspeciePlantaExists(especiePlanta.IdEspeciePlanta))
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
            return View(especiePlanta);
        }

        // GET: Admin/EspeciePlantas/Delete/5
        public async Task<IActionResult> Eliminar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var especiePlanta = await _context.EspeciePlanta.Include(e => e.CampaniaDetalles).FirstOrDefaultAsync(m => m.IdEspeciePlanta == id);
            if (especiePlanta == null)
            {
                return NotFound();
            }

            byte[] imagenData = especiePlanta.ImagenEspecie;

            if (imagenData != null)
            {
                string imagenBase64 = Convert.ToBase64String(imagenData);
                ViewBag.Imagen = string.Format("data:image/png;base64,{0}", imagenBase64);
            }
            else
            {
                ViewBag.Imagen = null;
            }

            return View(especiePlanta);
        }

        // POST: Admin/EspeciePlantas/Eliminar/5
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmed(int id)
        {
            var especiePlanta = await _context.EspeciePlanta.FindAsync(id);
            if (especiePlanta != null)
            {
                _context.EspeciePlanta.Remove(especiePlanta);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EspeciePlantaExists(int id)
        {
            return _context.EspeciePlanta.Any(e => e.IdEspeciePlanta == id);
        }
    }
}
