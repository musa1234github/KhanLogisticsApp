using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhanLogistics.Models;

namespace KhanLogistics.Controllers
{
    public class FactoriesController : Controller
    {
        private readonly TransportMgmtContext _context;

        public FactoriesController(TransportMgmtContext context)
        {
            _context = context;
        }

        // GET: Factories
        public async Task<IActionResult> Index()
        {
              return _context.TblFactories != null ? 
                          View(await _context.TblFactories.ToListAsync()) :
                          Problem("Entity set 'TransportDbContext.TblFactories'  is null.");
        }

        // GET: Factories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.TblFactories == null)
            {
                return NotFound();
            }

            var tblFactory = await _context.TblFactories
                .FirstOrDefaultAsync(m => m.FID == id);
            if (tblFactory == null)
            {
                return NotFound();
            }

            return View(tblFactory);
        }

        // GET: Factories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Factories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FID,Code,FactoryName,Address,IsActive,CreatedOn,ModifiedOn,CreatedBy,ModifiedBy,Gstin")] TblFactory tblFactory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tblFactory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tblFactory);
        }

        // GET: Factories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.TblFactories == null)
            {
                return NotFound();
            }

            var tblFactory = await _context.TblFactories.FindAsync(id);
            if (tblFactory == null)
            {
                return NotFound();
            }
            return View(tblFactory);
        }

        // POST: Factories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FID,Code,FactoryName,Address,IsActive,CreatedOn,ModifiedOn,CreatedBy,ModifiedBy,Gstin")] TblFactory tblFactory)
        {
            if (id != tblFactory.FID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tblFactory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TblFactoryExists(tblFactory.FID))
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
            return View(tblFactory);
        }

        // GET: Factories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.TblFactories == null)
            {
                return NotFound();
            }

            var tblFactory = await _context.TblFactories
                .FirstOrDefaultAsync(m => m.FID == id);
            if (tblFactory == null)
            {
                return NotFound();
            }

            return View(tblFactory);
        }

        // POST: Factories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.TblFactories == null)
            {
                return Problem("Entity set 'TransportDbContext.TblFactories'  is null.");
            }
            var tblFactory = await _context.TblFactories.FindAsync(id);
            if (tblFactory != null)
            {
                _context.TblFactories.Remove(tblFactory);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TblFactoryExists(int id)
        {
          return (_context.TblFactories?.Any(e => e.FID == id)).GetValueOrDefault();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSingleOrMultiple(int[] ids)
        {
            string result = string.Empty;
            try
            {
                if (ids.Count()> 0)
                {
                    foreach (int id in ids)
                    {
                        var data = await   _context.TblFactories.Where(d => d.FID == id).FirstOrDefaultAsync();
                        if (data != null)
                        {
                            _context.TblFactories.Remove(data);
                        }
                    }
                   await _context.SaveChangesAsync();
                    TempData["success"] = "Record Deleted";
                    result = "success";
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                result = "error";
            }
            return new JsonResult(result); // Return JSON result
        }
    }
}
