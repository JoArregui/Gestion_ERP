using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    static async Task Main()
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("http://localhost:5109/");
        try
        {
            var res = await client.GetFromJsonAsync<List<object>>("api/articulos");
            Console.WriteLine("OK: " + (res?.Count ?? 0));
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
