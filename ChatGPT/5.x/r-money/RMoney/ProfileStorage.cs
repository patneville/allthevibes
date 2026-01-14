using System.IO;
using System.Text.Json;

namespace RMoney
{
    public static class ProfileStorage
    {
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public static void Save(string path, UserState state)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(state, Options));
        }

        public static UserState Load(string path)
        {
            return JsonSerializer.Deserialize<UserState>(File.ReadAllText(path), Options) ?? new UserState();
        }
    }
}