using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var secret = "clave-local-temporal-de-al-menos-32-caracteres";
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "test@erp.local"),
            new Claim("EmpresaId", "1"),
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "test@erp.local")
        };

        var token = new JwtSecurityToken(
            issuer: "ERP.Api",
            audience: "ERP.Web",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        Console.WriteLine("Generated token (first 50 chars): " + tokenString.Substring(0, 50));

        using var client = new HttpClient();
        client.BaseAddress = new Uri("http://localhost:5109/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

        try
        {
            var res = await client.GetFromJsonAsync<List<object>>("api/articulos");
            Console.WriteLine("OK, count: " + (res?.Count ?? 0));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception Type: " + ex.GetType().FullName);
            Console.WriteLine("Message: " + ex.Message);
            if (ex.InnerException != null)
                Console.WriteLine("Inner: " + ex.InnerException.GetType().FullName + " - " + ex.InnerException.Message);
        }
    }
}
