using FlowDesk.Domain.Common;
using FlowDesk.Infrastructure.Identity;
using FlowDesk.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Web.Controllers;

[Authorize]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new RegisterViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string fullName = TextRules.Normalize(model.FullName);

        if (string.IsNullOrWhiteSpace(fullName))
        {
            ModelState.AddModelError(
                nameof(model.FullName),
                "نام و نام خانوادگی را وارد کنید.");

            return View(model);
        }

        string email = model.Email.Trim();

        var user = new ApplicationUser
        {
            FullName = fullName,
            Email = email,
            UserName = email
        };

        IdentityResult result;

        try
        {
            result = await _userManager.CreateAsync(
                user,
                model.Password);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627))
        {
            ModelState.AddModelError(
                string.Empty,
                "حسابی با این ایمیل قبلاً ثبت شده است.");

            return View(model);
        }

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    TranslateError(error));
            }

            return View(model);
        }

        await _signInManager.SignInAsync(
            user,
            isPersistent: false);

        return RedirectToLocal(model.ReturnUrl);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            userName: model.Email.Trim(),
            password: model.Password,
            isPersistent: model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        string message = result.IsLockedOut
            ? "به دلیل چند تلاش ناموفق، ورود موقتاً قفل شده است. پنج دقیقه بعد دوباره تلاش کنید."
            : "ورود انجام نشد. ایمیل و رمز عبور را بررسی کنید.";

        ModelState.AddModelError(string.Empty, message);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;

        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    private static string TranslateError(IdentityError error)
    {
        return error.Code switch
        {
            "DuplicateEmail" or "DuplicateUserName" =>
                "حسابی با این ایمیل قبلاً ثبت شده است.",

            "InvalidEmail" or "InvalidUserName" =>
                "ایمیل معتبر وارد کنید.",

            "PasswordTooShort" =>
                "رمز عبور باید حداقل ۱۰ کاراکتر باشد.",

            "PasswordRequiresDigit" =>
                "رمز عبور باید حداقل یک عدد داشته باشد.",

            "PasswordRequiresLower" =>
                "رمز عبور باید حداقل یک حرف کوچک انگلیسی داشته باشد.",

            "PasswordRequiresUpper" =>
                "رمز عبور باید حداقل یک حرف بزرگ انگلیسی داشته باشد.",

            "PasswordRequiresNonAlphanumeric" =>
                "رمز عبور باید حداقل یک علامت مانند ! یا @ داشته باشد.",

            "PasswordRequiresUniqueChars" =>
                "از کاراکترهای متنوع‌تری در رمز عبور استفاده کنید.",

            _ =>
                "ثبت‌نام انجام نشد. اطلاعات را بررسی کنید و دوباره تلاش کنید."
        };
    }
}