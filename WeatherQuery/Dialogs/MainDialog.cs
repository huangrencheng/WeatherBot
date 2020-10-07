using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeatherQuery.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        protected readonly IConfiguration Configuration;
        protected readonly ILogger Logger;

        public MainDialog(IConfiguration configuration, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            Configuration = configuration;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                //IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));
            AddDialog(new DetailQueryDialog());

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(Configuration["LuisAppId"]) || string.IsNullOrEmpty(Configuration["LuisAPIKey"]) || string.IsNullOrEmpty(Configuration["LuisAPIHostName"]))
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file."), cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("����һ��������ѯ������\n ʾ����\"���������������\"") }, cancellationToken);
            }
        }

        //��ѯLuis�����ؽ��
        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("ʾ����\"���������������\"") }, cancellationToken);
            Console.WriteLine("******************************");
            Console.WriteLine(stepContext.Context);
            var QueryDetails = await LuisHelper.ExecuteLuisQuery(Configuration, Logger, stepContext.Context, cancellationToken);
            /*
            var QueryDetails = stepContext.Result != null
                    ?
                await LuisHelper.ExecuteLuisQuery(Configuration, Logger, stepContext.Context, cancellationToken)
                    :
                new QueryDetails();
            */
            return await stepContext.BeginDialogAsync(nameof(DetailQueryDialog), QueryDetails, cancellationToken);
        }

        //weather query request
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("") was cancelled or the user failed to confirm, the Result here will be null.
            if (stepContext.Result != null)
            {
                var result = (QueryDetails)stepContext.Result;
                if (result.DateType == null)
                {
                    Console.WriteLine("no datetype, date: " + result.Date);
                    if (result.Date.Contains(","))
                    {
                        result.DateType = "daterange";
                        var results = DateTimeRecognizer.RecognizeDateTime(result.Date, Culture.English);
                        int i = 0;
                        foreach (var r in results)
                        {
                            var values = (List<Dictionary<string, string>>)r.Resolution["values"];
                            foreach (var value in values)
                            {
                                if (value.TryGetValue("timex", out var timex))
                                    if (i++ == 0)
                                        result.StartDate = timex.ToString();
                                    else
                                        result.EndDate = timex.ToString();
                            }
                        }
                    }
                    else
                    {
                        result.DateType = "date";
                    }
                }
                Console.WriteLine(result.DateType + " " + result.Date + " " + result.Location);
                String loc = HttpUtility.UrlEncode(result.Location);
                String date_type = result.DateType;
                DateTime now = Convert.ToDateTime(DateTime.Now.ToShortDateString());
                DateTime date = DateTime.Now;
                DateTime start_date = DateTime.Now;
                DateTime end_date = DateTime.Now;
                int days_range = 1;
                
                if (date_type == "daterange")
                {
                    DateTime.TryParse(result.StartDate, out start_date);
                    start_date = Convert.ToDateTime(start_date.ToShortDateString());
                    DateTime.TryParse(result.EndDate, out end_date);
                    end_date = Convert.ToDateTime(end_date.ToShortDateString());
                    days_range = end_date.Subtract(start_date).Days;
                    //Console.WriteLine("start date:" + start_date + "; end date:" + end_date + ";days range:" + days_range);
                }
                else
                {
                    DateTime.TryParse(result.Date, out date);
                    date = Convert.ToDateTime(date.ToShortDateString());
                    //Console.WriteLine("date:" + date);
                }

                Console.WriteLine("LUIS result: location " + loc + "; type " + date_type);
                String url_root = "https://api.seniverse.com/v3/";
                String pri_key = "SpNlNXG30FKAwvTPy";
                //����ʵ��
                String url_now = "weather/now.json?key=" + pri_key + "&location=";
                String url_now2 = "&language=zh-Hans&unit=c";
                //ÿ������
                String url_daily = "weather/daily.json?key=" + pri_key + "&location=";
                String url_daily2 = "&language=zh-Hans&unit=c&start=";
                String url_daily3 = "&days=";
                //����ʵ��
                String url_air_now = "air/now.json?key=" + pri_key + "&location=";
                String url_air_now2 = "&language=zh-Hans&scope=city";
                //ÿ�տ���
                String url_air_daily = "air/daily.json?key=" + pri_key + "&language=zh-Hans&location=";
                String url_air_daily2 = "&days =";
                

                String msg = "��Ǹ����ѯʧ��";
                
                if (result.key == "NowQuery")
                {
                    Console.WriteLine(url_root + url_now + loc + url_now2);
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url_root + url_now + loc + url_now2);
                    request.Method = "GET";
                    request.ContentType = "charset=UTF-8";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    string retString = reader.ReadToEnd();
                    reader.Close();
                    //test
                    Console.WriteLine("[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[");
                    Console.WriteLine(retString);

                    JObject resJson = (JObject)JsonConvert.DeserializeObject(retString);
                    String path = resJson["results"][0]["location"]["path"].ToString();
                    String timezone = resJson["results"][0]["location"]["timezone"].ToString();
                    String text = resJson["results"][0]["now"]["text"].ToString();
                    String temperature = resJson["results"][0]["now"]["temperature"].ToString();

                    msg = "����ѯ�ĵ���Ϊ: \t" + path  + "\n\r��ǰ�������:\t" + text + "���¶�" + temperature;
                }
                else if (result.key == "WeatherQuery")
                {
                    int start;
                    int days;
                    if (date_type == "daterange")
                    {
                        start = start_date.Subtract(now).Days ;
                        if (end_date.Subtract(now).Days > 14)
                            days = 15 - start;
                        else
                            days = end_date.Subtract(start_date).Days;
                    }
                    else
                    {
                        start = date.Subtract(now).Days;
                        days = 1;
                    }
                    Console.WriteLine(url_root + url_daily + loc + url_daily2 + start + url_daily3 + days);
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url_root + url_daily + loc + url_daily2 + start + url_daily3 + days);
                    request.Method = "GET";
                    request.ContentType = "charset=UTF-8";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    string retString = reader.ReadToEnd();
                    reader.Close();
                    //test
                    Console.WriteLine("[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[");
                    Console.WriteLine(retString);

                    JObject resJson = (JObject)JsonConvert.DeserializeObject(retString);
                    String path = resJson["results"][0]["location"]["path"].ToString();
                    String timezone = resJson["results"][0]["location"]["timezone"].ToString();
                    msg = "����ѯ��λ��Ϊ" + path + "\n\r";
                    for (int i = 0; i < days; i++)
                    {
                        String tmp_date = resJson["results"][0]["daily"][i]["date"].ToString();
                        String text_day = resJson["results"][0]["daily"][i]["text_day"].ToString();
                        String text_night = resJson["results"][0]["daily"][i]["text_night"].ToString();
                        String high = resJson["results"][0]["daily"][i]["high"].ToString();
                        String low = resJson["results"][0]["daily"][i]["low"].ToString();
                        String wind_direction = resJson["results"][0]["daily"][i]["wind_direction"].ToString();
                        String wind_direction_degree = resJson["results"][0]["daily"][i]["wind_direction_degree"].ToString(); //����Ƕ�
                        String wind_speed = resJson["results"][0]["daily"][i]["wind_speed"].ToString(); //����
                        String wind_scale = resJson["results"][0]["daily"][i]["wind_scale"].ToString(); //�ȼ�
                        String tmp_msg = tmp_date + "�գ�" + "�����������Ϊ" + text_day + "��ҹ���������Ϊ" + text_night + "������¶�" + high + "������¶�" + low + ",��" + wind_scale + "��" + wind_direction + "��\n\r";
                        msg += tmp_msg;
                    }
                }
                else if (result.key == "AirQuery")
                {
                    int days = 0;
                    int start = 0;
                    if (date_type == "daterange")
                    {
                        days = end_date.Subtract(now).Days;
                        start = start_date.Subtract(now).Days;
                        if (days > 4)
                            days = 5;
                    }
                    else
                    {
                        start = date.Subtract(now).Days;
                        days = start + 1;
                    }
                    
                    
                    Console.WriteLine(url_root + url_air_daily + loc + url_air_daily2 + days);
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url_root + url_air_daily + loc + url_air_daily2 + days);
                    request.Method = "GET";
                    request.ContentType = "charset=UTF-8";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    string retString = reader.ReadToEnd();
                    reader.Close();
                    //test
                    Console.WriteLine("airquery start:" + start + "; days:" + days);
                    Console.WriteLine(retString);

                    JObject resJson = (JObject)JsonConvert.DeserializeObject(retString);
                    String path = resJson["results"][0]["location"]["path"].ToString();
                    String timezone = resJson["results"][0]["location"]["timezone"].ToString();
                    msg = "����ѯ��λ��Ϊ" + path + "\n\r";
                    for(int i = start; i < days; i++)
                    {
                        String tmp_date = resJson["results"][0]["daily"][i]["date"].ToString();
                        String aqi = resJson["results"][0]["daily"][i]["aqi"].ToString();
                        String pm25 = resJson["results"][0]["daily"][i]["pm25"].ToString();
                        String pm10 = resJson["results"][0]["daily"][i]["pm10"].ToString();
                        String so2 = resJson["results"][0]["daily"][i]["so2"].ToString(); //��������
                        String no2 = resJson["results"][0]["daily"][i]["no2"].ToString(); //��������
                        String co = resJson["results"][0]["daily"][i]["co"].ToString(); //һ����̼
                        String o3 = resJson["results"][0]["daily"][i]["o3"].ToString(); //����
                        String quality = resJson["results"][0]["daily"][i]["quality"].ToString();
                        msg += tmp_date + ":��������Ϊ" + quality + "������ָ��Ϊ" + aqi + "��pm2.5ָ��Ϊ" + pm25 + "��pm10ָ��Ϊ" + pm10 + "����������Ũ��Ϊ" + so2 + "\n\r";
                    }
                    
                }
                else if (result.key == "AirNow")
                {
                    Console.WriteLine(url_root + url_air_now + loc + url_air_now2);
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url_root + url_air_now + loc + url_air_now2);
                    request.Method = "GET";
                    request.ContentType = "charset=UTF-8";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    string retString = reader.ReadToEnd();
                    reader.Close();
                    //test
                    Console.WriteLine("[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[");
                    Console.WriteLine(retString);

                    JObject resJson = (JObject)JsonConvert.DeserializeObject(retString);
                    String path = resJson["results"][0]["location"]["path"].ToString();
                    String timezone = resJson["results"][0]["location"]["timezone"].ToString();
                    msg = "����ѯ��λ��Ϊ" + path + "\n\r";
                    
                    String aqi = resJson["results"][0]["air"]["city"]["aqi"].ToString();
                    String pm25 = resJson["results"][0]["air"]["city"]["pm25"].ToString();
                    String pm10 = resJson["results"][0]["air"]["city"]["pm10"].ToString();
                    String so2 = resJson["results"][0]["air"]["city"]["so2"].ToString(); //��������
                    String no2 = resJson["results"][0]["air"]["city"]["no2"].ToString(); //��������
                    String co = resJson["results"][0]["air"]["city"]["co"].ToString(); //һ����̼
                    String o3 = resJson["results"][0]["air"]["city"]["o3"].ToString(); //����
                    String primary_pollutant = resJson["results"][0]["air"]["city"]["primary_pollutant"].ToString();
                    String quality = resJson["results"][0]["air"]["city"]["quality"].ToString();
                    msg = "��ǰ��������Ϊ" + quality + "����ָ��Ϊ" + aqi + "��pm2.5ָ��Ϊ" + pm25 + "��pm10ָ��Ϊ" + pm10 + "��������Ũ��Ϊ" + so2 ;
                }
                else if (result.key == "RainQuery")
                {
                    int start;
                    int days;
                    if (date_type == "daterange")
                    {
                        start = start_date.Subtract(now).Days;
                        days = end_date.Subtract(start_date).Days;
                    }
                    else
                    {
                        start = date.Subtract(now).Days;
                        days = 1;
                    }
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url_root + url_daily + loc + url_daily2 + start + url_daily3 + days_range);
                    request.Method = "GET";
                    request.ContentType = "charset=UTF-8";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    string retString = reader.ReadToEnd();
                    reader.Close();
                    //test
                    Console.WriteLine("[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[");
                    Console.WriteLine(retString);

                    JObject resJson = (JObject)JsonConvert.DeserializeObject(retString);
                    String path = resJson["results"][0]["location"]["path"].ToString();
                    String timezone = resJson["results"][0]["location"]["timezone"].ToString();
                    msg = "����ѯ��λ��Ϊ" + path + "\n\r";
                    for (int i = 0; i < days_range; i++)
                    {
                        String tmp_date = resJson["results"][0]["daily"][i]["date"].ToString();
                        String text_day = resJson["results"][0]["daily"][i]["text_day"].ToString();
                        String text_night = resJson["results"][0]["daily"][i]["text_night"].ToString();
                        
                        String tmp_msg = tmp_date + "��" + "�����������Ϊ" + text_day + "��ҹ���������Ϊ" + text_night + ";";
                        string[] rain_s = { "��", "��", "ѩ", "����" };
                        int flag_day = 1;
                        foreach (var s in rain_s)
                        {
                            if (text_day.Contains(s))
                            {
                                tmp_msg += "�������Я����ɡ��";
                                flag_day = 0;
                                break;
                            }
                        }
                        if (flag_day == 1)
                            tmp_msg += "��������Я����ɡ��";
                        int flag_night = 1;
                        foreach (var s in rain_s)
                        {
                            if (text_night.Contains(s))
                            {
                                flag_night = 0;
                                tmp_msg += "����ҹ��Я����ɡ��";
                                break;
                            }
                        }
                        if (flag_night == 1)
                            tmp_msg += "ҹ������Я����ɡ��";
                        tmp_msg += "\n\r";
                        msg += tmp_msg;
                    }
                }
                else
                {
                    int start;
                    int days;
                    if (date_type == "daterange")
                    {
                        start = start_date.Subtract(now).Days;
                        if (end_date.Subtract(now).Days > 14)
                            days = 15 - start;
                        else
                            days = end_date.Subtract(start_date).Days;
                    }
                    else
                    {
                        start = date.Subtract(now).Days;
                        days = 1;
                    }
                    Console.WriteLine(url_root + url_daily + loc + url_daily2 + start + url_daily3 + days);
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url_root + url_daily + loc + url_daily2 + start + url_daily3 + days);
                    request.Method = "GET";
                    request.ContentType = "charset=UTF-8";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    string retString = reader.ReadToEnd();
                    reader.Close();
                    //test
                    Console.WriteLine("[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[");
                    Console.WriteLine(retString);

                    JObject resJson = (JObject)JsonConvert.DeserializeObject(retString);
                    String path = resJson["results"][0]["location"]["path"].ToString();
                    String timezone = resJson["results"][0]["location"]["timezone"].ToString();
                    msg = "����ѯ��λ��Ϊ" + path + "\n\r";
                    for (int i = 0; i < days; i++)
                    {
                        String tmp_date = resJson["results"][0]["daily"][i]["date"].ToString();
                        String text_day = resJson["results"][0]["daily"][i]["text_day"].ToString();
                        String text_night = resJson["results"][0]["daily"][i]["text_night"].ToString();
                        String high = resJson["results"][0]["daily"][i]["high"].ToString();
                        String low = resJson["results"][0]["daily"][i]["low"].ToString();
                        String wind_direction = resJson["results"][0]["daily"][i]["wind_direction"].ToString();
                        String wind_direction_degree = resJson["results"][0]["daily"][i]["wind_direction_degree"].ToString(); //����Ƕ�
                        String wind_speed = resJson["results"][0]["daily"][i]["wind_speed"].ToString(); //����
                        String wind_scale = resJson["results"][0]["daily"][i]["wind_scale"].ToString(); //�ȼ�
                        String tmp_msg = tmp_date + "��" + "�����������Ϊ" + text_day + "��ҹ���������Ϊ" + text_night + "������¶�" + high + "������¶�" + low + ",��" + wind_scale + "��" + wind_direction + "��\n\r";
                        msg += tmp_msg;
                    }
                }
                
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("ֻ�ܲ�ѯ����"), cancellationToken);
            }
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}