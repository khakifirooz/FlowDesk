using FlowDesk.Domain.Common;
using FlowDesk.Infrastructure.Identity;
using FlowDesk.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
    private readonly AccountDeletionService _accountDeletion;
    private readonly IConfiguration _configuration;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AccountDeletionService accountDeletion,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _accountDeletion = accountDeletion;
        _configuration = configuration;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        base.OnActionExecuting(context);
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

        var account = await _userManager.FindByEmailAsync(model.Email.Trim());
        var result = account is null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await _signInManager.PasswordSignInAsync(account, model.Password, model.RememberMe, lockoutOnFailure: true);

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

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var account = await _userManager.GetUserAsync(User);
        if (account is null) return Forbid();

        ViewData["ProfileEmail"] = account.Email;
        return View(new ProfileViewModel
        {
            FullName = account.FullName,
            UserName = account.UserName ?? string.Empty,
            JobTitle = account.JobTitle,
            Bio = account.Bio
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var account = await _userManager.GetUserAsync(User);
        if (account is null) return Forbid();
        ViewData["ProfileEmail"] = account.Email;

        string fullName = TextRules.Normalize(model.FullName);
        string userName = model.UserName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fullName))
            ModelState.AddModelError(nameof(model.FullName), "نام نمایشی را وارد کنید.");

        if (!ModelState.IsValid) return View(model);

        account.FullName = fullName;
        account.UserName = userName;
        account.JobTitle = string.IsNullOrWhiteSpace(model.JobTitle) ? null : model.JobTitle.Trim();
        account.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();

        IdentityResult result;
        try { result = await _userManager.UpdateAsync(account); }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException sql &&
            sql.Number is 2601 or 2627)
        {
            ModelState.AddModelError(nameof(model.UserName), "این نام کاربری قبلاً گرفته شده است.");
            return View(model);
        }

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Code switch
                {
                    "DuplicateUserName" => "این نام کاربری قبلاً گرفته شده است.",
                    "InvalidUserName" => "نام کاربری معتبر نیست.",
                    "ConcurrencyFailure" => "پروفایل هم‌زمان تغییر کرده است. صفحه را دوباره باز کنید.",
                    _ => TranslateError(error)
                });
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(account);
        TempData["SuccessMessage"] = "پروفایل ذخیره شد.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult DeleteAccount() =>
        _configuration.GetValue<bool>("Features:AccountDeletion")
            ? View(new DeleteAccountViewModel()) : NotFound();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model, CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue<bool>("Features:AccountDeletion")) return NotFound();
        if (!model.ConfirmDeletion)
            ModelState.AddModelError(nameof(model.ConfirmDeletion), "حذف دائمی حساب را تأیید کنید.");
        if (!ModelState.IsValid) return View(model);

        var account = await _userManager.GetUserAsync(User);
        if (account is null) return Forbid();

        var result = await _accountDeletion.DeleteAsync(account, model.Password, cancellationToken);
        if (result == AccountDeletionResult.Deleted)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, result switch
        {
            AccountDeletionResult.InvalidPassword => "رمز عبور فعلی درست نیست.",
            AccountDeletionResult.ManagesProjects => "این حساب مدیر یک یا چند پروژه است و فعلاً قابل حذف نیست.",
            AccountDeletionResult.LinkedToProjects => "ابتدا از مدیر پروژه بخواهید اتصال حساب شما را از همهٔ پروژه‌ها قطع کند.",
            _ => "حساب حذف نشد. صفحه را دوباره باز کنید و تلاش کنید."
        });
        model.Password = string.Empty;
        ModelState.Remove(nameof(model.Password));
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