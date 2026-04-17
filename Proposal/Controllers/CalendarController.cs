using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authorization;

namespace Proposal.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly IConfiguration _config;
        public CalendarController(IConfiguration config) { _config = config; }

        public IActionResult Index() => View();

        // 這是給 FullCalendar 呼叫的 API，回傳 JSON
        [HttpGet]
        public JsonResult GetEvents()
        {
            var events = new List<object>();
            string connString = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();
                string sql = "SELECT * FROM Events WHERE Username = @User";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {

                    cmd.Parameters.AddWithValue("@User", User.Identity.Name);

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            events.Add(new
                            {
                                id = r["Id"],
                                title = r["Title"].ToString(),
                                start = Convert.ToDateTime(r["StartDateTime"]).ToString("yyyy-MM-ddTHH:mm:ss"),
                                end = r["EndDateTime"] == DBNull.Value ? null : Convert.ToDateTime(r["EndDateTime"]).ToString("yyyy-MM-ddTHH:mm:ss"),
                                allDay = Convert.ToBoolean(r["IsAllDay"])
                            });
                        }
                    }
                }
            }
            return Json(events);
        }

        [HttpPost]
        public IActionResult AddEvent(string title, string start, string end)
        {
            string connString = _config.GetConnectionString("DefaultConnection"); // 請確認你的連線字串名稱

            using (SqlConnection cn = new SqlConnection(connString))
            {
                cn.Open();
                string sql = "INSERT INTO Events (Username, Title, StartDateTime, EndDateTime) VALUES (@User, @Title, @Start, @End)";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@User", User.Identity.Name);
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@Start", DateTime.Parse(start));
                    cmd.Parameters.AddWithValue("@End", string.IsNullOrEmpty(end) ? (object)DBNull.Value : DateTime.Parse(end));
                    cmd.ExecuteNonQuery();
                }
            }
            return Json(new { success = true });
        }
    }
}