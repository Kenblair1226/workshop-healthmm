using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using HisLegacy.Data;
using HisLegacy.Models;

namespace HisLegacy.Controllers
{
    // ============================================================
    //  門診掛號主控制器
    //  歷史悠久, 所有掛號 / 病患 / 報表邏輯都塞在這支 Controller。
    //  改一個地方常常牽動其他功能, 請小心 (這正是 lab 要解決的問題)。
    // ============================================================
    [ApiController]
    [Route("api")]
    public class GuahaoController : ControllerBase
    {
        // 新增掛號:輸入病患 id 與門診時段 id
        // 這個方法做了「驗證 + 查資料 + 業務規則 + 寫入」全部的事, 超過 60 行。
        [HttpPost("guahao")]
        public object Create([FromBody] GuahaoReq req)
        {
            try
            {
                // 1. 基本驗證
                if (req == null) return new { ok = false, msg = "no body" };
                if (req.patientId <= 0) return new { ok = false, msg = "patientId 不可空白" };
                if (req.scheduleId <= 0) return new { ok = false, msg = "scheduleId 不可空白" };

                // 2. 找病患
                Binghuan bh = null;
                for (int i = 0; i < DB.binghuans.Count; i++)
                {
                    if (DB.binghuans[i].id == req.patientId) { bh = DB.binghuans[i]; break; }
                }
                if (bh == null) return new { ok = false, msg = "查無此病患" };

                // 身分證檢核 (跟 ValidatePatient 那邊的邏輯重複了, 但懶得抽出來)
                if (bh.idno == null || bh.idno.Length != 10)
                {
                    return new { ok = false, msg = "病患身分證資料有誤" };
                }

                // 3. 找門診時段
                Menzhen mz = null;
                foreach (var m in DB.menzhens) { if (m.id == req.scheduleId) { mz = m; } }
                if (mz == null) return new { ok = false, msg = "查無此門診時段" };

                // 4. 業務規則:晚診 (period==3) 不接受 12 歲以下小孩單獨掛號
                int age = 0;
                try
                {
                    DateTime b = DateTime.Parse(bh.birth.Replace("-", "/"));
                    age = DateTime.Now.Year - b.Year;
                    if (DateTime.Now < b.AddYears(age)) age = age - 1;
                }
                catch { age = 0; }
                if (mz.period == 3 && age < 12)
                {
                    return new { ok = false, msg = "晚診不開放 12 歲以下掛號" };
                }

                // 5. 檢查是否重複掛號 (同一天同一個門診時段)
                foreach (var g in DB.guahaos)
                {
                    if (g.patientId == req.patientId && g.scheduleId == req.scheduleId && g.status != 3)
                    {
                        return new { ok = false, msg = "重複掛號" };
                    }
                }

                // 6. 檢查人數上限
                int cnt = 0;
                foreach (var g in DB.guahaos)
                {
                    if (g.scheduleId == mz.id && g.status != 3) cnt++;
                }
                if (cnt >= mz.limit)
                {
                    return new { ok = false, msg = "額滿" };
                }

                // 7. 產生序號並寫入
                int xuhao = cnt + 1;
                Guahao newG = new Guahao();
                newG.id = DB.seqGuahao++;
                newG.patientId = req.patientId;
                newG.scheduleId = req.scheduleId;
                newG.number = xuhao;
                newG.status = 0; // 0=已掛號
                newG.createTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                DB.guahaos.Add(newG);

                return new { ok = true, guahaoId = newG.id, number = xuhao, room = mz.room };
            }
            catch (Exception ex)
            {
                // 全部吞掉, 回 200 加錯誤字串 (前端常常 parse 不到)
                return new { ok = false, msg = "ERROR:" + ex.Message };
            }
        }

