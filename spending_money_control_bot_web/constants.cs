using MongoDB.Bson;
using MongoDB.Driver;

namespace Monobank
{
    public class constants
    {
        public static string host = "spendingmoneycontrolapi20230428130151.azurewebsites.net";
        public static string token;
        public static string botId = "6033500949:AAGLxuMrDbgEXgOUE6Y5L_itJQUc8jUvsj4";
        public static MongoClient mongoClient;
        public static IMongoDatabase database;
        public static IMongoCollection<BsonDocument> collection;
        public static IMongoCollection<BsonDocument> collection2;
        public static string urlUserInfo = $"https://api.monobank.ua/personal/client-info";
    }
}
