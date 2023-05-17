using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Monobank;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

class Program
{

    static ITelegramBotClient bot = new TelegramBotClient(constants.botId);
    static HttpClient httpClient = new HttpClient();
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message && update.Message != null && !string.IsNullOrEmpty(update.Message.Text))
        {
            var message = update.Message;

            User user = message.From;
            string user_firstname = user.FirstName;
            long user_id = user.Id;


            var transactionsList = new List<Transactions>();


            var document = new BsonDocument
                    {
                        { "user_id", user_id},
                        { "user_firstname", user_firstname },
                        { "monotoken", "" },
                        { "balance", 0.0 },
                        { "bot_waiting_for_user_token", false },
                        {"bot_waiting_for_transaction_index", false },
                        {"bot_waiting_for_new_transaction", false },
                        {"mononame", "" },
                        {"monocard", new BsonArray(new string[] {"Пусто"})},
                        {"card_id", new BsonArray(new string[] { }) },

                {"last_command_call_time", new BsonDateTime(DateTime.Now.AddMinutes(-5)) },
                {"first_appearance", new BsonDateTime(DateTime.Now) },
                {"transactions", new BsonArray(transactionsList.Select(t => t.ToBsonDocument())) }
                };

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var exists = constants.collection.Find(filter).Any();

            if (!exists)
            {
                constants.collection.InsertOne(document);
            }

            var resp = await httpClient.GetAsync($"https://{constants.host}/Monobank/bot_waiting_for_token/{user_id}");
            var res = await resp.Content.ReadAsStringAsync();
            bool bot_waiting_for_user_token = Convert.ToBoolean(res);
            resp = await httpClient.GetAsync($"https://{constants.host}/Monobank/bot_waiting_transaction_index/{user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_waiting_for_transaction_index = Convert.ToBoolean(res);
            resp = await httpClient.GetAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_waiting_for_new_transaction = Convert.ToBoolean(res);



            filter = Builders<BsonDocument>.Filter.Empty;
            var document003 = constants.collection2.Find(filter).FirstOrDefault();
            double dollar = Convert.ToDouble(document003["dollar"]);
            double euro = Convert.ToDouble(document003["euro"]);

            filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);

            if (message.Text.ToLower() == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Привіт, {user_firstname}. Щоб підв'язати свій монобанк, потрібно вписати токен. Взяти його можна звідси: https://api.monobank.ua/ \n\nЩоб додати токен впишіть /add_token\n\nПовний список команд:\n/start - запустити бота\n/add_transaction - додати транзакцію\n/delete_transaction - видалити транзакцію\n/my_profile - мій профіль\n/exchange_rate - актуальний курс валют\n/add_token - додати токен\n/my_token - мій токен\n/delete_token - видалити токен\n/day_transactions - транзакції за день\n/week_transactions - транзакції за неділю\n/month_transactions - транзакції за місяць");
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_token/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_transaction_index/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}?b=false", null);
                return;
            }

            if (message.Text.ToLower() == "/add_token")
            {
                var response = await httpClient.GetAsync($"https://{constants.host}/Monobank/get_monotoken/{user_id}");
                var result = await response.Content.ReadAsStringAsync();

                var filter6 = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
                var document6 = constants.collection.Find(filter6).FirstOrDefault();
                document6.TryGetValue("last_command_call_time", out BsonValue lastCallTime);
                DateTime lastCommandCallTime = lastCallTime.ToUniversalTime();


                if (DateTime.UtcNow - lastCommandCallTime < TimeSpan.FromMinutes(1))
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Ви занадто часто використовуєте команди. Спробуйте через хвилину");
                    return;
                }


                if (result == "")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Добре. Відправте ваш токен");
                    await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_token/{user_id}?b=true", null);

                    var filter7 = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
                    var update7 = Builders<BsonDocument>.Update.Set("last_command_call_time", DateTime.Now);
                    constants.collection.UpdateOne(filter7, update7);
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Ви вже додали токен. Якщо ви хочете його видалити, використайте команду /delete_token");
                    var filter7 = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
                    var update7 = Builders<BsonDocument>.Update.Set("last_command_call_time", DateTime.Now.AddMinutes(-5));
                    constants.collection.UpdateOne(filter7, update7);
                }
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_transaction_index/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}?b=false", null);
                return;
            }
            if (message.Text.ToLower() == "/my_token")
            {
                var response = await httpClient.GetAsync($"https://{constants.host}/Monobank/get_monotoken/{user_id}");
                var result = await response.Content.ReadAsStringAsync();
                if (result == "")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Ви ще не додали токен. Для того, щоб додати токен, використайте команду /add_token");
                }

                else await botClient.SendTextMessageAsync(message.Chat, $"Ваш токен: {result}");
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_token/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_transaction_index/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}?b=false", null);
                return;
            }
            if (message.Text.ToLower() == "/delete_token")
            {
                HttpClient httpclient66 = new HttpClient();
                var response = await httpclient66.GetAsync($"https://{constants.host}/Monobank/get_monotoken/{user_id}");
                var result = await response.Content.ReadAsStringAsync();

                var filter6 = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
                var document6 = constants.collection.Find(filter6).FirstOrDefault();
                document6.TryGetValue("last_command_call_time", out BsonValue lastCallTime);
                DateTime lastCommandCallTime = lastCallTime.ToUniversalTime();


                if (DateTime.UtcNow - lastCommandCallTime < TimeSpan.FromMinutes(1))
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Ви занадто часто використовуєте команди. Спробуйте через хвилину");
                    return;
                }


                if (result == "")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Ви ще не додали токен. Для того, щоб додати токен, використайте команду /add_token");
                }
                else
                {
                    await httpClient.DeleteAsync($"https://{constants.host}/Monobank/delete_using_token/{user_id}?token={result}");
                    await botClient.SendTextMessageAsync(message.Chat, "Токен успішно видалено, інформація акаунта оновлена. Якщо ви хочете додати новий - використайте команду /add_token");

                    var filter7 = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
                    var update7 = Builders<BsonDocument>.Update.Set("last_command_call_time", DateTime.Now);
                    constants.collection.UpdateOne(filter7, update7);
                }
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_token/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_transaction_index/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}?b=false", null);
                return;
            }
            if (message.Text.ToLower() == "/my_profile")
            {
                UserProfile user1 = await new UserProfile().GetUserProfileAsync(user_id);
                await user1.SendProfile(botClient, user1);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_token/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_transaction_index/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}?b=false", null);
                return;
            }
            if (message.Text.ToLower() == "/exchange_rate")
            {
                await httpClient.PostAsync($"https://{constants.host}/Monobank/send_exchange_rate?id={user_id}", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_token/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_transaction_index/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}?b=false", null);
                return;
            }
            if (message.Text.ToLower() == "/day_transactions")
            {
                DateTime daytime = DateTime.Today;
                DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                TimeSpan span = daytime.ToUniversalTime().Subtract(unixEpoch);
                long unixTime = Convert.ToInt32(span.TotalSeconds);

                var response = await httpClient.GetAsync($"https://spendingmoneycontrolapi20230428130151.azurewebsites.net/Monobank/get_user_transactions?id={user_id}&unixtime={unixTime}");
                var result = await response.Content.ReadAsStringAsync();

                if (result != "[]")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Транзакції за цей день:");
                    var transactions = result.Split(new string[] { "\",\"" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var transaction in transactions)
                    {
                        var cleanedTransaction = transaction.Replace("\"", "").Replace("\\n", "\n");
                        await botClient.SendTextMessageAsync(message.Chat, cleanedTransaction.Trim('[', ']', ' '));
                    }
                }
                else await botClient.SendTextMessageAsync(message.Chat, "Транзакцій за цей день немає");

                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_token/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_transaction_index/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}?b=false", null);
                return;
            }
            if (message.Text.ToLower() == "/week_transactions")
            {
                DateTime daytime = DateTime.Today.AddDays(-7);
                DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                TimeSpan span = daytime.ToUniversalTime().Subtract(unixEpoch);
                long unixTime = Convert.ToInt32(span.TotalSeconds);

                var response = await httpClient.GetAsync($"https://spendingmoneycontrolapi20230428130151.azurewebsites.net/Monobank/get_user_transactions?id={user_id}&unixtime={unixTime}");
                var result = await response.Content.ReadAsStringAsync();

                if (result != "[]")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Транзакції за цю неділю:");
                    var transactions = result.Split(new string[] { "\",\"" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var transaction in transactions)
                    {
                        var cleanedTransaction = transaction.Replace("\"", "").Replace("\\n", "\n");
                        await botClient.SendTextMessageAsync(message.Chat, cleanedTransaction.Trim('[', ']', ' '));
                    }
                }
                else await botClient.SendTextMessageAsync(message.Chat, "Транзакцій за цю неделю немає");

                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_token/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_transaction_index/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}?b=false", null);
                return;
            }
            if (message.Text.ToLower() == "/month_transactions")
            {
                DateTime daytime = DateTime.Today.AddDays(-30);
                DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                TimeSpan span = daytime.ToUniversalTime().Subtract(unixEpoch);
                long unixTime = Convert.ToInt32(span.TotalSeconds);

                var response = await httpClient.GetAsync($"https://spendingmoneycontrolapi20230428130151.azurewebsites.net/Monobank/get_user_transactions?id={user_id}&unixtime={unixTime}");
                var result = await response.Content.ReadAsStringAsync();

                if (result != "[]")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Транзакції за цей місяць:");
                    var transactions = result.Split(new string[] { "\",\"" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var transaction in transactions)
                    {
                        var cleanedTransaction = transaction.Replace("\"", "").Replace("\\n", "\n");
                        await botClient.SendTextMessageAsync(message.Chat, cleanedTransaction.Trim('[', ']', ' '));
                    }
                }
                else await botClient.SendTextMessageAsync(message.Chat, "Транзакцій за цей місяць немає");

                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_token/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_transaction_index/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}?b=false", null);
                return;
            }
            if (message.Text.ToLower() == "/delete_transaction")
            {
                var response = await httpClient.GetAsync($"https://spendingmoneycontrolapi20230428130151.azurewebsites.net/Monobank/get_user_transactions?id={user_id}&unixtime=1");
                var result = await response.Content.ReadAsStringAsync();
                if (result == "[]") await botClient.SendTextMessageAsync(message.Chat, "Список ваших транзакцій порожній");
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Добре. Введіть номер транзакції, яку ви хочете видалити");
                    await httpClient.PutAsync($"https://spendingmoneycontrolapi20230428130151.azurewebsites.net/Monobank/bot_waiting_transaction_index/{user_id}?b=true", null);
                }
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_token/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}?b=false", null);
                return;
            }
            if (message.Text.ToLower() == "/add_transaction")
            {
                await botClient.SendTextMessageAsync(message.Chat, "Добре. Відправте мені повідомлення у вигляді: ('назва транзакції', 'сума', 'валюта'),\nнаприклад: (овочі, -50.4, UAH)");
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}?b=true", null);
                return;
            }



            if (message.Text.Contains("я хочу"))
            {
                await botClient.SendTextMessageAsync(message.Chat, "Я теж багато чого хочу");
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_token/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_transaction_index/{user_id}?b=false", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}?b=false", null);
                return;
            }

            if (bot_waiting_for_user_token)
            {
                var user_token = message.Text;
                var update_using_token_response = await httpClient.PutAsync($"https://{constants.host}/Monobank/update_using_token/{user_id}?token={user_token}", null);
                await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_token/{user_id}?b=false", null);
                if (update_using_token_response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    await botClient.SendTextMessageAsync(message.Chat, $"Токен успішно збережено. Інформація про ваш акаунт оновлена.");
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat, $"Неправильний токен.");
                    var filter7 = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
                    var update7 = Builders<BsonDocument>.Update.Set("last_command_call_time", DateTime.Now.AddMinutes(-5));
                    constants.collection.UpdateOne(filter7, update7);
                }


                return;
            }
            if (bot_waiting_for_transaction_index)
            {
                var index = message.Text;
                var response = await httpClient.DeleteAsync($"https://spendingmoneycontrolapi20230428130151.azurewebsites.net/Monobank/delete_utransaction/{user_id}?index={index}");
                await httpClient.PutAsync($"https://spendingmoneycontrolapi20230428130151.azurewebsites.net/Monobank/bot_waiting_transaction_index/{user_id}?b=false", null);

                if (response.StatusCode != System.Net.HttpStatusCode.BadRequest) botClient.SendTextMessageAsync(message.Chat, "Транзакція видалена");

                else await botClient.SendTextMessageAsync(message.Chat, "Помилка");
                return;
            }
            if (bot_waiting_for_new_transaction)
            {
                try
                {

                    var tr_text = message.Text;
                    string[] parts = tr_text.Split(',');

                    string description = parts[0].Trim();
                    string operationAmountString = parts[1].Trim();
                    string currencyCodeString = parts[2].Trim();

                    DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    TimeSpan span = DateTime.Now.ToUniversalTime().Subtract(unixEpoch);
                    long unixTime = Convert.ToInt32(span.TotalSeconds);

                    long curcd = 0;
                    switch (currencyCodeString)
                    {
                        case "UAH":
                            curcd = 980;
                            break;
                        case "USD":
                            curcd = 840;
                            break;
                        case "EUR":
                            curcd = 978;
                            break;
                    }

                    Transactions transaction = new Transactions
                    {
                        Description = description,
                        OperationAmount = Convert.ToInt64(Convert.ToDouble(operationAmountString) * 100.0),
                        CurrencyCode = curcd,
                        Time = unixTime


                    };


                    switch (curcd)
                    {
                        case 980:
                            await httpClient.PutAsync($"https://{constants.host}/Monobank/update_balance/{user_id}?balance={Convert.ToDouble(operationAmountString)}", null);
                            break;
                        case 840:
                            await httpClient.PutAsync($"https://{constants.host}/Monobank/update_balance/{user_id}?balance={Convert.ToDouble(operationAmountString) * dollar}", null);
                            break;
                        case 978:
                            await httpClient.PutAsync($"https://{constants.host}/Monobank/update_balance/{user_id}?balance={Convert.ToDouble(operationAmountString) * euro}", null);
                            break;
                    }



                    var filter001 = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
                    var document001 = constants.collection.Find(filter001).FirstOrDefault();
                    var last_transactions = document001["transactions"].AsBsonArray;
                    last_transactions.Add(transaction.ToBsonDocument());

                    var update11 = Builders<BsonDocument>.Update.Set("transactions", last_transactions);
                    constants.collection.UpdateOne(filter001, update11);
                    await botClient.SendTextMessageAsync(user_id, "Успішно додано");
                    await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}?b=false", null);
                }
                catch
                {
                    await botClient.SendTextMessageAsync(user_id, "Помилка");
                    await httpClient.PutAsync($"https://{constants.host}/Monobank/bot_waiting_for_new_transaction/{user_id}?b=false", null);
                }

                return;
            }

            await botClient.SendTextMessageAsync(message.Chat, "Я не розумію, що ти хочеш.");
        }
        else
        {
            if (update.Message != null && update.Message.From != null)
            {
                long user_id = update.Message.From.Id;
                await botClient.SendTextMessageAsync(user_id, "Не треба мене ламати. Я розрахований тільки на текстові повідомлення");
            }
        }

    }

    public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
    }
    public static async Task UpdateTransactions()
    {
        try
        {
            ITelegramBotClient bott = new TelegramBotClient(constants.botId);
            while (true)
            {
                Thread.Sleep(120000);

                var filter = Builders<BsonDocument>.Filter.Empty;
                var document003 = constants.collection2.Find(filter).FirstOrDefault();
                double dollar = Convert.ToDouble(document003["dollar"]);
                double euro = Convert.ToDouble(document003["euro"]);




                filter = Builders<BsonDocument>.Filter.Empty;
                var documents = constants.collection.Find(filter).ToList();

                foreach (var document in documents)
                {
                    HttpClient UpdateTransactions_client = new HttpClient();
                    string tok = Convert.ToString(document["monotoken"]);


                    var filter7 = Builders<BsonDocument>.Filter.Eq("user_id", Convert.ToInt64(document["user_id"]));
                    var update = Builders<BsonDocument>.Update.Set("first_appearance", DateTime.Now);

                    constants.collection.UpdateOne(filter7, update);


                    if (tok != "")
                    {
                        document.TryGetValue("first_appearance", out BsonValue first_appearance);
                        DateTime firstapp = first_appearance.AsDateTime;

                        DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        TimeSpan span = firstapp.ToUniversalTime().Subtract(unixEpoch);
                        long unixTime = Convert.ToInt32(span.TotalSeconds);

                        UpdateTransactions_client.DefaultRequestHeaders.Add("X-Token", tok);

                        var card_ids = document["card_id"].AsBsonArray;
                        foreach (var cardid in card_ids)
                        {
                            string urlTransactions = $"https://api.monobank.ua/personal/statement/{cardid}/{unixTime}/";
                            HttpResponseMessage responseTransactions = await UpdateTransactions_client.GetAsync(urlTransactions);
                            string responseBodyTransactions = await responseTransactions.Content.ReadAsStringAsync();

                            var last_transactions = document["transactions"].AsBsonArray;
                            var user_id_tr = Convert.ToInt64(document["user_id"]);
                            try
                            {
                                var transactions = JsonConvert.DeserializeObject<Transactions[]>(responseBodyTransactions);


                                foreach (var transaction in transactions)
                                {
                                    string curCode = null;
                                    switch (transaction.CurrencyCode)
                                    {
                                        case 980:
                                            curCode = "UAH";
                                            break;
                                        case 840:
                                            curCode = "USD";
                                            break;
                                        case 978:
                                            curCode = "EUR";
                                            break;
                                        default:
                                            curCode = null;
                                            break;
                                    }
                                    bott.SendTextMessageAsync(user_id_tr, $"Нова транзакція!\nЧас(UTC+0): {new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(transaction.Time)}\nОпис: {transaction.Description}\nСума: {transaction.OperationAmount / 100.0:f2} {curCode}");




                                    if (curCode == "UAH") await UpdateTransactions_client.PutAsync($"https://{constants.host}/Monobank/update_balance/{user_id_tr}?balance={transaction.OperationAmount / 100.0}", null);
                                    else if (curCode == "USD") await UpdateTransactions_client.PutAsync($"https://{constants.host}/Monobank/update_balance/{user_id_tr}?balance={transaction.OperationAmount / 100.0 * dollar}", null);
                                    else if (curCode == "EUR") await UpdateTransactions_client.PutAsync($"https://{constants.host}/Monobank/update_balance/{user_id_tr}?balance={transaction.OperationAmount / 100.0 * euro}", null);




                                    last_transactions.Add(transaction.ToBsonDocument());
                                }
                            }
                            catch
                            {
                            }
                            update = Builders<BsonDocument>.Update.Set("transactions", last_transactions);
                            constants.collection.UpdateOne(filter7, update);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            bot.SendTextMessageAsync(710503033, ex.Message);
        }
    }
    public static async Task UpdateExchangeRates()
    {
        while (true)
        {
            Thread.Sleep(310000);
            dynamic dollars = null;
            dynamic euro = null;
            var responsecurr = await httpClient.GetAsync("https://api.monobank.ua/bank/currency");
            var content = await responsecurr.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(content);

            foreach (var currency in data)
            {
                if (currency.currencyCodeA == 840 && currency.currencyCodeB == 980)
                {
                    dollars = currency.rateBuy;
                }
                if (currency.currencyCodeA == 978 && currency.currencyCodeB == 980)
                {
                    euro = currency.rateBuy;
                }
            }


            var document = new BsonDocument
        {
            { "dollar", Convert.ToDouble(dollars) },
            { "euro", Convert.ToDouble(euro) }
        };
            var filter = Builders<BsonDocument>.Filter.Empty;
            var exist = constants.collection2.Find(filter).Any();
            if (exist)
            {
                constants.collection2.ReplaceOneAsync(filter, document);
            }
            else
            {
                constants.collection2.InsertOne(document);
            }
        }
    }


    static void Main(string[] args)
    {

        Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);

        constants.mongoClient = new MongoClient("mongodb+srv://user1:qwerty123@cluster0.vkmbyoc.mongodb.net/test");
        constants.database = constants.mongoClient.GetDatabase("base1");
        constants.collection = constants.database.GetCollection<BsonDocument>("tg-names");
        constants.collection2 = constants.database.GetCollection<BsonDocument>("collection1");

        Task.Run(async () => await UpdateTransactions());
        Task.Run(async () => await UpdateExchangeRates());

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { },
        };
        bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
        //Console.ReadLine();
    }
}
//
//