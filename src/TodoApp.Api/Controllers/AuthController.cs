using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TodoApp.Api.Contracts.Auth;
using TodoApp.Api.Options;
using TodoApp.Domain.Abstractions;

namespace TodoApp.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    ITokenService tokenService,
    IRefreshTokenStore refreshTokenStore,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new IdentityUser
        {
            UserName = request.UserName,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokenResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.UserName);
        if (user is null)
        {
            return Unauthorized();
        }

        var success = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!success.Succeeded)
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user.Id, user.UserName ?? request.UserName, roles);
        var refreshToken = await refreshTokenStore.CreateAsync(user.Id, TimeSpan.FromDays(_jwtOptions.RefreshTokenDays), cancellationToken);

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Ok(new AuthTokenResponse(
            accessToken,
            refreshToken.Token,
            DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes)));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokenResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var active = await refreshTokenStore.GetActiveAsync(request.RefreshToken, cancellationToken);
        if (active is null)
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(active.UserId);
        if (user is null)
        {
            return Unauthorized();
        }

        await refreshTokenStore.RevokeAsync(request.RefreshToken, cancellationToken);
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user.Id, user.UserName ?? "user", roles);
        var nextRefresh = await refreshTokenStore.CreateAsync(user.Id, TimeSpan.FromDays(_jwtOptions.RefreshTokenDays), cancellationToken);

        return Ok(new AuthTokenResponse(
            accessToken,
            nextRefresh.Token,
            DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes)));
    }

    [HttpGet("oidc/login")]
    [AllowAnonymous]
    public IActionResult OidcLogin([FromQuery] string returnUrl = "/")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = returnUrl
        };

        return Challenge(properties, "oidc");
    }
}