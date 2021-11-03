using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace weatherForecast
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ///HttpClient для отправки запросов
        private static readonly HttpClient client = new HttpClient();

        ///Структура для хранения данных о минимальной разнице температур и даты, когда
        ///предположительно будет установлено
        private struct minDif
        {
            public string minDifTemp { get; set; }
            public string minDifDate { get; set; }
        }

        ///Структура для хранения данных о максимальной продолжительности светового дня
        ///и даты, когда это предположительно произойдет
        private struct maxLightDay
        {
            public string maxLDLen { get; set; }
            public string DLDate { get; set; }
        }
        
        ///Структура для хранения данных о минимальной разнице температур и продолждительности
        ///светового дня
        private struct weatherRes
        {
            public minDif minDifTemp { get; set; }
            public maxLightDay maxLD { get; set; }
        }

        public  MainWindow()
        {
            InitializeComponent();       
        }

        /// <summary>
        /// Запрашивает данные, используя API openweathermap, для расчета минимальной разницы ночных температур ("ощущаемой" и реальной) 
        /// (если в задании подразумевается разность температур без возведения в модуль),
        /// а так же данные для расчета продолжительности светового дня. 
        /// </summary>
        /// <returns>Возвращает структуру, содержащую минимальную разницу температур, дату когда
        /// данная разница предположительно будет зафиксированна, максимальную продолжительность 
        /// светового дня в часах, дату когда предположительно это произойдет</returns>
        private async Task<weatherRes> getWeatherForecast()
        {
            weatherRes res = new weatherRes();
            try
            {
                //Запрашиваем данные для расчета минимальной разницы температур
                var responseString = await client.GetAsync("http://api.openweathermap.org/data/2.5/forecast?id=539839&units=metric&appid=a1844320d6a512c4c61a813ca116763d");
                var json = await responseString.Content.ReadAsStringAsync();
                //Десериализум полученные данные в динамический тип
                dynamic weather = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
                IEnumerable<dynamic> listWeather = (IEnumerable<dynamic>)weather.list;
                minDif resMin = getDateMinDif(listWeather);
                res.minDifTemp = resMin;
                
                //Запрашиваем данные для расчета максимальной продолжительности дня
                responseString = await client.GetAsync("http://api.openweathermap.org/data/2.5/onecall?lat=59.907&lon=30.512&units=metric&exclude=current,minutely,hourly,alerts,hourly&appid=a1844320d6a512c4c61a813ca116763d");
                json = await responseString.Content.ReadAsStringAsync();
                //Десериализум полученные данные в динамический тип
                weather = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
                listWeather = (IEnumerable<dynamic>)weather.daily;
                maxLightDay resMax = getMaxLightDate(listWeather);
                res.maxLD = resMax;
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
            return res;
        }

        /// <summary>
        /// Извлекает данные о минимальной разнице температур в ночное время (если в задании подразумевается
        /// разность температур без возведения в модуль)
        /// и получает дату когда это предположительно будет зафиксированно
        /// </summary>
        /// <param name="listWeather"> содержит десериализованные из JSON почасовые данные о погоде </param>
        /// <returns>Возвращет структуру, содержащую данные с минимальной разницей температур в ночное время
        /// и дату когда это будет предположительно зафиксированно</returns>
        private minDif getDateMinDif(IEnumerable<dynamic> listWeather)
        {
            minDif res = new minDif();
            res.minDifDate = "";
            double minDif = double.MaxValue;

            foreach (var item in listWeather)
            {
                if(item.sys.pod == "n")
                {
                    double dif = Convert.ToDouble(item.main.feels_like) - Convert.ToDouble(item.main.temp);
                    //Если разница температур меньше чем данные которые мы получили ранее (если в задании подразумевается
                    /// разность температур без возведения в модуль)
                    if (minDif > dif)
                    {
                        minDif = dif;
                        res.minDifDate = item.dt_txt;
                    }
                }
            }
            res.minDifTemp = minDif.ToString();
            res.minDifDate = res.minDifDate.Substring(0, 10);
            return res;
        }

        /// <summary>
        /// Извлекает данные о максимальной продолжительности дня и дату когда это
        /// предположительно произойдет
        /// </summary>
        /// <param name="listWeather">содержит десериализованные из JSON ежедневный прогназ погоды </param>
        /// <returns>Возвращет структуру, содержащую данные с максимальной продолжительностью дня в часах
        /// и дату когда это будет предположительно зафиксированно</returns>
        private maxLightDay getMaxLightDate(IEnumerable<dynamic> listWeather)
        {
            maxLightDay res = new maxLightDay();
            res.DLDate = "";
            int maxDL = int.MinValue;

            foreach (var item in listWeather)
            {
                int dif = Convert.ToInt32(item.sunset) - Convert.ToInt32(item.sunrise);
                //Если продолжительность дна больше чем уже которые мы находили
                if (maxDL < dif)
                {
                    maxDL = dif;
                    res.DLDate = Convert.ToString(item.dt);
                }
            }
            //Устанавливаем продолжительность дня в часах
            res.maxLDLen = ((double)maxDL/3600).ToString();
            res.DLDate = UnixTimeStampToDateTime(Convert.ToDouble(res.DLDate)).ToString("dd-MM-yyyy");
            return res;
        }

        /// <summary>
        /// Преобразует время из формата Unix UTC в DateTime
        /// </summary>
        /// <param name="unixTimeStamp">время в формате Unix UTC</param>
        /// <returns>Возвращет преобразованную дату в DateTime</returns>
        private DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        /// <summary>
        /// Заполняет поля labl на форме данными о минимальной разнице температур, даты, 
        /// максимальной продолжительности дня и даты когда это предположительно произойдет
        /// </summary>
        /// <param name="WD">структура, которая содержит данные о минимальной разнице температур, даты, 
        /// максимальной продолжительности дня и даты когда это предположительно произойдет</param>
        private void setWeatherData(weatherRes WD)
        {
            //Устанавливаем минимальную разницу температур и дату
            lblDayMinDif.Content = WD.minDifTemp.minDifTemp + "С,  Дата: " + WD.minDifTemp.minDifDate;

            //Устанавливаем максимальную продолжительность дня и дату
            lblMaxLightDay.Content = WD.maxLD.maxLDLen + " часов, Дата: " + WD.maxLD.DLDate;
        }

        /// <summary>
        /// Обработчик нажатия кнопки Обновить. Запрашивает данные о погоде и заполняет форму, 
        /// полученными данными. Работает асинхронно.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                weatherRes res = new weatherRes();
                res = await getWeatherForecast();

                setWeatherData(res);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
