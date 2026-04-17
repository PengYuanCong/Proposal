using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration; // 確保有這個
using Microsoft.Data.SqlClient;
using Proposal.Models;
using System.Collections.Generic;
using System;

namespace Proposal.Controllers // 確認是你的專案名稱
{
    [Authorize] // 保護這個 Controller，沒登入進不來
    public class UserController : Controller
    {
        private readonly IConfiguration _config;
        public UserController(IConfiguration config) { _config = config; }


        // 👇 這是負責接收並儲存大頭貼的方法
        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
        {
            // 檢查使用者有沒有真的選取檔案
            if (avatarFile != null && avatarFile.Length > 0)
            {
                // 設定圖片要存檔的資料夾：專案目錄下的 wwwroot/avatars
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");

                // 如果資料夾不存在，系統會自動建立一個
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // 將圖片強制命名為「使用者帳號.jpg」(例如：Peng.jpg)
                // 這樣每次上傳新照片就會自動覆蓋舊照片，不用改資料庫！
                var fileName = User.Identity.Name + ".jpg";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // 將上傳的檔案存入伺服器
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }
            }

            // 存檔完成後，重新載入會員頁面
            return RedirectToAction("Profile");
        }



        public IActionResult Profile()
        {
            List<CalculationHistory> historyList = new List<CalculationHistory>();

            ViewBag.Username = User.Identity?.Name;


            // 現在它終於能拿到跟 Calculator 一模一樣的連線字串了
            string connString = _config.GetConnectionString("DefaultConnection");
            try
            {
                using (SqlConnection cn = new SqlConnection(connString))
                {
                    cn.Open();
                    // 成功連線雷達
                    ViewBag.DbInfo = $"🟢 連線成功！伺服器：{cn.DataSource}";

                    // 💡 無敵防呆 SQL：不管大小寫、不管前後空白，通通比對！
                    string sql = @"SELECT * FROM CalculationHistory 
                                   WHERE LOWER(LTRIM(RTRIM(Username))) = LOWER(LTRIM(RTRIM(@User))) 
                                   ORDER BY CreatedAt DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@User", User.Identity.Name ?? "");

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                historyList.Add(new CalculationHistory
                                {
                                    FormulaType = dr["FormulaType"]?.ToString() ?? "未知公式",
                                    InputDetails = dr["InputDetails"]?.ToString() ?? "無數據",
                                    ResultContent = dr["ResultContent"]?.ToString() ?? "無結果",
                                    CreatedAt = dr["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(dr["CreatedAt"]) : DateTime.Now
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 🔴 如果有任何連線或讀取錯誤，直接顯示在網頁上給你看
                ViewBag.DbInfo = $"🔴 發生錯誤：{ex.Message}";
            }

            return View(historyList);
        }
    }

}
