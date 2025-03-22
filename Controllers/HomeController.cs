using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DameChanceSV2.Models;
using DameChanceSV2.DAL;
using DameChanceSV2.Utilities;

namespace DameChanceSV2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UsuarioDAL _usuarioDAL;

        public HomeController(ILogger<HomeController> logger, UsuarioDAL usuarioDAL)
        {
            _logger = logger;
            _usuarioDAL = usuarioDAL;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // =====================================
        // ADMIN DASHBOARD
        // =====================================
        [HttpGet]
        public IActionResult AdminDashboard()
        {
            if (!EsAdmin()) return NotFound();

            var (total, verificados, noVerificados, admins) = _usuarioDAL.GetUserCounts();
            var listaUsuarios = _usuarioDAL.GetAllUsuarios();
            int sinVerificarMas3Dias = _usuarioDAL.GetUnverifiedCountOlderThan3Days();

            ViewBag.Total = total;
            ViewBag.Verificados = verificados;
            ViewBag.NoVerificados = noVerificados;
            ViewBag.Admins = admins;
            ViewBag.SinVerificarMas3Dias = sinVerificarMas3Dias;

            return View("AdminDashboard", listaUsuarios);
        }

        // GET: /Home/CreateUser
        [HttpGet]
        public IActionResult CreateUser()
        {
            if (!EsAdmin()) return NotFound();
            return View();
        }

        // POST: /Home/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(Usuario model)
        {
            if (!EsAdmin()) return NotFound();

            if (ModelState.IsValid)
            {
                model.Contrasena = PasswordHelper.HashPassword(model.Contrasena);
                _usuarioDAL.InsertUsuario(model);
                return RedirectToAction("AdminDashboard");
            }

            return View(model);
        }

        // GET: /Home/EditUser/5
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            if (!EsAdmin()) return NotFound();

            var usuario = _usuarioDAL.GetUsuarioById(id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // POST: /Home/EditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(Usuario model)
        {
            if (!EsAdmin()) return NotFound();

            if (ModelState.IsValid)
            {
                // Recuperar la contraseña original del usuario
                var usuarioOriginal = _usuarioDAL.GetUsuarioById(model.Id);
                if (usuarioOriginal != null)
                {
                    model.Contrasena = usuarioOriginal.Contrasena;
                }

                _usuarioDAL.UpdateUsuario(model);
                return RedirectToAction("AdminDashboard");
            }

            var allErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            ViewBag.Errors = allErrors;

            return View(model);
        }

        // GET: /Home/DeleteUser/5
        [HttpGet]
        public IActionResult DeleteUser(int id)
        {
            if (!EsAdmin()) return NotFound();

            var usuario = _usuarioDAL.GetUsuarioById(id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // POST: /Home/DeleteUserConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUserConfirmed(int id)
        {
            if (!EsAdmin()) return NotFound();

            _usuarioDAL.DeleteUsuario(id);
            return RedirectToAction("AdminDashboard");
        }

        // =====================================
        // MÉTODO PRIVADO DE CHEQUEO DE ADMIN
        // =====================================
        private bool EsAdmin()
        {
            var userSession = Request.Cookies["UserSession"];
            if (string.IsNullOrEmpty(userSession)) return false;

            if (!int.TryParse(userSession, out int userId)) return false;

            var user = _usuarioDAL.GetUsuarioById(userId);
            if (user == null) return false;

            return (user.RolId == 1); // Rol 1 = Admin
        }
    }
}
