using System;
using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IEmailVerificationService emailVerification, IEmailService emailService, ISmsService smsService) : BaseApiController
{
    [HttpPost("register")]// api/account/register
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {

        if (registerDto == null)
        {
            return BadRequest("DEBUG: The entire registration object (registerDto) is null.");
        }

        if (string.IsNullOrWhiteSpace(registerDto.Email))
        {
            return BadRequest($"DEBUG: The email field is null or empty. DisplayName received: '{registerDto.DisplayName}'");
        }
        //

        var isEmailValid = await emailVerification.IsEmailValidAsync(registerDto.Email);
        if (!isEmailValid)
        {
            return BadRequest("The provided email address is not valid or could not be verified.");
        }

        var user = new AppUser
        {
            DisplayName = registerDto.DisplayName,
            Email = registerDto.Email,
            UserName = registerDto.Email,
            PhoneNumber = registerDto.PhoneNumber,

            Member = new Member
            {
                DisplayName = registerDto.DisplayName,
                Gender = registerDto.Gender,
                City = registerDto.City,
                Country = registerDto.Country,
                DateOfBirth = registerDto.DateOfBirth

            }
        };
        var result = await userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("identity", error.Description);

            }
            return ValidationProblem();
        }

        await userManager.AddToRoleAsync(user, "Member");

        await SendConfirmationEmailAsync(user, "Confirm Your Email", "<h1>Welcome to our App!</h1>");

        return Ok(await user.ToDto(tokenService));
    }

    [HttpPost("confirm-email")]
    public async Task<ActionResult> ConfirmEmail(ConfirmationEmailDto confirmEmailDto)
    {
        var user = await userManager.FindByEmailAsync(confirmEmailDto.Email!);
        if (user == null) return Unauthorized("Invalid email or confirmation code.");
        if (user.EmailConfirmed) return BadRequest("This email has already been confirmed.");

        if (user.EmailConfirmationCode != confirmEmailDto.Code || user.EmailConfirmationCodeExpiry <= DateTime.UtcNow)
        {
            return Unauthorized("Invalid or expired confirmation code.");
        }

        user.EmailConfirmed = true;
        user.EmailConfirmationCode = null;
        user.EmailConfirmationCodeExpiry = null;

        var smsCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        user.SmsConfirmationCode = smsCode;
        user.SmsConfirmationCodeExpiry = DateTime.UtcNow.AddMinutes(3);
        await userManager.UpdateAsync(user);

        var smsMessage = $"Your verification code is: {smsCode}";
        await smsService.SendSmsAsync(user.PhoneNumber!, smsMessage);

        return Ok(new { message = "Email confirmed. Please check your phone for an SMS verification code." });
    }

    [HttpPost("confirm-phone")]
    public async Task<ActionResult<UserDto>> ConfirmPhone(ConfirmPhoneDto confirmPhoneDto)
    {
        var user = await userManager.FindByEmailAsync(confirmPhoneDto.Email!);
        if (user == null) return Unauthorized("Invalid email or code.");

        // Ensure steps are done in order
        if (!user.EmailConfirmed) return BadRequest("Please confirm your email address first.");
        if (user.PhoneNumberConfirmed) return BadRequest("This phone number has already been confirmed.");

        if (user.SmsConfirmationCode != confirmPhoneDto.Code || user.SmsConfirmationCodeExpiry <= DateTime.UtcNow)
        {
            return Unauthorized("Invalid or expired SMS code.");
        }

        user.PhoneNumberConfirmed = true;
        user.SmsConfirmationCode = null;
        user.SmsConfirmationCodeExpiry = null;
        await userManager.UpdateAsync(user);

        await SetRefreshTokenCookie(user);
        return await user.ToDto(tokenService);
    }

    [HttpPost("resend-confirmation-code")]
    public async Task<ActionResult> ResendConfirmationCode([FromBody] string email)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return Ok(new { message = "If an account with this email exists, a new confirmation code has been sent." });
        }
        if (user.EmailConfirmed && !user.PhoneNumberConfirmed)
        {
            var smsCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            user.SmsConfirmationCode = smsCode;
            user.SmsConfirmationCodeExpiry = DateTime.UtcNow.AddMinutes(3);

            await userManager.UpdateAsync(user);

            var smsMessage = $"Your new verification code is: {smsCode}";
            await smsService.SendSmsAsync(user.PhoneNumber!, smsMessage);

            return Ok(new { message = "A new phone confirmation code has been sent." });
        }
        if (!user.EmailConfirmed)
        {
            await SendConfirmationEmailAsync(user, "New Confirmation Code", "<h1>New Confirmation Code</h1>");
            return Ok(new { message = "A new email confirmation code has been sent." });
        }
        return BadRequest(new { message = "This account is already fully confirmed." });
    }


    private async Task SendConfirmationEmailAsync(AppUser user, string subject, string title)
    {
        // Generate 6-digit code
        var confirmationCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        user.EmailConfirmationCode = confirmationCode;
        user.EmailConfirmationCodeExpiry = DateTime.UtcNow.AddMinutes(3); // Code is valid for 15 minutes
        await userManager.UpdateAsync(user);

        // Send the email
        var emailBody = $"{title}<p>Your email confirmation code is: <strong>{confirmationCode}</strong></p>";
        await emailService.SendEmailAsync(user.Email!, subject, emailBody);
    }



    [HttpPost("login")]// api/account/login
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await userManager.FindByEmailAsync(loginDto.Email);
        if (user == null) return Unauthorized("Invalid email");

        var result = await userManager.CheckPasswordAsync(user, loginDto.Password);
        if (!result) return Unauthorized("iNVALID password");

        if (!user.EmailConfirmed)
        return Unauthorized("Please confirm your email before logging in");

        if (!user.PhoneNumberConfirmed)
        return Unauthorized("Please confirm your phone number before logging in");

        await SetRefreshTokenCookie(user);
        return await user.ToDto(tokenService);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<UserDto>> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (refreshToken == null) return NoContent();

        var user = await userManager.Users
        .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken
        && x.RefreshTokenExpiry > DateTime.UtcNow);

        if (user == null) return Unauthorized();

        await SetRefreshTokenCookie(user);
        return await user.ToDto(tokenService);
    }

    private async Task SetRefreshTokenCookie(AppUser user)
    {
        var refreshToken = tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await userManager.UpdateAsync(user);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        await userManager.Users
        .Where(x => x.Id == User.GetMemberId())
        .ExecuteUpdateAsync(setters => setters
        .SetProperty(x => x.RefreshToken, _ => null)
        .SetProperty(x => x.RefreshTokenExpiry, _ => null)
        );

        Response.Cookies.Delete("refresToken");
        return Ok();

    }
}
