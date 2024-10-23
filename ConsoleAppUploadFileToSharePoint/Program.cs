using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ConsoleAppUploadFileToSharePoint
{
  internal class Program
  {
    private static string clientId = "YOUR_CLIENT_ID";
    private static string tenantId = "YOUR_TENANT_ID";
    private static string clientSecret = "YOUR_CLIENT_SECRET";
    private static string siteId = "YOUR_SITE_ID";
    private static string driveId = "YOUR_DRIVE_ID"; // usually, it's your document library

    static void Main()
    {
      Task.Run(async () =>
      {
        string filePath = @"path\to\your\file.csv";
        string fileName = "yourfile.csv";

        var token = await GetAccessToken();

        await ListDocumentsInSharePoint(token);

        await UploadFileToSharePoint(token, filePath, fileName);

        Console.WriteLine("File uploaded successfully.");
      }).GetAwaiter().GetResult();
      Console.WriteLine("Press any key to exit:");
      Console.ReadKey();
    }

    private static async Task<string> GetAccessToken()
    {
      var app = ConfidentialClientApplicationBuilder.Create(clientId)
          .WithClientSecret(clientSecret)
          .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
          .Build();

      var result = await app.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" }).ExecuteAsync();
      return result.AccessToken;
    }

    private static async Task ListDocumentsInSharePoint(string token)
    {
      using (var client = new HttpClient())
      {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync($"https://graph.microsoft.com/v1.0/sites/{siteId}/drives/{driveId}/root/children");

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var json = JToken.Parse(content);

        Console.WriteLine("Documents in SharePoint:");

        foreach (var item in json["value"])
        {
          Console.WriteLine(item["name"]);
        }
      }
    }

    private static async Task UploadFileToSharePoint(string token, string filePath, string fileName)
    {
      using (var client = new HttpClient())
      {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new ByteArrayContent(System.IO.File.ReadAllBytes(filePath));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await client.PutAsync(
            $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives/{driveId}/root:/{fileName}:/content", content);

        response.EnsureSuccessStatusCode();
      }
    }
  }
}
