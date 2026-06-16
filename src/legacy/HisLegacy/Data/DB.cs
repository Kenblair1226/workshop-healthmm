using System;
using System.Collections.Generic;
using HisLegacy.Models;

namespace HisLegacy.Data
{
    // 「資料庫」。其實是放在記憶體的 static List, 重開就回到初始資料。
    // 早期說好要接 SQL Server, 結果 demo 完就變正式版了... (常見的技術債)
    public static class DB
    {
        public static List<Binghuan> binghuans = new List<Binghuan>();
        public static List<Kebie> kebies = new List<Kebie>();
        public static List<Yishi> yishis = new List<Yishi>();
        public static List<Menzhen> menzhens = new List<Menzhen>();
        public static List<Guahao> guahaos = new List<Guahao>();

        // 流水號產生器 (沒有 lock, 多執行緒下會有問題, 但一直沒人發現)
        public static int seqGuahao = 1000;

        static DB()
        {
            Seed();
        }

        public static void Seed()
        {
            kebies.Add(new Kebie() { code = "INT", name = "內科" });
            kebies.Add(new Kebie() { code = "SUR", name = "外科" });
            kebies.Add(new Kebie() { code = "PED", name = "小兒科" });
            kebies.Add(new Kebie() { code = "ENT", name = "耳鼻喉科" });

            yishis.Add(new Yishi() { id = 1, name = "王大明", deptCode = "INT" });
            yishis.Add(new Yishi() { id = 2, name = "李美麗", deptCode = "INT" });
            yishis.Add(new Yishi() { id = 3, name = "陳志強", deptCode = "SUR" });
            yishis.Add(new Yishi() { id = 4, name = "林淑芬", deptCode = "PED" });

            // 門診時段 (寫死今天日期, demo 用)
            string today = DateTime.Now.ToString("yyyy/MM/dd");
            menzhens.Add(new Menzhen() { id = 1, doctorId = 1, deptCode = "INT", date = today, period = 1, room = "201", limit = 30 });
            menzhens.Add(new Menzhen() { id = 2, doctorId = 2, deptCode = "INT", date = today, period = 2, room = "202", limit = 20 });
            menzhens.Add(new Menzhen() { id = 3, doctorId = 3, deptCode = "SUR", date = today, period = 1, room = "301", limit = 15 });
            menzhens.Add(new Menzhen() { id = 4, doctorId = 4, deptCode = "PED", date = today, period = 3, room = "401", limit = 25 });

            binghuans.Add(new Binghuan() { id = 1, name = "張三", sex = "M", birth = "1985/03/12", idno = "A123456789", phone = "0912345678" });
            binghuans.Add(new Binghuan() { id = 2, name = "李四", sex = "女", birth = "1990-07-08", idno = "B223456788", phone = "0922333444" });
            binghuans.Add(new Binghuan() { id = 3, name = "小明", sex = "M", birth = "2020/01/20", idno = "C123456780", phone = "0933000111" });
        }
    }
}
