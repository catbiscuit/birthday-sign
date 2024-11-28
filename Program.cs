using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace BirthDaySign
{
    internal class Program
    {
        private static ChineseLunisolarCalendar chineseDate = new();

        static void Main(string[] args)
        {
            Console.WriteLine("Start running...");

            Run();

            Console.WriteLine("End running...");
        }

        static void Run()
        {
            Conf conf = Deserialize<Conf>(GetEnvValue("CONF"));
            if (conf == null)
            {
                Console.WriteLine("Configuration initialization failed");
                return;
            }

            if (conf.Peoples == null)
            {
                Console.WriteLine("The list is empty");
                return;
            }

            DateTime dtNow = DateTime.Now;
            DateTime dtDate = dtNow.Date;
            int chineseYear = chineseDate.GetYear(dtDate);
            int chineseMonth = chineseDate.GetMonth(dtDate);
            int chineseDay = chineseDate.GetDayOfMonth(dtDate);

            //提前多少天，推送提醒
            int max_distance = 2;

            List<string> diffError = [];
            Dictionary<int, List<string>> diffDay = [];
            List<CalendarInfo> calendarList = [];
            for (int i = 0; i <= max_distance; i++)
            {
                diffDay.Add(i, []);

                DateTime currentDate = dtDate.AddDays(i);

                calendarList.Add(new CalendarInfo
                {
                    Distance = i,
                    LunarDate = new DateInfo
                    {
                        Year = chineseDate.GetYear(currentDate),
                        Month = chineseDate.GetMonth(currentDate),
                        Day = chineseDate.GetDayOfMonth(currentDate),
                    },
                    SolarDate = new DateInfo
                    {
                        Year = currentDate.Year,
                        Month = currentDate.Month,
                        Day = currentDate.Day,
                    },
                });
            }

            int idx = 1;
            foreach (var p in conf.Peoples)
            {
                Console.WriteLine($"total:{conf.Peoples.Count},current:{idx++}");

                //农历-有值
                if (string.IsNullOrWhiteSpace(p.LunarMonthDay) == false)
                {
                    var valid = ValidMonthDay(p.LunarMonthDay, true);
                    if (string.IsNullOrWhiteSpace(valid.error_msg) == false)
                    {
                        diffError.Add($"({diffError.Count + 1})、{p.Name} =>{valid.error_msg}");
                        continue;
                    }

                    var calendar = calendarList.FirstOrDefault(x => (x?.LunarDate?.Month ?? 0) + "." + (x?.LunarDate?.Day ?? 0) == valid.birth_month_dot_day);
                    if (calendar == null)
                    {
                        Console.WriteLine("=>Not yet due date");
                        continue;
                    }

                    string birth_md = ",农历" + valid.birth_month_dot_day;
                    string age_info = ",年龄未知";
                    if (p.BirthYear > 0)
                    {
                        int age = calendar.LunarDate.Year - p.BirthYear;
                        if (calendar.Distance > 0)
                            age_info = $",现在{age - 1}周岁,生日过后{age}周岁";
                        else
                            age_info = $",现在{age}周岁";
                    }

                    diffDay[calendar.Distance].Add($"({diffDay[calendar.Distance].Count + 1}){p.Name}{birth_md}{age_info}");
                }

                //阳历-有值
                if (string.IsNullOrWhiteSpace(p.SolarMonthDay) == false)
                {
                    var valid = ValidMonthDay(p.SolarMonthDay, false);
                    if (string.IsNullOrWhiteSpace(valid.error_msg) == false)
                    {
                        diffError.Add($"({diffError.Count + 1})、{p.Name} =>{valid.error_msg}");
                        continue;
                    }

                    var calendar = calendarList.FirstOrDefault(x => (x?.SolarDate?.Month ?? 0) + "." + (x?.SolarDate?.Day ?? 0) == valid.birth_month_dot_day);
                    if (calendar == null)
                    {
                        Console.WriteLine("=>Not yet due date");
                        continue;
                    }

                    string birth_md = ",阳历" + valid.birth_month_dot_day;
                    string age_info = ",年龄未知";
                    if (p.BirthYear > 0)
                    {
                        int age = calendar.SolarDate.Year - p.BirthYear;
                        if (calendar.Distance > 0)
                            age_info = $",现在{age - 1}周岁,生日过后{age}周岁";
                        else
                            age_info = $",现在{age}周岁";
                    }

                    diffDay[calendar.Distance].Add($"({diffDay[calendar.Distance].Count + 1}){p.Name}{birth_md}{age_info}");
                }

                Console.WriteLine("=>No need to execute");
            }

            if (diffDay.Any(x => x.Value.Count > 0) || diffError.Count > 0)
            {
                string title = "生日提醒";
                string content = "";
                string topicName = "Birthday Remind Services";
                
                List<string> message_all = [$"今天:{dtDate.ToString("yyyy年M月d日")},农历:{chineseMonth}.{chineseDay}"];
                foreach (var item in diffDay)
                {
                    if (item.Value.Count > 0)
                    {
                        string t = item.Key switch
                        {
                            0 => "今 天 生 日:",
                            1 => "明 天 生 日:",
                            2 => "后 天 生 日:",
                            _ => item.Key + " 天 后 生 日:",
                        };
                        message_all.Add("");
                        message_all.Add(t);
                        message_all.AddRange(item.Value);
                    }
                }
                if (diffError.Count > 0)
                {
                    message_all.Add("");
                    message_all.Add("执 行 错 误:");
                    message_all.AddRange(diffError);
                }
                message_all.Add("");
                message_all.Add("");
                message_all.Add("");
                message_all.Add($"当前时间:{dtNow.ToString("yyyy-MM-dd HH:mm:ss")}");

                content = string.Join("\n", message_all);

                SendEMail(title, content, topicName, conf.Smtp_Server, conf.Smtp_Port, conf.Smtp_Email, conf.Smtp_Password, conf.Receive_Email_List);
                SendBark(title, content, conf.Bark_Devicekey, conf.Bark_Icon).GetAwaiter().GetResult();
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("【summary】No need to send.");
                foreach (var item in diffDay)
                {
                    sb.AppendLine($"distance{item.Key},count:{item.Value.Count}.");
                }
                sb.AppendLine($"error,count:{diffError.Count}.");
                Console.WriteLine(sb.ToString());
            }
        }

        static (int birth_month, int birth_day, string error_msg, string birth_month_dot_day) ValidMonthDay(string monthDay, bool isLunar)
        {
            string propName = isLunar ? "LunarMonthDay" : "SolarMonthDay";
            int birth_month = 0;
            int birth_day = 0;

            string[] md = monthDay.Split(".");
            if (md.Length != 2)
            {
                return (birth_month, birth_day, $"{propName} {monthDay} FormatError", birth_month + "." + birth_day);
            }

            if (int.TryParse(md[0], out birth_month) == false || int.TryParse(md[1], out birth_day) == false)
            {
                return (birth_month, birth_day, $"{propName} {monthDay} ValueError", birth_month + "." + birth_day);
            }

            if (birth_month < 1 || birth_month > 12 || birth_day < 1 || birth_day > 31)
            {
                return (birth_month, birth_day, $"{propName} {monthDay} ValueOutOfRange", birth_month + "." + birth_day);
            }

            return (birth_month, birth_day, "", birth_month + "." + birth_day);
        }

        static int SendEMail(string title, string content, string topicName, string smtp_Server, int smtp_Port, string smtp_Email, string smtp_Password, List<string> receive_Email_List)
        {
            if (string.IsNullOrWhiteSpace(smtp_Email) || string.IsNullOrWhiteSpace(smtp_Password) || receive_Email_List == null)
            {
                Console.WriteLine("RECEIVE_EMAIL_LIST is null");
                return 0;
            }

            MailAddress fromMail = new(smtp_Email, topicName);
            foreach (var item in receive_Email_List)
            {
                if (string.IsNullOrWhiteSpace(item))
                    continue;

                MailAddress toMail = new(item);

                MailMessage mail = new(fromMail, toMail)
                {
                    IsBodyHtml = false,
                    Subject = title,
                    Body = content
                };

                SmtpClient client = new()
                {
                    EnableSsl = true,
                    Host = smtp_Server,
                    Port = smtp_Port,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(smtp_Email, smtp_Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                client.Send(mail);
            }

            Console.WriteLine("【email】 Success");
            return 1;
        }

        static async Task<int> SendBark(string title, string content, string bark_Devicekey, string bark_Icon)
        {
            if (string.IsNullOrWhiteSpace(bark_Devicekey))
            {
                Console.WriteLine("BARK_DEVICEKEY is empty");
                return 0;
            }

            string url = "https://api.day.app/push";
            if (string.IsNullOrWhiteSpace(bark_Icon) == false)
                url = url + "?icon=" + bark_Icon;

            Dictionary<string, string> headers = new()
            {
                { "charset", "utf-8" }
            };

            Dictionary<string, object> dic = new()
            {
                { "title", title },
                { "body", content },
                { "device_key", bark_Devicekey }
            };

            var result = await HttpPostBody(url, headers, dic);
            var barkResponse = Deserialize<BarkResponse>(result);
            if (barkResponse == null)
            {
                Console.WriteLine("【Bark】Send message to Bark Error");
                return -1;
            }
            else
            {
                if (barkResponse.code == 200)
                {
                    Console.WriteLine("【Bark】Send message to Bark successfully.");
                    return 1;
                }
                else
                {
                    Console.WriteLine($"【Bark】【Send Message Response】{barkResponse.text}");
                    return 0;
                }
            }
        }

        static async Task<string> HttpPostBody(string url, Dictionary<string, string> headers, Dictionary<string, object> dic)
        {
            try
            {
                HttpClient _client = new HttpClient();

                var p = JsonSerializer.Serialize(dic);

                HttpContent httpContent = new StringContent(p);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                foreach (var item in headers)
                    if (httpContent.Headers.Contains(item.Key) == false)
                        httpContent.Headers.Add(item.Key, item.Value);

                HttpResponseMessage response = await _client.PostAsync(url, httpContent);

                string result = string.Empty;
                if (response.IsSuccessStatusCode)
                    result = await response.Content.ReadAsStringAsync();

                return result;
            }
            catch (Exception ex)
            {
                return ex?.Message ?? "error";
            }
        }

        static T Deserialize<T>(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });
        }

        static string GetEnvValue(string key) => Environment.GetEnvironmentVariable(key);
    }

    public class Conf
    {
        public string Bark_Devicekey { get; set; }
        public string Bark_Icon { get; set; }
        public string Smtp_Server { get; set; }
        public int Smtp_Port { get; set; }
        public string Smtp_Email { get; set; }
        public string Smtp_Password { get; set; }
        public List<string> Receive_Email_List { get; set; }
        public List<People> Peoples { get; set; }
    }

    public class People
    {
        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 出生年份，未知的话填=0
        /// </summary>
        public int BirthYear { get; set; }
        /// <summary>
        /// 农历，2.16，过农历生日就填，过阳历生日这个就为空
        /// </summary>
        public string LunarMonthDay { get; set; }
        /// <summary>
        /// 阳历，3.18，过阳历生日就填，过农历生日这个就为空
        /// </summary>
        public string SolarMonthDay { get; set; }
    }

    public class BarkResponse
    {
        public int code { get; set; }
        public string text { get; set; }
    }

    public class CalendarInfo
    {
        /// <summary>
        /// 距离今天多少天，今天=0，明天=1
        /// </summary>
        public int Distance { get; set; }
        /// <summary>
        /// 农历
        /// </summary>
        public DateInfo LunarDate { get; set; }
        /// <summary>
        /// 阳历
        /// </summary>
        public DateInfo SolarDate { get; set; }
    }
    public class DateInfo
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
    }
}