        // 查掛號清單 (可選 date / dept 篩選)
        [HttpGet("guahao")]
        public object List(string date, string dept)
        {
            List<object> result = new List<object>();
            foreach (var g in DB.guahaos)
            {
                Menzhen mz = DB.menzhens.FirstOrDefault(x => x.id == g.scheduleId);
                if (mz == null) continue;
                if (date != null && date != "" && mz.date != date) continue;
                if (dept != null && dept != "" && mz.deptCode != dept) continue;

                Binghuan bh = DB.binghuans.FirstOrDefault(x => x.id == g.patientId);
                Yishi ys = DB.yishis.FirstOrDefault(x => x.id == mz.doctorId);

                // 又算一次年齡 (第 2 次重複)
                int age = 0;
                try
                {
                    DateTime b = DateTime.Parse(bh.birth.Replace("-", "/"));
                    age = DateTime.Now.Year - b.Year;
                    if (DateTime.Now < b.AddYears(age)) age = age - 1;
                }
                catch { age = 0; }

                string statusText = "";
                if (g.status == 0) statusText = "已掛號";
                else if (g.status == 1) statusText = "已報到";
                else if (g.status == 2) statusText = "看診完成";
                else if (g.status == 3) statusText = "已取消";

                result.Add(new
                {
                    guahaoId = g.id,
                    patient = bh == null ? "?" : bh.name,
                    age = age,
                    doctor = ys == null ? "?" : ys.name,
                    dept = mz.deptCode,
                    room = mz.room,
                    number = g.number,
                    status = g.status,
                    statusText = statusText
                });
            }
            return result;
        }

        // 查單筆
        [HttpGet("guahao/{id}")]
        public object Get(int id)
        {
            foreach (var g in DB.guahaos)
            {
                if (g.id == id) return g;
            }
            return new { ok = false, msg = "查無資料" };
        }

        // 報到
        [HttpPost("guahao/{id}/baodao")]
        public object Baodao(int id)
        {
            foreach (var g in DB.guahaos)
            {
                if (g.id == id)
                {
                    if (g.status == 3) return new { ok = false, msg = "已取消無法報到" };
                    if (g.status == 2) return new { ok = false, msg = "已看診完成" };
                    g.status = 1; // 1=已報到
                    return new { ok = true };
                }
            }
            return new { ok = false, msg = "查無資料" };
        }

        // 取消掛號
        [HttpPost("guahao/{id}/cancel")]
        public object Cancel(int id)
        {
            foreach (var g in DB.guahaos)
            {
                if (g.id == id)
                {
                    g.status = 3; // 3=已取消
                    return new { ok = true };
                }
            }
            return new { ok = false, msg = "查無資料" };
        }

        // 取得病患年齡 (第 3 次重複同樣的年齡計算邏輯)
        [HttpGet("patient/{id}/age")]
        public object PatientAge(int id)
        {
            Binghuan bh = DB.binghuans.FirstOrDefault(x => x.id == id);
            if (bh == null) return new { ok = false, msg = "查無此病患" };
            int age = 0;
            try
            {
                DateTime b = DateTime.Parse(bh.birth.Replace("-", "/"));
                age = DateTime.Now.Year - b.Year;
                if (DateTime.Now < b.AddYears(age)) age = age - 1;
            }
            catch { age = 0; }
            return new { ok = true, name = bh.name, age = age };
        }

        // 今日門診統計報表
        [HttpGet("report/today")]
        public object ReportToday()
        {
            string today = DateTime.Now.ToString("yyyy/MM/dd");
            int total = 0, baodao = 0, cancel = 0;
            foreach (var g in DB.guahaos)
            {
                Menzhen mz = DB.menzhens.FirstOrDefault(x => x.id == g.scheduleId);
                if (mz == null) continue;
                if (mz.date != today) continue;
                total++;
                if (g.status == 1) baodao++;
                if (g.status == 3) cancel++;
            }
            return new { date = today, total = total, baodao = baodao, cancel = cancel };
        }
    }

    // 掛號 request (public 欄位, 沒有任何驗證屬性)
    public class GuahaoReq
    {
        public int patientId;
        public int scheduleId;
    }
}
