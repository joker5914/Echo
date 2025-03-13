using Microsoft.Data.Sqlite;

public static class DatabaseHelper
{
    private static readonly string dbPath = "event_log.db";

    // ✅ Creates a new database connection
    public static SqliteConnection GetConnection()
    {
        var connection = new SqliteConnection($"Data Source={dbPath};");
        connection.Open();
        return connection;
    }

    // ✅ Initializes the database (called once at startup)
    public static void InitializeDatabase()
    {
        using (var connection = GetConnection())
        {
            string createEventsTable = @"CREATE TABLE IF NOT EXISTS Events (
                                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                            Name TEXT NOT NULL UNIQUE,
                                            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                                        );";

            string createTransactionsTable = @"CREATE TABLE IF NOT EXISTS Transactions (
                                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                EventId INTEGER NOT NULL,
                                                CardNumber TEXT NOT NULL,
                                                CheckInTime DATETIME NULL,
                                                CheckOutTime DATETIME NULL,
                                                FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
                                            );";

            ExecuteNonQuery(connection, createEventsTable);
            ExecuteNonQuery(connection, createTransactionsTable);
        }
    }

    // ✅ Helper method to execute INSERT, UPDATE, DELETE queries
    public static void ExecuteNonQuery(SqliteConnection connection, string query, Dictionary<string, object>? parameters = null)
    {
        using (var cmd = new SqliteCommand(query, connection))
        {
            if (parameters != null)
            {
                foreach (var param in parameters)
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
            }
            cmd.ExecuteNonQuery();
        }
    }

    // ✅ Retrieves a list of all events
    public static List<(int Id, string Name, string CreatedAt)> GetEvents()
    {
        var events = new List<(int, string, string)>();
        using (var connection = GetConnection())
        using (var cmd = new SqliteCommand("SELECT Id, Name, CreatedAt FROM Events", connection))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                events.Add((Convert.ToInt32(reader["Id"]), reader["Name"].ToString(), reader["CreatedAt"].ToString()));
            }
        }
        return events;
    }

    // ✅ Inserts a new event
    public static void CreateEvent(string eventName)
    {
        using (var connection = GetConnection())
        {
            ExecuteNonQuery(connection, "INSERT INTO Events (Name) VALUES (@name)",
                new Dictionary<string, object> { { "@name", eventName } });
        }
    }

    // ✅ Deletes an event and its transactions
    public static void DeleteEvent(int eventId)
    {
        using (var connection = GetConnection())
        {
            ExecuteNonQuery(connection, "DELETE FROM Events WHERE Id = @eventId",
                new Dictionary<string, object> { { "@eventId", eventId } });
        }
    }
}
