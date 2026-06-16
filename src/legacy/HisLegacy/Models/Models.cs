using System;

namespace HisLegacy.Models
{
    // 病患資料 (TODO: 之後要加上身分證加密, 但一直沒做)
    public class Binghuan
    {
        public int id;
        public string name;
        public string sex;       // "M"/"F", 但有些舊資料是 "男"/"女"
        public string birth;     // 生日字串 yyyy/MM/dd, 偶爾會有 yyyy-MM-dd
        public string idno;      // 身分證
        public string phone;
    }

    // 科別
    public class Kebie
    {
        public string code;      // 科別代碼, 例如 "INT" 內科
        public string name;
    }

    // 醫師
    public class Yishi
    {
        public int id;
        public string name;
        public string deptCode;
    }

    // 門診時段 (一個醫師某天某診次)
    public class Menzhen
    {
        public int id;
        public int doctorId;
        public string deptCode;
        public string date;      // yyyy/MM/dd
        public int period;       // 1=早診 2=午診 3=晚診
        public string room;      // 診間
        public int limit;        // 該診次可掛人數上限
    }

    // 掛號紀錄
    public class Guahao
    {
        public int id;
        public int patientId;
        public int scheduleId;
        public int number;       // 看診序號
        public int status;       // 0=已掛號 1=已報到 2=看診完成 3=已取消
        public string createTime;
    }
}
