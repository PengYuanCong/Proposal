using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Proposal.Models;
using System.Collections.Generic;

namespace Proposal.Controllers
{
    [Authorize]
    public class CalculatorController : Controller
    {
        private readonly IConfiguration _config;
        public CalculatorController(IConfiguration config) { _config = config; }

        [HttpGet]
        public IActionResult Index()
        {
            List<Equipment> myEquipments = new List<Equipment>();
            List<Loadout> myLoadouts = new List<Loadout>(); // 新增：用於存放組合清單

            string connString = _config.GetConnectionString("DefaultConnection");
            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();

                // 1. 抓取單件裝備清單 (你原本的邏輯)
                string sqlEq = "SELECT Id, Name FROM Equipments WHERE Username = @User";
                using (SqlCommand cmd = new SqlCommand(sqlEq, cn))
                {
                    cmd.Parameters.AddWithValue("@User", User.Identity.Name);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            myEquipments.Add(new Equipment { Id = (int)dr["Id"], Name = dr["Name"].ToString() });
                        }
                    }
                }

                // 2. 新增：抓取「裝備組合」清單 (讓下拉選單可以選)
                string sqlLo = "SELECT Id, LoadoutName FROM Loadouts WHERE Username = @User";
                using (SqlCommand cmd = new SqlCommand(sqlLo, cn))
                {
                    cmd.Parameters.AddWithValue("@User", User.Identity.Name);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            myLoadouts.Add(new Loadout { Id = (int)dr["Id"], LoadoutName = dr["LoadoutName"].ToString() });
                        }
                    }
                }
            }

            // 將資料傳給 View
            ViewBag.EquipmentList = myEquipments;
            ViewBag.LoadoutList = myLoadouts; // 傳送組合清單
            return View();
        }

        // 3. 新增：API Action，讓前端點選組合後，能抓取「總合數值」
        [HttpGet]
        public IActionResult GetLoadoutStats(int loadoutId)
        {
            string connString = _config.GetConnectionString("DefaultConnection");
            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();
                // 使用 CROSS APPLY 一次加總六件裝備的數值
                string sql = @"
                    SELECT 
                        SUM(e.HP) as TotalHP, 
                        SUM(e.Attack) as TotalAttack, 
                        SUM(e.MagicAttack) as TotalMagicAttack,
                        SUM(e.PhysicalDefense) as TotalPD,
                        SUM(e.MagicDefense) as TotalMD,
                        SUM(e.Price) as TotalPrice
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
                                hp = dr["TotalHP"] == DBNull.Value ? 0 : dr["TotalHP"],
                                attack = dr["TotalAttack"] == DBNull.Value ? 0 : dr["TotalAttack"],
                                magicAttack = dr["TotalMagicAttack"] == DBNull.Value ? 0 : dr["TotalMagicAttack"],
                                pDef = dr["TotalPD"] == DBNull.Value ? 0 : dr["TotalPD"],
                                mDef = dr["TotalMD"] == DBNull.Value ? 0 : dr["TotalMD"],
                                price = dr["TotalPrice"] == DBNull.Value ? 0 : dr["TotalPrice"]
                            });
                        }
                    }
                }
            }
            return NotFound();
        }

        // --- 你原本的 Calculate 和 SaveRecord 保持不變 ---
        [HttpPost]
        public IActionResult Calculate(string formulaType, double param1, double param2, double param3)
        {
            // ... (維持你原本的邏輯)
            double result = 0;
            string message = "";
            switch (formulaType) { /* 你的公式邏輯 */ }
            ViewBag.Result = message;
            ViewBag.FormulaType = formulaType;
            return View("Index");
        }

        [HttpPost]
        public IActionResult SaveRecord(string type, string inputs, string result)
        {
            // ... (維持你原本的邏輯)
            return Json(new { success = true });
        }
    }
}