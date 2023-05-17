using Telegram.Bot;

namespace Monobank
{
    public class UserProfile
    {
        public long user_tg_id;
        public string account_name;
        public string monobank_name;
        public string balance;
        public string monocard;
        public UserProfile(long user_tg_id, string account_name, string balance, string mononame, string monocard)
        {
            this.user_tg_id = user_tg_id;
            this.account_name = account_name;
            this.balance = balance;
            this.monobank_name= mononame;
            this.monocard = monocard;
        }
        public UserProfile()
        {

        }
        public async Task<UserProfile> GetUserProfileAsync(long user_tg_id)
        {
            HttpClient GetUserProfileAsync_client = new HttpClient();

            var task1 = GetUserProfileAsync_client.GetStringAsync($"https://{constants.host}/Monobank/get_users_firstname/{user_tg_id}");
            var task2 = GetUserProfileAsync_client.GetStringAsync($"https://{constants.host}/Monobank/get_balance_from_base/{user_tg_id}");
            var task3 = GetUserProfileAsync_client.GetStringAsync($"https://{constants.host}/Monobank/get_monobank_name/{user_tg_id}");
            var task4 = GetUserProfileAsync_client.GetStringAsync($"https://{constants.host}/Monobank/get_card/{user_tg_id}");

            await Task.WhenAll(task1, task2, task3, task4);

            string result1 = task1.Result;
            string result2 = task2.Result;
            string result3 = task3.Result;
            string result4 = task4.Result;

            return new UserProfile(user_tg_id, result1, result2, result3, result4);
        }

        public async Task SendProfile(ITelegramBotClient botClient, UserProfile user)
        {
            await botClient.SendTextMessageAsync(user_tg_id, $"Інформація про ваш акаунт\n\t\t\t--- Ім'я акаунта: {account_name}\n\t\t\t--- Баланс: {balance}\nМонобанк\n\t\t\t--- Ім'я: {monobank_name}\n\t\t\t--- Номера підв'язаних карт: \n-----------------------------\n{monocard}-----------------------------");
        }
    }
}
