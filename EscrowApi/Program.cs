using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Json;
using Newtonsoft.Json.Linq;

namespace EscrowApi
{ 
    //
    class Program
    {
        public static string _accessToken { get; set; }
        public const string ApiLogin = "Login";
        public const string ApiPassword = "Password";

        static void Main(string[] args)
        {
            string _accessToken = Authentication(); //Получение токенов и информации о пользователе

            UsersAdd("Arthur1990@mail.ru"); //Добавление нового пользователя (для того чтобы проводить сделки в системе, необходимо создать как минимум двух пользователей)
            UsersAdd("TechnikSell@mail.ru");

            int contractId = CreateContract(_accessToken); //Создание двухсторонней сделки
            


            AcceptContractByPayer(contractId, _accessToken); //Принятие предложения и оплата (покупателем)
            AcceptContractByPayee(contractId, _accessToken); //Принятие предложения и ввод номера карты для получения средств (продавцом)
            //После принятия предложения пользователями, предложение переходит в сделку


            int dealId = DealGetByContractId(contractId, _accessToken);

            PassportAdd(_accessToken); //Подтверждение паспортных данных, для пользователя TechnikSell@mail.ru, без подтвержденных паспортных данных, продавец не сможет получить деньги
            ConfrimDeal(dealId, _accessToken); //Подтверждение сделки Arthur1990@mail.ru(покупатель)

            Console.ReadKey();
        }


        //метод доступа получения токенов и информации о пользователи

        public static string Authentication()
        {
            var request = WebRequest.Create("http://localhost:96/api/authentication");
            request.ContentType = "application/json";

            request.Method = "POST";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string json =
                    "{\"UserLogin\":\"" + ApiLogin + "\"," +  //UserLogin - логин площадки
                    "\"Password\":\"" + ApiPassword + "\"}"; //Password - пароль площадки 
                streamWriter.Write(json);
            }

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            using (Stream responseStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                var jObj = Newtonsoft.Json.JsonConvert.DeserializeObject(reader.ReadToEnd());
                Console.WriteLine("Метод доступа получения токенов и информации о пользователи:\n");

                Console.WriteLine(jObj);

                JObject obj = JObject.Parse(jObj.ToString());
                JToken token;
                if (obj.TryGetValue("Data", out token))
                {
                    //в ответе получаем AccessToken и подписываем им каждый запрос к сервису. Header: Authentication - bearer {_accessToken}
                    obj = JObject.Parse(token.ToString());
                    if (obj.TryGetValue("AccessToken", out token))
                    {
                        return token.ToString();
                    }
                }
            }

            return "";
        }


        public static int CreateContract(string _accessToken)
        {
            //Продажа цифрового товара(телефон)
            var key = _accessToken;
            var request = WebRequest.Create("http://localhost:96/api/contracts");
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "bearer " + key;

            request.Method = "POST";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string json =
                    "{\"UserLogin\":\"TestUser1\"," + //Логин инициатора (Продавец) 
                    "\"PartnerName\":\"TestUser2\"," + //Логин партнера (Покупателя) 
                    "\"Name\":\"Продажа телефона\"," + //Название сделки
                    "\"InitiatorRole\":\"Payee\"," +  //Роль инициатора сделки (TechnikSell@mail.ru продавец, Arthur1990@mail.ru - покупатель; если указать Payer, то Arthur1990@mail.ru - продавец)
                    "\"ContractDuration\":\"2019-06-28T00:00:00\"," + //Срок действия предложения 
                    "\"PayMethod\":\"WebMoney\"," + //Метод оплаты (CardsTest - для проведения тестовых сделок без движения средств, Cards - настоящие сделки)
                    "\"Amount\":\" 200\"," + //Сумма сделки
                    "\"Currency\":\"RUB\"," + //Валюта с помощью которой производится покупка-продажа телефона
                    "\"CommissionsType\":\"Payee\"," + //Выбор типа комиссии (TechnikSell@mail.ru будет оплачивать комиссию)
                    "\"DepositInitiator\":\" 0\"," + //Залог инициатора сделки
                    "\"DepositPartner\":\" 200\"," + //Залог партнёра по сделке
                    "\"DepositToPayee\":\"true\"," + //Оплата сделки производится за счёт залога Arthur1990@mail.ru
                    "\"DealDuration\":\"2019-06-30T00:00:00\"}"; //Дата окончания действия сделки (после принятия, на сделку отводится 2 дня)
                streamWriter.Write(json);
            }

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            using (Stream responseStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                var jObj = Newtonsoft.Json.JsonConvert.DeserializeObject(reader.ReadToEnd());
                Console.WriteLine("Создание новой сделки:\n");
                Console.WriteLine(jObj);
                
                JObject obj = JObject.Parse(jObj.ToString());
                JToken token;
                if (obj.TryGetValue("Data", out token))
                {
                    obj = JObject.Parse(token.ToString());
                    if (obj.TryGetValue("Id", out token))
                    {
                        return Convert.ToInt32(token);
                    }
                }
            }

