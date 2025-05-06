﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using auctionbay_backend.DTOs;
using auctionbay_backend.Models;

namespace auctionbay_backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthService(
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        /// <summary>
        /// Registers a new user with the specified credentials.
        /// </summary>
        /// <param name="dto">Data transfer object containing registration details.</param>
        /// <returns>
        /// An <see cref="IdentityResult"/> indicating success or failure.
        /// </returns>
        public async Task<IdentityResult> RegisterAsync(RegisterDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
            {
                var error = new IdentityError
                {
                    Code = "PasswordMismatch",
                    Description = "Passwords do not match."
                };
                return IdentityResult.Failed(error);
            }

            var user = new ApplicationUser
            {
                FirstName = dto.Name,
                LastName = dto.Surname,
                Email = dto.Email,
                UserName = dto.Email
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            return result;
        }
        /// <summary>
        /// Attempts to sign in a user with the provided credentials and returns a JWT on success.
        /// </summary>
        /// <param name="dto">Data transfer object containing login credentials.</param>
        /// <returns>
        /// A JWT string if login succeeds; otherwise <c>null</c>.
        /// </returns>
        public async Task<string?> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || string.IsNullOrWhiteSpace(user.UserName))
            {
                return null;
            }

            var signInResult = await _signInManager.PasswordSignInAsync(
              user.UserName,
              dto.Password,
              isPersistent: false,
              lockoutOnFailure: false);

            if (!signInResult.Succeeded)
            {
                return null;
            }

            // generate and return JWT token (await here!)
            return await GenerateJwtToken(user);
        }

        /// <summary>
        /// Generates a JWT token for the specified user based on configuration settings.
        /// </summary>
        /// <param name="user">The authenticated user.</param>
        /// <returns>A JWT string.</returns>
        // AuthService.cs  – GenerateJwtToken
        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var jwt = _configuration.GetSection("Jwt").Get<JwtSettings>();
            var key = Encoding.UTF8.GetBytes(jwt.Key);

            var claims = new List<Claim> {
        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim("id", user.Id)
      };

            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityTokenHandler().CreateToken(
              new SecurityTokenDescriptor
              {
                  Subject = new ClaimsIdentity(claims),
                  Expires = DateTime.UtcNow.AddMinutes(jwt.ExpiresInMinutes),
                  Issuer = jwt.Issuer,
                  Audience = jwt.Audience,
                  SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256)
              });

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Initiates a password reset process by generating a reset token and (optionally) emailing it.
        /// </summary>
        /// <param name="dto">Data transfer object containing the user's email.</param>
        /// <returns>A successful <see cref="IdentityResult"/> regardless of email existence.</returns>
        //Forgot password: generate a password reset token and  TODO: email it
        public async Task<IdentityResult> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // For security, do not reveal user absence
                return IdentityResult.Success;
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            // TODO: send token via email
            Console.WriteLine($"Password reset token for {user.Email}: {resetToken}");

            return IdentityResult.Success;
        }

        /// <summary>
        /// Resets the user's password using a valid reset token and new password.
        /// </summary>
        /// <param name="dto">Data transfer object containing reset token and new password details.</param>
        /// <returns>
        /// An <see cref="IdentityResult"/> indicating success or failure.
        /// </returns>
        //reset the users password using the provided token
        public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                var error = new IdentityError
                {
                    Code = "UserNotFound",
                    Description = "Could not find the user."
                };
                return IdentityResult.Failed(error);
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                var error = new IdentityError
                {
                    Code = "PasswordMismatch",
                    Description = "New password and confirmation do not match."
                };
                return IdentityResult.Failed(error);
            }

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            return result;
        }
    }
}