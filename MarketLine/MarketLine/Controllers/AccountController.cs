using MarketLine.Models;
using MarketLine.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MarketLine.Controllers
{
    public class AccountController : Controller
    {
        private const string ResetEmailKey = "PasswordResetEmail";
        private const string ResetTokenKey = "PasswordResetToken";

        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Profile");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, AuthMessages.InvalidLogin);
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Item");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Profile");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("FirstName,LastName,Email,Phone,Password,ConfirmPassword")] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
                IsAdmin = false,
                RegistrationDate = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                AddRegistrationErrors(result);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Profile");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            HttpContext.Session.Remove(ResetTokenKey);
            HttpContext.Session.SetString(ResetEmailKey, model.Email);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                HttpContext.Session.SetString(ResetTokenKey, token);
            }

            TempData["AccountMessage"] = AuthMessages.ForgotPassword;
            return RedirectToAction(nameof(ResetPassword));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? email = null, string? token = null)
        {
            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(token))
            {
                return View(new ResetPasswordViewModel
                {
                    Email = email,
                    Token = token
                });
            }

            var sessionEmail = HttpContext.Session.GetString(ResetEmailKey);
            if (string.IsNullOrWhiteSpace(sessionEmail))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel { Email = sessionEmail });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var token = !string.IsNullOrWhiteSpace(model.Token)
                ? model.Token
                : HttpContext.Session.GetString(ResetTokenKey);
            var sessionEmail = HttpContext.Session.GetString(ResetEmailKey);
            var email = !string.IsNullOrWhiteSpace(sessionEmail) ? sessionEmail : model.Email;

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || string.IsNullOrWhiteSpace(token))
            {
                ModelState.AddModelError(string.Empty, AuthMessages.ResetFailed);
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, AuthMessages.ResetFailed);
                return View(model);
            }

            HttpContext.Session.Remove(ResetEmailKey);
            HttpContext.Session.Remove(ResetTokenKey);
            TempData["AccountMessage"] = AuthMessages.PasswordChanged;
            return RedirectToAction(nameof(Login));
        }

        private void AddRegistrationErrors(IdentityResult result)
        {
            var revealsAccount = result.Errors.Any(error =>
                error.Code is "DuplicateEmail" or "DuplicateUserName");

            if (revealsAccount)
            {
                ModelState.AddModelError(string.Empty, AuthMessages.RegisterFailed);
                return;
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
