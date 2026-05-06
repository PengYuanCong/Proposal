using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System;
using Proposal.Models; // 這裡用你的 Proposal
using Microsoft.AspNetCore.Authorization;
using ClosedXML.Excel;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Proposal.Controllers // 這裡用你的 Proposal
{
    [Authorize]
    public class EquipmentController : Controller
    {
        private readonly IConfiguration _config;

        public EquipmentController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public IActionResult Index(string searchString)
        {
            List<Equipment> equipmentList = new List<Equipment>();
            string connString = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();

                // 基礎 SQL：確保資料隔離，只抓本人的裝備
                string sql = "SELECT * FROM Equipments WHERE Username = @User";

                // 如果使用者有輸入查詢文字，就動態加上名稱過濾條件
                if (!string.IsNullOrEmpty(searchString))
                {
                    sql += " AND Name LIKE @Search";
                }

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    // 綁定本人的帳號
                    cmd.Parameters.AddWithValue("@User", User.Identity.Name);

                    // 如果有輸入查詢文字，綁定 LIKE 參數 (使用 % 來達成模糊搜尋)
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        cmd.Parameters.AddWithValue("@Search", "%" + searchString + "%");
                    }

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Equipment equipment = new Equipment();
                            equipment.Id = Convert.ToInt32(reader["Id"]);
                            equipment.Name = reader["Name"].ToString();
                            equipment.HP = Convert.ToInt32(reader["HP"]);
                            equipment.Attack = Convert.ToInt32(reader["Attack"]);
                            equipment.MagicAttack = Convert.ToInt32(reader["MagicAttack"]);
                            equipment.PhysicalDefense = Convert.ToInt32(reader["PhysicalDefense"]);
                            equipment.MagicDefense = Convert.ToInt32(reader["MagicDefense"]);
                            equipment.Price = Convert.ToInt32(reader["Price"]);

                            equipmentList.Add(equipment);
                        }
                    }
                }
            }

            // 將使用者剛剛輸入的字傳回前端，讓輸入框能保留搜尋紀錄
            ViewData["CurrentFilter"] = searchString;
            return View(equipmentList);
        }



        // 1. 負責「顯示」新增表單的畫面 (GET 請求)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 2. 負責「接收」表單送出的資料，並寫入資料庫 (POST 請求)
        [HttpPost]
        public IActionResult Create(Equipment model)
        {
            // 取得連線字串
            string connString = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();

                // 【面試大加分技巧】：使用 @ 參數化查詢，可以防止駭客進行 SQL Injection 攻擊！
                string sql = @"INSERT INTO Equipments 
               (Username, Name, HP, Attack, MagicAttack, PhysicalDefense, MagicDefense, Price) 
               VALUES 
               (@User, @Name, @HP, @Attack, @MagicAttack, @PhysicalDefense, @MagicDefense, @Price)";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    // 將模型裡面的資料，綁定到 SQL 語法的參數上
                    cmd.Parameters.AddWithValue("@User", User.Identity.Name);
                    cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
                    cmd.Parameters.AddWithValue("@HP", model.HP);
                    cmd.Parameters.AddWithValue("@Attack", model.Attack);
                    cmd.Parameters.AddWithValue("@MagicAttack", model.MagicAttack);
                    cmd.Parameters.AddWithValue("@PhysicalDefense", model.PhysicalDefense);
                    cmd.Parameters.AddWithValue("@MagicDefense", model.MagicDefense);
                    cmd.Parameters.AddWithValue("@Price", model.Price);

                    // 執行資料庫寫入動作
                    cmd.ExecuteNonQuery();
                }
            }

            // 寫入成功後，把使用者導回裝備列表頁 (Index) 看最新結果
            return RedirectToAction("Index");
        }



        // --- 修改裝備 ---
        // 1. 負責顯示「修改」表單的畫面 (根據 ID 抓出舊資料)(防止其他人更改id修改別人資料 已改~~)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            Equipment equipment = new Equipment();
            string connString = _config.GetConnectionString("DefaultConnection"); // 記得確認連線字串名稱！

            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();
                string sql = "SELECT * FROM Equipments WHERE Id = @Id AND Username = @User";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@User", User.Identity.Name);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            equipment.Id = Convert.ToInt32(reader["Id"]);
                            equipment.Name = reader["Name"].ToString();
                            equipment.HP = Convert.ToInt32(reader["HP"]);
                            equipment.Attack = Convert.ToInt32(reader["Attack"]);
                            equipment.MagicAttack = Convert.ToInt32(reader["MagicAttack"]);
                            equipment.PhysicalDefense = Convert.ToInt32(reader["PhysicalDefense"]);
                            equipment.MagicDefense = Convert.ToInt32(reader["MagicDefense"]);
                            equipment.Price = Convert.ToInt32(reader["Price"]);
                        }
                        else
                        {
                            // 💡 防駭客機制：如果撈不到資料 (可能是裝備不存在，或是他想偷改別人的)
                            // 直接把他踢回首頁，不給他看修改畫面！
                            return RedirectToAction("Index");
                        }
                    }
                }
            }
            return View(equipment); // 把舊資料傳給修改畫面
        }

        // 2. 負責接收使用者改好的資料，並 UPDATE 進資料庫
        [HttpPost]
        public IActionResult Edit(Equipment model)
        {
            string connString = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();
                string sql = @"UPDATE Equipments SET 
                       Name = @Name, HP = @HP, Attack = @Attack, 
                       MagicAttack = @MagicAttack, PhysicalDefense = @PhysicalDefense, 
                       MagicDefense = @MagicDefense, Price = @Price 
                       WHERE Id = @Id AND Username = @User";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    // 不要忘記綁定 Id 參數，不然系統不知道要更新哪一筆！
                    cmd.Parameters.AddWithValue("@Id", model.Id);
                    cmd.Parameters.AddWithValue("@User", User.Identity.Name);
                    cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
                    cmd.Parameters.AddWithValue("@HP", model.HP);
                    cmd.Parameters.AddWithValue("@Attack", model.Attack);
                    cmd.Parameters.AddWithValue("@MagicAttack", model.MagicAttack);
                    cmd.Parameters.AddWithValue("@PhysicalDefense", model.PhysicalDefense);
                    cmd.Parameters.AddWithValue("@MagicDefense", model.MagicDefense);
                    cmd.Parameters.AddWithValue("@Price", model.Price);

                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }

        // --- 刪除裝備 ---
        // 點擊刪除後直接執行 DELETE 動作
        public IActionResult Delete(int id)
        {
            string connString = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();
                string sql = "DELETE FROM Equipments WHERE Id = @Id AND Username = @User";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@User", User.Identity.Name);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ExportExcel()
        {
            string connString = _config.GetConnectionString("DefaultConnection");

            // 建立一個新的 Excel 工作簿
            using (var workbook = new XLWorkbook())
            {
                // 新增一個工作表
                var worksheet = workbook.Worksheets.Add("我的裝備清單");

                // 設定第一列的標題 (固定格式)
                worksheet.Cell(1, 1).Value = "裝備名稱";
                worksheet.Cell(1, 2).Value = "HP";
                worksheet.Cell(1, 3).Value = "物理攻擊";
                worksheet.Cell(1, 4).Value = "魔法攻擊";
                worksheet.Cell(1, 5).Value = "物理防禦";
                worksheet.Cell(1, 6).Value = "魔法防禦";
                worksheet.Cell(1, 7).Value = "價格";

                // 把標題列變粗體加底色
                var headerRange = worksheet.Range("A1:G1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                int currentRow = 2; // 從第二列開始填資料

                using (SqlConnection cn = new SqlConnection(connString))
                {
                    cn.Open();
                    // 只抓自己的裝備！
                    string sql = "SELECT * FROM Equipments WHERE Username = @User";
                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@User", User.Identity.Name);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                worksheet.Cell(currentRow, 1).Value = reader["Name"].ToString();
                                worksheet.Cell(currentRow, 2).Value = Convert.ToInt32(reader["HP"]);
                                worksheet.Cell(currentRow, 3).Value = Convert.ToInt32(reader["Attack"]);
                                worksheet.Cell(currentRow, 4).Value = Convert.ToInt32(reader["MagicAttack"]);
                                worksheet.Cell(currentRow, 5).Value = Convert.ToInt32(reader["PhysicalDefense"]);
                                worksheet.Cell(currentRow, 6).Value = Convert.ToInt32(reader["MagicDefense"]);
                                worksheet.Cell(currentRow, 7).Value = Convert.ToInt32(reader["Price"]);
                                currentRow++;
                            }
                        }
                    }
                }

                // 自動調整欄寬
                worksheet.Columns().AdjustToContents();

                // 準備將檔案回傳給使用者下載
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"裝備清單_{DateTime.Now:yyyyMMdd}.xlsx";

                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        [HttpPost]
        public IActionResult ImportExcel(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length <= 0)
            {
                TempData["Error"] = "請選擇要上傳的 Excel 檔案！";
                return RedirectToAction("Index");
            }

            string connString = _config.GetConnectionString("DefaultConnection");

            using (var stream = new MemoryStream())
            {
                excelFile.CopyTo(stream);
                // 讀取上傳的 Excel 檔案
                using (var workbook = new XLWorkbook(stream))
                {
                    // 抓取第一個工作表
                    var worksheet = workbook.Worksheet(1);
                    // 找出總共有幾列資料
                    var rowCount = worksheet.LastRowUsed().RowNumber();

                    using (SqlConnection cn = new SqlConnection(connString))
                    {
                        cn.Open();
                        // 假設第一列是標題，所以我們從第 2 列開始讀取資料 (row = 2)
                        for (int row = 2; row <= rowCount; row++)
                        {
                            // 讀取第一格的裝備名稱，如果是空的就跳過這行
                            string name = worksheet.Cell(row, 1).Value.ToString();
                            if (string.IsNullOrWhiteSpace(name)) continue;

                            // 準備 SQL 寫入語法
                            string sql = @"INSERT INTO Equipments 
                                           (Username, Name, HP, Attack, MagicAttack, PhysicalDefense, MagicDefense, Price) 
                                           VALUES 
                                           (@User, @Name, @HP, @Attack, @MagicAttack, @PhysicalDefense, @MagicDefense, @Price)";

                            using (SqlCommand cmd = new SqlCommand(sql, cn))
                            {
                                // 綁定目前登入者的帳號
                                cmd.Parameters.AddWithValue("@User", User.Identity.Name);
                                cmd.Parameters.AddWithValue("@Name", name);

                                // 使用 GetValue<int>() 安全地將 Excel 的數字轉為 C# 的 int
                                // 如果 Excel 裡面是空的，給預設值 0 防呆
                                cmd.Parameters.AddWithValue("@HP", worksheet.Cell(row, 2).IsEmpty() ? 0 : worksheet.Cell(row, 2).GetValue<int>());
                                cmd.Parameters.AddWithValue("@Attack", worksheet.Cell(row, 3).IsEmpty() ? 0 : worksheet.Cell(row, 3).GetValue<int>());
                                cmd.Parameters.AddWithValue("@MagicAttack", worksheet.Cell(row, 4).IsEmpty() ? 0 : worksheet.Cell(row, 4).GetValue<int>());
                                cmd.Parameters.AddWithValue("@PhysicalDefense", worksheet.Cell(row, 5).IsEmpty() ? 0 : worksheet.Cell(row, 5).GetValue<int>());
                                cmd.Parameters.AddWithValue("@MagicDefense", worksheet.Cell(row, 6).IsEmpty() ? 0 : worksheet.Cell(row, 6).GetValue<int>());
                                cmd.Parameters.AddWithValue("@Price", worksheet.Cell(row, 7).IsEmpty() ? 0 : worksheet.Cell(row, 7).GetValue<int>());

                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }

            TempData["Success"] = "🎉 Excel 資料匯入成功！";
            return RedirectToAction("Index");
        }




        // 讓前端可以透過 ID 抓取特定裝備的數值
        [HttpGet]
        public IActionResult GetEquipmentDetails(int id)
        {
            string connString = _config.GetConnectionString("DefaultConnection");
            Equipment eq = null;

            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();
                string sql = "SELECT * FROM Equipments WHERE Id = @Id AND Username = @User";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@User", User.Identity.Name);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            eq = new Equipment
                            {
                                HP = Convert.ToInt32(dr["HP"]),
                                Attack = Convert.ToInt32(dr["Attack"]),
                                MagicAttack = Convert.ToInt32(dr["MagicAttack"]),
                                PhysicalDefense = Convert.ToInt32(dr["PhysicalDefense"]),
                                MagicDefense = Convert.ToInt32(dr["MagicDefense"]),
                                // 👇 就是少了這一行！請把價格也包裝進去
                                Price = Convert.ToInt32(dr["Price"])
                            };
                        }
                    }
                }
            }
            return Json(eq);
        }


    

    [HttpPost]
        public IActionResult SaveLoadout(string loadoutName, List<int> equipmentIds)
        {
            if (string.IsNullOrEmpty(loadoutName) || equipmentIds == null || equipmentIds.Count == 0)
            {
                return Json(new { success = false, message = "請輸入組合名稱並至少選擇一件裝備！" });
            }

            string connString = _config.GetConnectionString("DefaultConnection");
            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();
                string sql = @"INSERT INTO Loadouts (Username, LoadoutName, Eq1_Id, Eq2_Id, Eq3_Id, Eq4_Id, Eq5_Id, Eq6_Id) 
                       VALUES (@User, @LName, @E1, @E2, @E3, @E4, @E5, @E6)";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@User", User.Identity.Name);
                    cmd.Parameters.AddWithValue("@LName", loadoutName);

                    // 確保最多填入六件，不足的填 DBNull
                    for (int i = 1; i <= 6; i++)
                    {
                        if (i <= equipmentIds.Count)
                            cmd.Parameters.AddWithValue($"@E{i}", equipmentIds[i - 1]);
                        else
                            cmd.Parameters.AddWithValue($"@E{i}", DBNull.Value);
                    }
                    cmd.ExecuteNonQuery();
                }
            }
            return Json(new { success = true, message = "組合儲存成功！" });
        }



        [HttpGet]
        public IActionResult Search(string query)
        {
            // 如果沒有輸入任何關鍵字，直接導回首頁
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("Index", "Home");
            }

            List<Equipment> searchResults = new List<Equipment>();
            string connString = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();
                // 模糊搜尋名稱或包含該關鍵字的裝備，同樣要確保 Username 隔離
                string sql = "SELECT * FROM Equipments WHERE Name LIKE @q AND Username = @User";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@q", "%" + query + "%");
                    cmd.Parameters.AddWithValue("@User", User.Identity.Name);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            searchResults.Add(new Equipment
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                HP = Convert.ToInt32(reader["HP"]),
                                Attack = Convert.ToInt32(reader["Attack"]),
                                MagicAttack = Convert.ToInt32(reader["MagicAttack"]),
                                PhysicalDefense = Convert.ToInt32(reader["PhysicalDefense"]),
                                MagicDefense = Convert.ToInt32(reader["MagicDefense"]),
                                Price = Convert.ToInt32(reader["Price"])
                            });
                        }
                    }
                }
            }

            // 將搜尋關鍵字帶到 View，方便顯示「您搜尋的是：XXX」
            ViewData["SearchTerm"] = query;
            return View(searchResults);
        }



        // 取得特定組合的總數值 (用於計算公式)
        [HttpGet]
        public IActionResult GetLoadoutStats(int loadoutId)
        {
            string connString = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();
                // 使用 CROSS APPLY 一次性計算出該組合內 6 個欄位對應裝備的總和
                string sql = @"
            SELECT 
                ISNULL(SUM(e.HP), 0) as TotalHP, 
                ISNULL(SUM(e.Attack), 0) as TotalAtk, 
                ISNULL(SUM(e.MagicAttack), 0) as TotalMAtk,
                ISNULL(SUM(e.PhysicalDefense), 0) as TotalPDef,
                ISNULL(SUM(e.MagicDefense), 0) as TotalMDef,
                ISNULL(SUM(e.Price), 0) as TotalPrice
            FROM Loadouts l
            CROSS APPLY (
                SELECT HP, Attack, MagicAttack, PhysicalDefense, MagicDefense, Price 
                FROM Equipments 
                WHERE Id IN (l.Eq1_Id, l.Eq2_Id, l.Eq3_Id, l.Eq4_Id, l.Eq5_Id, l.Eq6_Id)
            ) e
            WHERE l.Id = @LId AND l.Username = @User";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@LId", loadoutId);
                    cmd.Parameters.AddWithValue("@User", User.Identity.Name);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            return Json(new
                            {
                                hp = dr["TotalHP"],
                                attack = dr["TotalAtk"],
                                magicAttack = dr["TotalMAtk"],
                                pDef = dr["TotalPDef"],
                                mDef = dr["TotalMDef"],
                                price = dr["TotalPrice"]
                            });
                        }
                    }
                }
            }
            return NotFound();
        }


    }
}
