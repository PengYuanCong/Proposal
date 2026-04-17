using Proposal.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Microsoft.Extensions.Options;

public class MediaController : Controller
{

    private readonly string _apiKey;





    public MediaController(IConfiguration configuration)
    {
        // 從設定檔讀取
        _apiKey = configuration["YouTubeSettings:ApiKey"];
    }



    public IActionResult Highlights()
    {
        return View();
    }


// 精彩操作首頁
[HttpGet]
    public async Task<IActionResult> GetYouTubeVideos(string query)
    {
        if (string.IsNullOrEmpty(query)) return BadRequest("請輸入關鍵字");

        // 此時這裡的 _apiKey 就不會報錯了，因為它已經定義在上面
        try
        {
            using (var client = new HttpClient())
            {
                string url = $"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=5&q={Uri.EscapeDataString(query)}&type=video&key={_apiKey}";

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }
                else
                {
                    // 這裡可以幫你檢查為什麼 API 沒過 (例如 Key 錯了)
                    return StatusCode((int)response.StatusCode, content);
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}

