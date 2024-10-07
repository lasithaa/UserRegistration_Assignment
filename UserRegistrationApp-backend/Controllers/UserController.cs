﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using UserRegistrationApp.Data;
using UserRegistrationApp.Models;


namespace UserRegistrationApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly MailService _mailService; 


        public UserController(AppDbContext context, MailService mailService)
        {
            _context = context;
            _mailService = mailService; 
        }
        // User Login Endpoint
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO userDto)
        {
            if (userDto == null || string.IsNullOrEmpty(userDto.Email) || string.IsNullOrEmpty(userDto.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(userDto.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            return Ok(new { message = "User logged in successfully", userId = user.Id, email = user.Email });
        }



        // User Registration Endpoint
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserDto userDto)
        {
            var user = new User
            {
                Email = userDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                DateOfBirth = userDto.DateOfBirth,
                IdentityCardNumber = userDto.IdentityCardNumber,
                ProfilePictureUrl = "/images/default-profile.png"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully" });
        }

        // Reset Password Endpoint
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromForm] string email, [FromForm] string token, [FromForm] string newPassword)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email && u.ResetToken == token);
            if (user == null || user.OtpExpirationTime < DateTime.UtcNow)
            {
                return BadRequest("Invalid or expired reset token.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            user.ResetToken = null;
            user.OtpExpirationTime = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successfully." });
        }


        //get user data
        // User Profile Endpoint
        [HttpGet("profile/{id}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            // Step 1: Fetch the user by id from the database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                // If the user does not exist, return a 404 Not Found response
                return NotFound(new { message = "User not found" });
            }

            // Step 2: Map User entity to ProfileDTO
            var profileDto = new ProfileDTO
            {
                Id = user.Id,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Email = user.Email
            };

            // Step 3: Return the profile data
            return Ok(profileDto);
        }




        // Profile Picture Upload Endpoint
        [HttpPost("upload-profile-picture")]
        public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile profilePicture, [FromForm] int userId)
        {
            if (profilePicture == null || profilePicture.Length == 0)
            {
                return BadRequest("Please upload a valid image.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
            }

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(profilePicture.FileName)}";
            var filePath = Path.Combine(imagesPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(stream);
            }

            user.ProfilePictureUrl = $"/images/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile picture uploaded successfully", imageUrl = user.ProfilePictureUrl });
        }

        // Send Reset Password OTP Endpoint
        [HttpPost("send-reset-password-link")]
        public async Task<IActionResult> SendResetPasswordLink([FromForm] string email)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var token = Guid.NewGuid().ToString();

            user.ResetToken = token;
            user.OtpExpirationTime = DateTime.UtcNow.AddMinutes(15);

            await _context.SaveChangesAsync();

            var resetLink = $"http://127.0.0.1:5500/resetPage.html?token={token}&email={user.Email}";

            var emailContent = $@"
                <html>
                <head>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            background-color: #f4f4f4;
                            margin: 0;
                            padding: 0;
                            font-size: 16px;
                            line-height: 1.6;
                        }}
                        .container {{
                            max-width: 600px;
                            margin: 20px auto;
                            background-color: #ffffff;
                            padding: 20px;
                            border-radius: 8px;
                            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
                        }}
                        h1 {{
                            color: #333;
                        }}
                        p {{
                            color: #555;
                        }}
                        .footer {{
                            margin-top: 30px;
                            font-size: 12px;
                            color: #888;
                            text-align: center;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1>Password Reset</h1>
                        <p>Hello Akalanka,</p>
                        <p>You requested to reset your password. Please click the button below to reset it. The link will expire in 15 minutes.</p>
        
                        <!-- Inline styles for the button -->
                        <a href='{resetLink}' 
                           style='display: inline-block; padding: 15px 30px; font-size: 18px; 
                                  color: #ffffff; background-color: #007bff; 
                                  background: linear-gradient(135deg, #007bff, #0056b3); 
                                  text-decoration: none; border-radius: 5px; text-align: center; 
                                  font-weight: bold; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); 
                                  transition: background 0.3s ease, transform 0.3s ease;'>
                            Reset Password
                        </a>

                        <p>If you did not request a password reset, you can ignore this email.</p>
                        <div class='footer'>
                            <p>Thank you,</p>
                            <p>Loving Vidu</p>
                        </div>
                    </div>
                </body>
                </html>";

            _mailService.SendEmail(user.Email, "Reset Password", emailContent);

            return Ok(new { message = "Password reset link sent to email." });
        }

    }
}
