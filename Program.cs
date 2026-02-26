using System.Text;

namespace BirthDaySign
{
    internal class Program
    {
        private static int distance = 2;

        static async Task Main(string[] args)
        {
            Console.WriteLine(Util.GetBeiJingTimeStr() + " - Start running...");

            await Run();

            Console.WriteLine(Util.GetBeiJingTimeStr() + " - End running...");

#if DEBUG
            Console.WriteLine("Program Run On Debug");
            Console.ReadLine();
#else
            Console.WriteLine("Program Run On Release");
#endif
        }

        static async Task Run()
        {
            Conf conf = Util.GetEnvValue("CONF")?.TryToObject<Conf>();
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
            Lunar.Lunar nowChineseDate = Lunar.Lunar.FromDate(dtDate);
            int chineseYear = nowChineseDate.Year;
            int chineseMonth = nowChineseDate.Month;
            int chineseDay = nowChineseDate.Day;

            //距离天数
            if (conf.Distance.HasValue && conf.Distance.Value >= 0)
                distance = conf.Distance.Value;

            Dictionary<int, List<string>> diffDay = [];
            List<CalendarInfo> calendarList = [];
            for (int i = 0; i <= distance; i++)
            {
                diffDay.Add(i, []);

                DateTime currentDate = dtDate.AddDays(i);
                Lunar.Lunar currentChineseDate = Lunar.Lunar.FromDate(currentDate);

                calendarList.Add(new CalendarInfo
                {
                    Distance = i,
                    LunarDate = new DateInfo
                    {
                        Year = currentChineseDate.Year,
                        Month = currentChineseDate.Month,
                        Day = currentChineseDate.Day,
                    },
                    SolarDate = new DateInfo
                    {
                        Year = currentDate.Year,
                        Month = currentDate.Month,
                        Day = currentDate.Day,
                    },
                });
            }

            List<string> diffError = [];
            for (int i = 0; i < conf.Peoples.Count; i++)
            {
                Console.WriteLine("");
                Console.WriteLine($"total:{conf.Peoples.Count},current:{i + 1}");

                var p = conf.Peoples[i];

                if (p == null)
                {
                    diffError.Add($"({diffError.Count + 1})、对象为空");
                    Console.WriteLine("    object is null");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(p.LunarDate) && string.IsNullOrWhiteSpace(p.SolarDate))
                {
                    Console.WriteLine("    =>No need to execute");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(p.Name))
                    p.Name = $"索引-{i}";

                //农历-有值
                if (string.IsNullOrWhiteSpace(p.LunarDate) == false)
                {
                    bool isLunar = true;
                    string dateName = isLunar ? "农历" : "阳历";
                    string propName = isLunar ? "LunarDate" : "SolarDate";
                    Console.WriteLine("  " + propName);

                    (bool success, string msg, DateInfo dateInfo) = ValidDate(p.LunarDate, isLunar);
                    if (success)
                    {
                        var calendar = calendarList.FirstOrDefault(x => x.LunarDate.MonthDay == dateInfo.MonthDay);
                        if (calendar != null)
                        {
                            string birth_info = $",{dateName}{dateInfo.MonthDay}";
                            string age_info = ",年龄未知";
                            if (dateInfo.Year > 0)
                            {
                                int age = calendar.LunarDate.Year - dateInfo.Year;
                                if (calendar.Distance > 0)
                                    age_info = $",现在{age - 1}周岁,生日过后{age}周岁";
                                else
                                    age_info = $",现在{age}周岁";
                            }

                            diffDay[calendar.Distance].Add($"({diffDay[calendar.Distance].Count + 1}){p.Name}{birth_info}{age_info}");
                            Console.WriteLine("    =>success");
                        }
                        else
                        {
                            Console.WriteLine("    =>Not yet due date");
                        }
                    }
                    else
                    {
                        diffError.Add($"({diffError.Count + 1})、{p.Name} =>{msg}");
                        Console.WriteLine("    =>error");
                    }
                }

                //阳历-有值
                if (string.IsNullOrWhiteSpace(p.SolarDate) == false)
                {
                    bool isLunar = false;
                    string dateName = isLunar ? "农历" : "阳历";
                    string propName = isLunar ? "LunarDate" : "SolarDate";
                    Console.WriteLine("  " + propName);

                    (bool success, string msg, DateInfo dateInfo) = ValidDate(p.SolarDate, isLunar);
                    if (success)
                    {
                        var calendar = calendarList.FirstOrDefault(x => x.SolarDate.MonthDay == dateInfo.MonthDay);
                        if (calendar != null)
                        {
                            string birth_info = $",{dateName}{dateInfo.MonthDay}";
                            string age_info = ",年龄未知";
                            if (dateInfo.Year > 0)
                            {
                                int age = calendar.SolarDate.Year - dateInfo.Year;
                                if (calendar.Distance > 0)
                                    age_info = $",现在{age - 1}周岁,生日过后{age}周岁";
                                else
                                    age_info = $",现在{age}周岁";
                            }

                            diffDay[calendar.Distance].Add($"({diffDay[calendar.Distance].Count + 1}){p.Name}{birth_info}{age_info}");
                            Console.WriteLine("    =>success");
                        }
                        else
                        {
                            Console.WriteLine("    =>Not yet due date");
                        }
                    }
                    else
                    {
                        diffError.Add($"({diffError.Count + 1})、{p.Name} =>{msg}");
                        Console.WriteLine("    =>error");
                    }
                }
            }

            if (diffDay.Any(x => x.Value.Count > 0) || diffError.Count > 0)
            {
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

                string title = "生日提醒";
                string content = string.Join("\n", message_all);
                string topicName = "Birthday Remind Services";

#if DEBUG
                Console.WriteLine(content);
#endif

                Console.WriteLine("Send");
                SendUtil.SendEMail(conf.Smtp_Server, conf.Smtp_Port, conf.Smtp_Email, conf.Smtp_Password, conf.Receive_Email_List, title, content, topicName);
                await SendUtil.SendBark(conf.Bark_Devicekey, conf.Bark_Icon, title, content);
            }
            else
            {
                StringBuilder sb = new();
                sb.AppendLine("");
                sb.AppendLine("【summary】No need to send.");
                foreach (var item in diffDay)
                {
                    sb.AppendLine($"distance{item.Key},count:{item.Value.Count}.");
                }
                sb.AppendLine($"error,count:{diffError.Count}.");
                Console.WriteLine(sb.ToString());
            }
        }

