using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;

namespace ConsoleAppUploadFileToSharePoint
{
  internal class Program
  {
    private static string clientId = "YOUR_CLIENT_ID";
    private static string tenantId = "YOUR_TENANT_ID";
    private static string clientSecret = "YOUR_CLIENT_SECRET";
    //private static string siteId = "YOUR_SITE_ID";
    //private static string driveId = "YOUR_DRIVE_ID"; // usually, it's your document library

    static async Task Main()
    {
      string filePath = @"TextFile1.txt";
      string fileName = "test18_upload.txt";
      var token = await GetAccessToken(tenantId, clientSecret, clientId);

      Console.WriteLine("Calcul du siteId...");
      var siteId = await GetSiteId(token, "companyName", "NameOfYourdocumentLibrary");

      Console.WriteLine("Calcul du driveId...");
      var driveId = await GetDriveId(token, siteId);
      Console.WriteLine("********************************");
      Console.WriteLine("voici les fichiers avant upload");
      Console.WriteLine("********************************");
      await ListDocumentsInSharePoint(token, siteId, driveId);
      await UploadFileToSharePoint(token, filePath, fileName, siteId, driveId);
      Console.WriteLine("********************************");
      Console.WriteLine("voici les fichiers après upload");
      Console.WriteLine("********************************");
      await ListDocumentsInSharePoint(token, siteId, driveId);
      Console.WriteLine($"Le fichier {fileName} a été uploadé correctement.");

      Console.WriteLine("Press any key to exit:");
      Console.ReadKey();
    }

    private static async Task<string> GetAccessToken(string tenantId, string clientSecret, string clientId)
    {
      var app = ConfidentialClientApplicationBuilder.Create(clientId)
          .WithClientSecret(clientSecret)
          .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
          .Build();

      var result = await app.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" }).ExecuteAsync();
      return result.AccessToken;
    }

    private static async Task ListDocumentsInSharePoint(string token, string siteId, string driveId)
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

    private static async Task UploadFileToSharePoint(string token, string filePath, string fileName, string siteId, string driveId)
    {
      using (var client = new HttpClient())
      {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new ByteArrayContent(File.ReadAllBytes(filePath));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await client.PutAsync(
            $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives/{driveId}/root:/{fileName}:/content", content);

        response.EnsureSuccessStatusCode();
      }
    }

    private static async Task<string> GetSiteId(string token, string companyName, string sharePointName)
    {
      using (var client = new HttpClient())
      {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync($"https://graph.microsoft.com/v1.0/sites/{companyName}.sharepoint.com:/sites/{sharePointName}");
        if (!response.IsSuccessStatusCode)
        {
          var errorContent = await response.Content.ReadAsStringAsync();
          throw new HttpRequestException($"HTTP error: {response.StatusCode}, Details: {errorContent}");
        }
        var content = await response.Content.ReadAsStringAsync();
        var json = JToken.Parse(content);
        string siteId = json["id"].ToString();
        return siteId;
      }
    }

    private static async Task<string> GetDriveId(string token, string siteId)
    {
      using (var client = new HttpClient())
      {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync($"https://graph.microsoft.com/v1.0/sites/{siteId}/drives");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var json = JToken.Parse(content);
        string driveId = json["value"][0]["id"].ToString();
        return driveId;
      }
    }
  }
}