            return 0;
        }

        public static void AcceptContract(int id)
        {
            //Продажа цифрового товара(телфон)
            var key = _accessToken;
            var request = WebRequest.Create("http://localhost:96/api/contracts/"+ id +"/accept");
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "bearer " + key;

            request.Method = "POST";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string json =
                      "\"PayeePurse\":\"4564542056989568\"," +  // //Номер банковской карты TechnikSell@mail.ru
                      //Реквезиты банковской карты  Arthur1990@mail.ru
                      "\"CardNumber\":\"5321642056987412\"," + //Номер банковской карты
                      "\"CardHolder\":\"ARTHUR ROMANOV\"," + //Владелец карты
                      "\"CardMonth\":\"01\"," + //Месяц окончания обслуживания карты
                      "\"CardYear\":\"23\"," + //Год окончания обслуживания карты
                      "\"CardCode\":\"121\"," + //CVC код
                      ////////////////////////////////////////////////
                      "\"Phone\":\"89600324534\"," + //Номер телефона Arthur1990@mail.ru для уведомлений по банковскому переводу
                      "\"Email\":\"Arthur1990@mail.ru\"," + //На почту Arthur1990@mail.ru придет уведомление о выполнении операции
                      "\"EnableNotifications\":\"true\"," + //Отправить уведомление на указанный телефон и почту Arthur1990@mail.ru
                      "\"SuccessReturnUrl\":\"TechnikSell.com?successful=true\"," + //URL на который будет перенаправлен пользователь при успешной оплате картой (TechnikSell.com?successful=true при успешной оплате)
                      "\"FailureReturnUrl\":\"TechnikSell.com?fail=true\"," + //URL на который будет перенаправлен пользователь при неудачной оплаты картой (TechnikSell.com?fail=true при ошибке оплаты)
                      //"\"UserPurseId\":\"G9G10G11G12G13G14G15G15\","  //Либо реквизиты карты либо Guid ранее заполненных данных по реквизитам карты, Guid можно получить отсюда http://localhost:96/api/profile/payment-accounts?userLogin=Arthur1990@mail.ru
                      "\"UserLogin\":\"Arthur1990@mail.ru\"}"; //Логин партнера (Покупателя) Arthur1990@mail.ru подтверждает и оплачивает сдлеку
                streamWriter.Write(json);
            }

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            using (Stream responseStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                var jObj = Newtonsoft.Json.JsonConvert.DeserializeObject(reader.ReadToEnd());
                Console.WriteLine("Перевести деньги в систему:\n");
                Console.WriteLine(jObj);
            }
        }

        public static void AcceptContractByPayer(int id, string _accessToken)
        {
            //Этот метод для открытия iframe покупателем на стороне площадки (для ввода данных карты, оплаты и принятия сделки)

            var key = _accessToken;
            var request = WebRequest.Create("https://guarantee.money/card-payment?" +
                "userLogin={Arthur1990@mail.ru}" + //Логин покупателя (Arthur1990@mail.ru)
                "serviceLogin={" + ApiLogin + "}" + //Логин площадки (логин площадки guarantee.money = 1)
                "&contractId={" + id + "}" + //id предложения (id текущей сделки между Arthur1990@mail.ru и TechnikSell@mail.ru)
                "&successReturnUrl={TechnikSell.com?successful=true}" + //URL на который будет перенаправлен пользователь при успешной оплате картой (TechnikSell.com?successful=true при успешной оплате)
                "&failureReturnUrl={TechnikSell.com?fail=true}" + //URL на который будет перенаправлен пользователь при неудачной оплаты картой (TechnikSell.com?fail=true при ошибке оплаты)
                "&payment={1}" + //Payment = 1, для оплаты(Arthur1990@mail.ru вводит все реквезиты карты), так же Payment = 0, для перечисления на карту(TechnikSell@mail.ru указывает только номер крты)
                "&sign={G9G10G11G12G13G14G15G15}" + //подпись
                "&test=1"); //необходимо указывать при тестовой сделке CardsTest
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "bearer " + key;

            request.Method = "GET";
        }

        public static void AcceptContractByPayee(int id, string _accessToken)
        {
            //Этот метод для открытия iframe на стороне площадки (для ввода номера карты и принятия сделки)

            var key = _accessToken;
            var request = WebRequest.Create("https://guarantee.money/card-payment?" +
                                            "userLogin={TechnikSell@mail.ru}" + //Логин покупателя (Arthur1990@mail.ru)
                                            "serviceLogin={" + ApiLogin + "}" + //Логин площадки (логин площадки guarantee.money = 1)
                                            "&contractId={" + id + "}" + //id предложения (id текущей сделки между Arthur1990@mail.ru и TechnikSell@mail.ru)
                                            "&successReturnUrl={TechnikSell.com?successful=true}" + //URL на который будет перенаправлен пользователь при успешной оплате картой (TechnikSell.com?successful=true при успешной оплате)
                                            "&failureReturnUrl={TechnikSell.com?fail=true}" + //URL на который будет перенаправлен пользователь при неудачной оплаты картой (TechnikSell.com?fail=true при ошибке оплаты)
                                            "&payment={1}" + //Payment = 1, для оплаты(Arthur1990@mail.ru вводит все реквезиты карты), так же Payment = 0, для перечисления на карту(TechnikSell@mail.ru указывает только номер крты)
                                            "&sign={G9G10G11G12G13G14G15G15}" + //подпись
                                            "&test=1"); //необходимо указывать при тестовой сделке CardsTest
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "bearer " + key;

            request.Method = "GET";
        }

        public static int DealGetByContractId(int contractId, string _accessToken)
        {
            //Этот метод для получения информации по сделке через идентификатор ID предложения сделки

            var key = _accessToken;
            var request = WebRequest.Create("http://localhost:96/api/deals/get-by-contractId/" + contractId );
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "bearer " + key;

            request.Method = "GET";

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            using (Stream responseStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                var jObj = Newtonsoft.Json.JsonConvert.DeserializeObject(reader.ReadToEnd());
                Console.WriteLine("Перевести деньги в систему:\n");
                Console.WriteLine(jObj);

                return Convert.ToInt32(jObj);
            }
            return 0;
        }

        public static void PassportAdd(string _accessToken)
        {
            var key = _accessToken;
            var request = WebRequest.Create("http://localhost:96/api/profile/passport");
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "bearer " + key;

            request.Method = "POST";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string json =
                //Паспортные данные пользователя TechnikSell@mail.ru
                "{\"FirstName\":\"Александр\"," + //Имя
                "\"Surname\":\"Водяшев\"," + //Фамилия 
                  "\"Patronymic\":\"Анатольевич\"," + //Отчество  
                  "\"BirthdayDate\":\"1990-06-20T15:31:50.597Z\"," + //Дата рождения
                  "\"PassportNumber\":\"659889\"," + //Дата рождения
                  "\"PassportSerial\":\"7858\"," + //Дата рождения
                  "\"PassportIssuedBy\":\"Отделом УФМС по Республике Татарстан в Советском районе города Казнь\"," + //Кем выдан паспорт
                  "\"PassportIssuerCode\":\"12\"," + //Номер отделения, выдавшего паспорт
                  "\"INN\":\"163265457889\"," + //Номер ИНН
                  "\"SNIL\":\"12365412312\"," + //Номер СНИЛС
                  "\"PassportBirthPlace\":\"Город Казань\"," + //Место рождения
                  "\"PassportIssuedDate\":\"2019-06-20T15:31:50.597Z\"," + //Дата выдачи паспорта
                  "\"PhoneNumber\":\"89600124531\"," + //Номер телеофна
                  "\"UserLogin\":\"TechnikSell@mail.ru\"}"; //Логин пользователя
                streamWriter.Write(json);
            }

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            using (Stream responseStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                var jObj = Newtonsoft.Json.JsonConvert.DeserializeObject(reader.ReadToEnd());
                Console.WriteLine("Подтверждение паспортных данных:\n");
                Console.WriteLine(jObj);
            }
        }

        public static void ConfrimDeal(int id, string _accessToken)
        {
            var key = _accessToken;
            var request = WebRequest.Create("http://localhost:96/api/deals/" + id + "/confirm");
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "bearer " + key;

            request.Method = "POST";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string json =
                      "{\"UserLogin\":\"Arthur1990@mail.ru\"}"; //Логин партнера (Покупателя) Arthur1990@mail.ru подтверждает, что товар пришел полностью исправным
                streamWriter.Write(json);
            }

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            using (Stream responseStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                var jObj = Newtonsoft.Json.JsonConvert.DeserializeObject(reader.ReadToEnd());
                Console.WriteLine("Подтверждение сделки:\n");
                Console.WriteLine(jObj);
            }
        }


        public static void UsersAdd(string userLogin)
        {
            var key = _accessToken;//AccessToken площадки
            var request = WebRequest.Create("http://localhost:96/api/registration/service-user");
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "bearer " + key;

            request.Method = "POST";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string json =
                //Данные о пользователи Arthur1990@mail.ru
                "{\"Login\":\"" + userLogin + "\"}"; //Отчество  
                streamWriter.Write(json);
            }

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            using (Stream responseStream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                var jObj = Newtonsoft.Json.JsonConvert.DeserializeObject(reader.ReadToEnd());
                Console.WriteLine("Создание нового пользователя:\n");
                Console.WriteLine(jObj);
            }
        }
    }
}
