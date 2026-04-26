using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KhanLogistics.Models;
using KhanLogistics.Bal;

namespace KhanLogistics.Controllers
{
    public class UsersController : Controller
    {
        private readonly ISrvUser _svcUser = null;

        public UsersController(/*TransportMgmtContext context,*/ ISrvUser svcUser)
        {
            //_context = context;
            _svcUser = svcUser;
        }
    

        // GET: TblUsers
        public async Task<IActionResult> Index()
        {
            return await Task.FromResult<IActionResult>(View(_svcUser.GetAllUsers().ToList()));
        }

        // GET: TblUsers/Details/5
        public async Task<IActionResult> Details(int id)
        {
            return await Task.FromResult<IActionResult>(View(_svcUser.GetUserById(id)));
           
        }

        // GET: TblUsers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TblUsers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,UserName,Email,Password,City,Address,Doj,LastLogIn,Role")] TblUser tblUser)
        {
            if (ModelState.IsValid)
            {
                _svcUser.CreateUser(tblUser);
                return await Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));
            }
            return await Task.FromResult<IActionResult>(View(tblUser));
        }

        // GET: TblUsers/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (id == null || id == 0)
            {
                return await Task.FromResult<IActionResult>(NotFound());
            }

            var tblUser = _svcUser.GetUserById(id);
            if (tblUser == null)
            {
                return await Task.FromResult<IActionResult>(NotFound());
            }
            return await Task.FromResult<IActionResult>(View(tblUser));
        }

        // POST: TblUsers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,UserName,Email,Password,City,Address,Doj,LastLogIn,Role")] TblUser tblUser)
        {
            if (id != tblUser.UserId)
            {
                return await Task.FromResult<IActionResult>(NotFound());
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _svcUser.UpdateUser(tblUser);
                    //await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_svcUser.TblUserExists(tblUser.UserId))
                    {
                        return await Task.FromResult<IActionResult>(NotFound());
                    }
                    else
                    {
                        throw;
                    }
                }
                return await Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));
            }
            return await Task.FromResult<IActionResult>(View(tblUser));
        }

        // GET: TblUsers/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            return await Task.FromResult<IActionResult>(View(_svcUser.GetUserById(id)));
        }

        // POST: TblUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            int i = _svcUser.DeleteUser(id);
            if (i != 1)
            {
                return await Task.FromResult<IActionResult>(Problem("Entity set 'KhanLogistics.TblVehicles'  is null."));
            }
            return await Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));
        }

        public Task<IActionResult> Login()
        {
            return Task.FromResult<IActionResult>(View("Login"));
        }
        [HttpPost]
        public Task<IActionResult> Login(string UserName, string UserPwd)
        {
            var obj = _svcUser.GetUserByCreds(UserName, UserPwd);

            if (obj != null)
            {
                return Task.FromResult<IActionResult>(RedirectToAction("Index", "Home"));
            }
            return Task.FromResult<IActionResult>(View("Login"));

        }

        //private bool TblUserExists(int id)
        //{
        //  return (_context.TblUsers?.Any(e => e.UserId == id)).GetValueOrDefault();
        //}
    }
}
