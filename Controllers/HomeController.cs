using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DameChanceSV2.Models;
using DameChanceSV2.DAL;

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
            // 1) Verificar si es Admin
            if (!EsAdmin()) return NotFound();

            // Bloque 2: Obtener contadores
            var (total, verificados, noVerificados, admins) = _usuarioDAL.GetUserCounts();

            // Bloque 1: Obtener listado de usuarios
            var listaUsuarios = _usuarioDAL.GetAllUsuarios();

            // Bloque 3: Obtener cuentas sin verificar hace mas de 3 dias
            int sinVerificarMas3Dias = _usuarioDAL.GetUnverifiedCountOlderThan3Days();

            // Pasar datos a la vista con ViewBag o ViewModel
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
                // Si deseas hashear la contraseña, descomenta la siguiente línea
                // model.Contrasena = PasswordHelper.HashPassword(model.Contrasena);

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
                _usuarioDAL.UpdateUsuario(model);
                return RedirectToAction("AdminDashboard");
            }
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
        // MÉTODO PRIVADO DE CHEQUEO
        // =====================================
        private bool EsAdmin()
        {
            // Verificar si existe cookie de sesión
            var userSession = Request.Cookies["UserSession"];
            if (string.IsNullOrEmpty(userSession)) return false;

            // Verificar si la cookie es un int válido
            if (!int.TryParse(userSession, out int userId)) return false;

            // Buscar el usuario en la BD
            var user = _usuarioDAL.GetUsuarioById(userId);
            if (user == null) return false;

            // RolId=1 => Admin
            return (user.RolId == 1);
        }
    }
}
