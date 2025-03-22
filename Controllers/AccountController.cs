using Microsoft.AspNetCore.Mvc;
using DameChanceSV2.Models;
using DameChanceSV2.DAL;
using DameChanceSV2.Utilities;
using System;
using DameChanceSV2.Services;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http; // Para CookieOptions

namespace DameChanceSV2.Controllers
{
    public class AccountController : Controller
    {
        private readonly UsuarioDAL _usuarioDAL;
        private readonly IEmailService _emailService;
        private readonly PerfilDeUsuarioDAL _perfilDeUsuarioDAL;

        // Diccionario para almacenar temporalmente los tokens de verificación (para demo).
        private static Dictionary<string, int> EmailVerificationTokens = new Dictionary<string, int>();

        // Diccionario para almacenar tokens de reseteo de contraseña (para demo).
        private static Dictionary<string, int> PasswordResetTokens = new Dictionary<string, int>();

        public AccountController(UsuarioDAL usuarioDAL, IEmailService emailService, PerfilDeUsuarioDAL perfilDeUsuarioDAL)
        {
            _usuarioDAL = usuarioDAL;
            _emailService = emailService;
            _perfilDeUsuarioDAL = perfilDeUsuarioDAL;
        }

        // GET: /Account/Registro
        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }

        // POST: /Account/Registro
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registro(RegistroViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Verificar si el correo ya existe.
                var usuarioExistente = _usuarioDAL.GetUsuarioByCorreo(model.Correo);
                if (usuarioExistente != null)
                {
                    ModelState.AddModelError("Correo", "El correo ya está registrado.");
                    return View(model);
                }

                // Crear usuario con contraseña hasheada y rol por defecto (usuario estándar).
                Usuario nuevoUsuario = new Usuario
                {
                    Nombre = model.Nombre,
                    Correo = model.Correo,
                    Contrasena = PasswordHelper.HashPassword(model.Contrasena),
                    Estado = false, // Inicialmente en false hasta que se verifique el correo
                    RolId = 2       // Asigna RolId=2 (usuario) por defecto
                };

                int nuevoId = _usuarioDAL.InsertUsuario(nuevoUsuario);

                // Generar token único para verificación y almacenarlo.
                string token = Guid.NewGuid().ToString();
                EmailVerificationTokens[token] = nuevoId;

                // Generar enlace de verificación.
                var verificationLink = Url.Action("VerificarCuenta", "Account", new { token = token }, Request.Scheme);

                // Enviar correo de verificación.
                string subject = "Verifica tu cuenta en DameChance";
                string body = $"<p>Hola {model.Nombre},</p>" +
                              $"<p>Por favor, verifica tu cuenta haciendo clic en el siguiente enlace:</p>" +
                              $"<p><a href='{verificationLink}'>Verificar mi cuenta</a></p>" +
                              $"<p>Si no te registraste en nuestro sitio, ignora este mensaje.</p>";

                _emailService.SendEmail(model.Correo, subject, body);

                // Mostrar vista informativa.
                ViewBag.Message = "Registro exitoso. Se ha enviado un correo de verificación a tu dirección.";
                return View("Informacion");
            }
            return View(model);
        }

        // GET: /Account/VerificarCuenta?token=xxx
        [HttpGet]
        public IActionResult VerificarCuenta(string token)
        {
            if (string.IsNullOrEmpty(token) || !EmailVerificationTokens.ContainsKey(token))
            {
                ViewBag.ErrorMessage = "El token es inválido o ha expirado.";
                return View("Error");
            }

            int usuarioId = EmailVerificationTokens[token];
            // Actualizar el estado del usuario a verificado (true).
            _usuarioDAL.UpdateEstado(usuarioId, true);

            // Eliminar el token.
            EmailVerificationTokens.Remove(token);

            ViewBag.Message = "Cuenta verificada con éxito. Ahora puedes iniciar sesión.";
            return View();
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var usuario = _usuarioDAL.GetUsuarioByCorreo(model.Correo);
                if (usuario != null && PasswordHelper.VerifyPassword(model.Contrasena, usuario.Contrasena))
                {
                    if (!usuario.Estado)
                    {
                        ModelState.AddModelError("", "Tu cuenta no está verificada. Revisa tu correo electrónico.");
                        return View(model);
                    }
                    // Autenticación exitosa. Crear cookie de sesión.
                    var options = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        Expires = DateTime.Now.AddMinutes(30)
                    };
                    Response.Cookies.Append("UserSession", usuario.Id.ToString(), options);
                    // Establecer también la cookie del rol.
                    Response.Cookies.Append("UserRole", usuario.RolId.ToString(), options);

                    // Verificar si tiene un perfil
                    if (usuario.RolId != 1)
                    {
                        var perfil = _perfilDeUsuarioDAL.GetPerfilByUsuarioId(usuario.Id);
                        if (perfil == null)
                        {
                            // Redirige al formulario de completar perfil
                            return RedirectToAction("CompletarPerfil", "Account");
                        }
                    }


                    // Redirigir según rol.
                    if (usuario.RolId == 1)
                    {
                        return RedirectToAction("AdminDashboard", "Home");
                    }
                    else
                    {
                        return RedirectToAction("Dashboard", "Home");
                    }
                }
                ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
            }
            return View(model);
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            Response.Cookies.Delete("UserSession");
            Response.Cookies.Delete("UserRole");
            return RedirectToAction("Login");
        }

        // GET: /Account/RecuperarContrasena
        [HttpGet]
        public IActionResult RecuperarContrasena()
        {
            return View();
        }

        // POST: /Account/RecuperarContrasena
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RecuperarContrasena(RecuperarContrasenaViewModel model)
        {
            if (ModelState.IsValid)
            {
                var usuario = _usuarioDAL.GetUsuarioByCorreo(model.Correo);
                if (usuario != null)
                {
                    // Generar token para recuperación.
                    string token = Guid.NewGuid().ToString();
                    PasswordResetTokens[token] = usuario.Id;

                    // Generar enlace de reseteo.
                    var resetLink = Url.Action("ResetPassword", "Account", new { token = token }, Request.Scheme);

                    // Construir asunto y cuerpo del correo.
                    string subject = "Recuperación de contraseña - DameChance";
                    string body = $@"
                        <p>Hola {usuario.Nombre},</p>
                        <p>Recibimos una solicitud para restablecer tu contraseña. 
                        Por favor, haz clic en el siguiente enlace para continuar:</p>
                        <p><a href='{resetLink}'>Recuperar Contraseña</a></p>
                        <p>Si no solicitaste este cambio, puedes ignorar este mensaje.</p>";

                    _emailService.SendEmail(usuario.Correo, subject, body);

                    ViewBag.Message = "Se ha enviado un enlace de recuperación a tu correo.";
                }
                else
                {
                    ModelState.AddModelError("Correo", "El correo no se encuentra registrado.");
                }
            }
            return View(model);
        }

        // GET: /Account/ResetPassword?token=xxx
        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            ViewBag.Token = token;
            return View();
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(string token, string nuevaContrasena, string confirmarContrasena)
        {
            if (string.IsNullOrEmpty(nuevaContrasena) || string.IsNullOrEmpty(confirmarContrasena))
            {
                ModelState.AddModelError("", "Todos los campos son obligatorios.");
                ViewBag.Token = token;
                return View();
            }
            if (nuevaContrasena != confirmarContrasena)
            {
                ModelState.AddModelError("", "Las contraseñas no coinciden.");
                ViewBag.Token = token;
                return View();
            }

            if (!PasswordResetTokens.ContainsKey(token))
            {
                ModelState.AddModelError("", "El token es inválido o ha expirado.");
                ViewBag.Token = token;
                return View("ResetPassword");
            }

            int userId = PasswordResetTokens[token];
            string hashed = PasswordHelper.HashPassword(nuevaContrasena);
            _usuarioDAL.UpdateContrasena(userId, hashed);
            PasswordResetTokens.Remove(token);

            return RedirectToAction("Login");
        }

        // GET: /Account/CompletarPerfil
        [HttpGet]
        public IActionResult CompletarPerfil()
        {
            // Solo permitir si está logueado
            var userSession = Request.Cookies["UserSession"];
            if (string.IsNullOrEmpty(userSession))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // POST: /Account/CompletarPerfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompletarPerfil(CompletarPerfilViewModel model)
        {
            var userSession = Request.Cookies["UserSession"];
            if (string.IsNullOrEmpty(userSession))
            {
                return RedirectToAction("Login");
            }

            if (!int.TryParse(userSession, out int userId))
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                // Guardar el perfil en la base de datos
                var perfilExistente = _perfilDeUsuarioDAL.GetPerfilByUsuarioId(userId);
                string rutaImagen = null;

                // Manejo de la imagen, si el usuario subió algo
                if (model.ImagenPerfil != null && model.ImagenPerfil.Length > 0)
                {
                    // Nombre único para el archivo
                    string nombreArchivo = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(model.ImagenPerfil.FileName);
                    string carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "perfiles");
                    string rutaFisica = Path.Combine(carpeta, nombreArchivo);

                    using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    {
                        model.ImagenPerfil.CopyTo(stream);
                    }

                    // Guardar la ruta relativa para la BD
                    rutaImagen = Path.Combine("images", "perfiles", nombreArchivo).Replace("\\", "/");
                }

                if (perfilExistente == null)
                {
                    // Crear nuevo perfil
                    PerfilDeUsuario nuevoPerfil = new PerfilDeUsuario
                    {
                        UsuarioId = userId,
                        Edad = model.Edad,
                        Genero = model.Genero,
                        Intereses = model.Intereses,
                        Ubicacion = model.Ubicacion,
                        ImagenPerfil = rutaImagen
                    };
                    _perfilDeUsuarioDAL.InsertPerfil(nuevoPerfil);
                }
                else
                {
                    // Actualizar perfil existente
                    perfilExistente.Edad = model.Edad;
                    perfilExistente.Genero = model.Genero;
                    perfilExistente.Intereses = model.Intereses;
                    perfilExistente.Ubicacion = model.Ubicacion;
                    if (!string.IsNullOrEmpty(rutaImagen))
                    {
                        perfilExistente.ImagenPerfil = rutaImagen;
                    }
                    _perfilDeUsuarioDAL.UpdatePerfil(perfilExistente);
                }

                // Una vez completado el perfil, redirigir al Dashboard de usuario común
                return RedirectToAction("Dashboard", "Home");
            }

            return View(model);
        }

    }
}