        static (bool success, string msg, DateInfo dateInfo) ValidDate(string date, bool isLunar)
        {
            string propName = isLunar ? "LunarDate" : "SolarDate";
            DateInfo dateInfo = new();
            int year = 0;
            int month = 0;
            int day = 0;

            if (string.IsNullOrWhiteSpace(date))
                return (false, $"{propName} IsNullOrWhiteSpace", dateInfo);

            string dot = ".";
            date = date.Replace("-", dot).Replace("/", dot).Trim();
            string[] str = date.Split(".");
            if (str.Length == 2)
            {
                if (int.TryParse(str[0].Trim(), out month) == false || int.TryParse(str[1].Trim(), out day) == false)
                    return (false, $"{propName} {date} ValueError", dateInfo);

                if (month < 1 || month > 12 || day < 1 || day > 31)
                    return (false, $"{propName} {date} ValueOutOfRange", dateInfo);

                dateInfo.Year = year;
                dateInfo.Month = month;
                dateInfo.Day = day;
                return (true, "", dateInfo);
            }
            else if (str.Length == 3)
            {
                bool v = int.TryParse(str[0].Trim(), out year);
                if (v == false || year <= 0)
                    year = 0;

                if (int.TryParse(str[1].Trim(), out month) == false || int.TryParse(str[2].Trim(), out day) == false)
                    return (false, $"{propName} {date} ValueError", dateInfo);

                if (month < 1 || month > 12 || day < 1 || day > 31)
                    return (false, $"{propName} {date} ValueOutOfRange", dateInfo);

                dateInfo.Year = year;
                dateInfo.Month = month;
                dateInfo.Day = day;
                return (true, "", dateInfo);
            }
            else
            {
                return (false, $"{propName} {date} FormatError", dateInfo);
            }
        }
    }

    public class Conf : SendConf
    {
        public int? Distance { get; set; }
        public List<People> Peoples { get; set; }
    }

    public class People
    {
        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 农历生日，过农历生日才填。示例：1990.2.23，分隔符支持["/","-","."]。0.2.23或2.23，这种年份填0或不填也支持，只是年龄会为未知
        /// </summary>
        public string LunarDate { get; set; }
        /// <summary>
        /// 阳历生日，过阳历生日才填。示例：1990.3.19，分隔符支持["/","-","."]。0.3.19或3.19，这种年份填0或不填也支持，只是年龄会为未知
        /// </summary>
        public string SolarDate { get; set; }
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
        public string MonthDay
        {
            get
            {
                return Month + "." + Day;
            }
            set
            {

            }
        }
    }
}
