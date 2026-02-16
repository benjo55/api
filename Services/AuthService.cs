using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace api.Services
{
    public class AuthService
    {
        private readonly IConfiguration _config;

        public AuthService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateJwtToken(List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key")));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"], // ⚠️ Doit correspondre à `Program.cs`
                audience: _config["Jwt:Audience"], // ⚠️ Doit correspondre à `Program.cs`
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            Console.WriteLine("🔑 Token généré :", tokenString); // ✅ Vérification
            return tokenString;
        }


        // public string GenerateJwtToken(List<Claim> claims) // ✅ Acceptation de List<Claim>
        // {
        //     var jwtKey = _config["Jwt:Key"];
        //     if (string.IsNullOrEmpty(jwtKey))
        //     {
        //         throw new ArgumentNullException("Jwt:Key", "JWT key is not configured.");
        //     }
        //     var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        //     var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //     var issuer = _config["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer");
        //     var audience = _config["Jwt:Audience"] ?? issuer; // Utilise Issuer si Audience est vide

        //     Console.WriteLine("✅ Génération JWT - Issuer: " + issuer);
        //     Console.WriteLine("✅ Génération JWT - Audience: " + audience);

        //     var token = new JwtSecurityToken(
        //         issuer,
        //         audience,
        //         claims,
        //         expires: DateTime.UtcNow.AddHours(2),
        //         signingCredentials: credentials
        //     );
        //     return new JwtSecurityTokenHandler().WriteToken(token);
        // }
    }
}
