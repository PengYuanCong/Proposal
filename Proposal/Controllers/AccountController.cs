using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Proposal.Controllers // 確認你的專案名稱
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;
        public AccountController(IConfiguration config) { _config = config; }

        // 1. 顯示登入畫面 (GET)
        [HttpGet]
        public IActionResult Login()
        {
            // 如果已經登入了，就直接踢回首頁，不用再登入一次
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // 2. 處理登入表單送出 (POST)
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            bool isValidUser = false;
            string connString = _config.GetConnectionString("DefaultConnection"); // 記得確認你的連線字串名稱！

            // 去資料庫檢查帳號密碼
            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();
                string sql = "SELECT * FROM Users WHERE Username = @User AND Password = @Pass";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@User", username);
                    cmd.Parameters.AddWithValue("@Pass", password);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) { isValidUser = true; }
                    }
                }
            }

            if (isValidUser)
            {
                // 登入成功！開始製作「身分證 (Claims)」
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim("Role", "Admin") // 可以自訂權限角色
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // 正式發放 Cookie 通行證給瀏覽器！
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home"); // 登入成功，導向首頁
            }

            // 登入失敗，把錯誤訊息傳回畫面
            ViewBag.Error = "帳號或密碼錯誤，請重新輸入！";
            return View();
        }



        // --- 註冊功能 ---

        // 1. 顯示註冊畫面 (GET)
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // 2. 處理註冊表單送出 (POST)
        [HttpPost]
        public IActionResult Register(string username, string password, string confirmPassword)
        {
            // 基礎防呆：確認兩次密碼輸入一致
            if (password != confirmPassword)
            {
                ViewBag.Error = "兩次輸入的密碼不一致，請重新確認！";
                return View();
            }

            string connString = _config.GetConnectionString("DefaultConnection"); // 請確認你的連線字串名稱
            bool isUserExist = false;

            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();

                // 步驟 A：先檢查資料庫裡有沒有重複的帳號
                string checkSql = "SELECT COUNT(1) FROM Users WHERE Username = @User";
                using (SqlCommand checkCmd = new SqlCommand(checkSql, cn))
                {
                    checkCmd.Parameters.AddWithValue("@User", username);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        isUserExist = true;
                    }
                }

                // 如果帳號已存在，退回註冊畫面並報錯
                if (isUserExist)
                {
                    ViewBag.Error = "這個帳號已經被註冊過了，請換一個名稱！";
                    return View();
                }

                // 步驟 B：如果帳號沒人用過，就正式寫入資料庫
                string insertSql = "INSERT INTO Users (Username, Password) VALUES (@User, @Pass)";
                using (SqlCommand insertCmd = new SqlCommand(insertSql, cn))
                {
                    insertCmd.Parameters.AddWithValue("@User", username);
                    insertCmd.Parameters.AddWithValue("@Pass", password); // 實務上這裡密碼應該要經過 Hash 加密
                    insertCmd.ExecuteNonQuery();
                }
            }

            // 註冊成功！使用 TempData 傳遞成功訊息給 Login 畫面
            TempData["SuccessMsg"] = "🎉 帳號註冊成功！請使用新帳號登入。";
            return RedirectToAction("Login");
        }

        // 3. 處理登出邏輯
        public async Task<IActionResult> Logout()
        {
            // 銷毀 Cookie 通行證
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}