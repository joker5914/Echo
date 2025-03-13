using Microsoft.Data.Sqlite;

class Program
{
    static string dbPath = "event_log.db";
    static SqliteConnection? connection;

    static void Main()
    {
        DatabaseHelper.InitializeDatabase();
        while (true)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Clear();
            Console.WriteLine("Event Management System");
            Console.WriteLine("1. Create Event");
            Console.WriteLine("2. List Events");
            Console.WriteLine("3. Select Event for Check-In/Out");
            Console.WriteLine("4. View Live Attendee List");
            Console.WriteLine("5. Export Event Data to CSV");
            Console.WriteLine("6. Delete Event");
            Console.WriteLine("7. Admin Dashboard");
            Console.WriteLine("8. Exit");
            Console.Write("Select an option: ");

            switch (Console.ReadLine())
            {
                case "1":
                    CreateEvent();
                    break;
                case "2":
                    ListEvents();
                    break;
                case "3":
                    HandleCardSwipes();
                    break;
                case "4":
                    ShowLiveAttendeeList();
                    break;
                case "5":
                    ExportEventData();
                    break;
                case "6":
                    DeleteEvent();
                    break;
                case "7":
                    AdminDashboard();
                    break;
                case "8":
                    return;
                default:
                    Console.WriteLine("Invalid option. Press any key to continue...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    static void CreateEvent()
    {
        Console.Clear();
        Console.WriteLine("🎉 ====================== Create a New Event ====================== 🎉\n");

        Console.Write("📌 Enter event name (or press Enter to cancel): ");
        string eventName = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(eventName))
        {
            Console.WriteLine("\n🔙 Event creation canceled. Returning to main menu...");
            Console.ReadLine();
            return;
        }

        DatabaseHelper.CreateEvent(eventName);

        Console.WriteLine($"\n✅ Event '{eventName}' has been successfully created!");
        Console.WriteLine("\n==================================================================");
        Console.WriteLine("🔙 Press Enter to return to the main menu...");
        Console.ReadLine();
    }

    static void ListEvents()
    {
        Console.Clear();
        Console.WriteLine("📅 ======================== Event List ======================== 📅\n");

        var events = DatabaseHelper.GetEvents();

        if (events.Count == 0)
        {
            Console.WriteLine("⚠ No events have been created yet.");
        }
        else
        {
            Console.WriteLine("ID   | Event Name                     | Created At");
            Console.WriteLine("------------------------------------------------------------");

            foreach (var ev in events)
            {
                Console.WriteLine($"{ev.Id,-4} | {ev.Name,-30} | {ev.CreatedAt}");
            }
        }

        Console.WriteLine("\n==============================================================");
        Console.WriteLine("🔙 Press Enter to return to the main menu...");
        Console.ReadLine(); // ✅ Wait for user input before returning
    }

    static int SelectEvent()
    {
        var events = DatabaseHelper.GetEvents();

        if (events.Count == 0)
        {
            Console.WriteLine("⚠ No events have been created yet.");
            Console.WriteLine("\n🔙 Press Enter to return to the main menu...");
            Console.ReadLine();
            return -1;
        }

        Console.WriteLine("  #  | Event Name                     | Created At");
        Console.WriteLine("--------------------------------------------------------------");

        Dictionary<int, int> eventMap = new Dictionary<int, int>();
        int optionNum = 1;

        foreach (var ev in events)
        {
            eventMap[optionNum] = ev.Id;
            Console.WriteLine($" {optionNum,-3} | {ev.Name,-30} | {ev.CreatedAt}");
            optionNum++;
        }

        Console.WriteLine("--------------------------------------------------------------");
        Console.WriteLine("  0  | 🔙 Go Back");

        Console.Write("\n🔎 Select an event (enter number): ");
        if (int.TryParse(Console.ReadLine(), out int selection))
        {
            if (selection == 0) return -1; // ✅ User chose "Go Back"
            if (eventMap.ContainsKey(selection)) return eventMap[selection];
        }

        Console.WriteLine("❌ Invalid selection. Try again.");
        return SelectEvent();
    }

    static void DeleteEvent()
    {
        Console.Clear();
        Console.WriteLine("🗑 ===================== Delete an Event ===================== 🗑\n");

        int eventId = SelectEvent();

        if (eventId == -1)
        {
            Console.WriteLine("\n🔙 Returning to the main menu...");
            Console.ReadLine();
            return;
        }

        string eventName = "";

        // ✅ Get the Event Name
        using (var connection = DatabaseHelper.GetConnection())
        using (var eventCmd = new SqliteCommand("SELECT Name FROM Events WHERE Id = @eventId", connection))
        {
            eventCmd.Parameters.AddWithValue("@eventId", eventId);
            using (var reader = eventCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    eventName = reader["Name"].ToString();
                }
                else
                {
                    Console.WriteLine("⚠ No event found with this ID.");
                    Console.ReadLine();
                    return;
                }
            }
        }

        // ✅ Confirm Before Deleting
        Console.Write($"\n⚠ Are you sure you want to delete the event '{eventName}' and all its transactions? (y/n): ");
        string confirmation = Console.ReadLine()?.Trim().ToLower();

        if (confirmation != "y")
        {
            Console.WriteLine("\n❌ Event deletion canceled.");
            Console.WriteLine("\n🔙 Press Enter to return to the main menu...");
            Console.ReadLine();
            return;
        }

        // ✅ Ensure related transactions are deleted before deleting the event
        using (var connection = DatabaseHelper.GetConnection())
        {
            using (var transaction = connection.BeginTransaction()) // ✅ Start a transaction
            {
                try
                {
                    // ✅ Delete all transactions for this event (WITHIN TRANSACTION)
                    using (var deleteTransactionsCmd = new SqliteCommand("DELETE FROM Transactions WHERE EventId = @eventId", connection, transaction))
                    {
                        deleteTransactionsCmd.Parameters.AddWithValue("@eventId", eventId);
                        deleteTransactionsCmd.ExecuteNonQuery();
                    }

                    // ✅ Now delete the event itself (WITHIN TRANSACTION)
                    using (var deleteEventCmd = new SqliteCommand("DELETE FROM Events WHERE Id = @eventId", connection, transaction))
                    {
                        deleteEventCmd.Parameters.AddWithValue("@eventId", eventId);
                        deleteEventCmd.ExecuteNonQuery();
                    }

                    transaction.Commit(); // ✅ Commit transaction after both deletions succeed

                    Console.WriteLine($"\n✅ Event '{eventName}' and all its transactions have been deleted.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); // ❌ Rollback if there's an error
                    Console.WriteLine($"❌ Error deleting event: {ex.Message}");
                }
            }
        }

        Console.WriteLine("\n==============================================================");
        Console.WriteLine("🔙 Press Enter to return to the main menu...");
        Console.ReadLine();
    }

    static void AdminDashboard()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("🛠️ ====================== Admin Dashboard ====================== 🛠️\n");

            Console.WriteLine("1️  Bulk Delete Events");
            Console.WriteLine("2️  Edit Event Name");
            Console.WriteLine("3️  Force Close All Check-Ins");
            Console.WriteLine("0  🔙 Return to Main Menu");

            Console.Write("\nSelect an option: ");
            string input = Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1":
                    BulkDeleteEvents();
                    break;
                case "2":
                    EditEvent();
                    break;
                case "3":
                    ForceCloseCheckIns();
                    break;
                case "0":
                    Console.WriteLine("\n🔙 Returning to the main menu...");
                    Console.ReadLine();
                    return;
                default:
                    Console.WriteLine("\n❌ Invalid option. Please try again.");
                    Console.WriteLine("\n🔙 Press Enter to return to the Admin Dashboard...");
                    Console.ReadLine();
                    break;
            }
        }
    }

    static void BulkDeleteEvents()
    {
        var events = DatabaseHelper.GetEvents();
        if (events.Count == 0)
        {
            Console.WriteLine("⚠ No events available to delete.");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("\n📌 Existing Events:");
        foreach (var ev in events)
        {
            Console.WriteLine($"{ev.Id}. {ev.Name}");
        }

        Console.Write("\nEnter Event IDs to delete (comma-separated): ");
        string input = Console.ReadLine();
        var eventIds = input.Split(',');

        foreach (var id in eventIds)
        {
            if (int.TryParse(id.Trim(), out int eventId))
            {
                DatabaseHelper.DeleteEvent(eventId);
                Console.WriteLine($"🗑 Deleted event {eventId}.");
            }
        }

        Console.WriteLine("\n✅ Bulk deletion complete.");
        Console.ReadKey();
    }

    static void EditEvent()
    {
        int eventId = SelectEvent();
        if (eventId == -1) return; // Exit if no events exist

        Console.Write("\nEnter new event name: ");
        string newName = Console.ReadLine().Trim();

        if (string.IsNullOrEmpty(newName))
        {
            Console.WriteLine("❌ Event name cannot be empty.");
            Console.ReadKey();
            return;
        }

        using (var connection = DatabaseHelper.GetConnection())
        {
            string query = "UPDATE Events SET Name = @newName WHERE Id = @eventId";
            DatabaseHelper.ExecuteNonQuery(connection, query, new Dictionary<string, object>
        {
            { "@newName", newName },
            { "@eventId", eventId }
        });
        }

        Console.WriteLine($"✅ Event ID {eventId} renamed to '{newName}'.");
        Console.ReadKey();
    }

    static void ForceCloseCheckIns()
    {
        Console.WriteLine("\n⚠ This will force-check out all attendees for all events!");
        Console.Write("Are you sure? (y/n): ");
        string confirmation = Console.ReadLine().Trim().ToLower();

        if (confirmation != "y")
        {
            Console.WriteLine("❌ Operation cancelled.");
            Console.ReadKey();
            return;
        }

        using (var connection = DatabaseHelper.GetConnection())
        {
            string query = "UPDATE Transactions SET CheckOutTime = CURRENT_TIMESTAMP WHERE CheckOutTime IS NULL";
            DatabaseHelper.ExecuteNonQuery(connection, query);
        }

        Console.WriteLine("✅ All open check-ins have been marked as checked out.");
        Console.ReadKey();
    }

    static void ShowLiveAttendeeList()
    {
        Console.Clear();
        Console.WriteLine("📋 ===================== Live Attendee List ===================== 📋\n");

        int eventId = SelectEvent();

        if (eventId == -1)
        {
            Console.WriteLine("\n🔙 Returning to the main menu...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine($"\n📌 Showing attendees currently checked in for Event {eventId}:\n");

        using (var connection = DatabaseHelper.GetConnection())
        using (var cmd = new SqliteCommand("SELECT CardNumber, CheckInTime FROM Transactions WHERE EventId = @eventId AND CheckOutTime IS NULL", connection))
        {
            cmd.Parameters.AddWithValue("@eventId", eventId);

            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.HasRows)
                {
                    Console.WriteLine("⚠ No attendees currently checked in.");
                }
                else
                {
                    Console.WriteLine("Card Number    | Check-In Time");
                    Console.WriteLine("------------------------------------");

                    while (reader.Read())
                    {
                        string cardNumber = reader["CardNumber"].ToString();
                        string checkInTime = reader["CheckInTime"].ToString();
                        Console.WriteLine($"{cardNumber,-14} | {checkInTime}");
                    }
                }
            }
        }

        Console.WriteLine("\n==============================================================");
        Console.WriteLine("🔙 Press Enter to return to the main menu...");
        Console.ReadLine();
    }

    static Dictionary<string, DateTime> lastScanTime = new Dictionary<string, DateTime>();

    static void HandleCardSwipes()
    {
        Console.Clear();
        Console.WriteLine("🔄 ==================== Check-In / Check-Out ==================== 🔄\n");

        int eventId = SelectEvent();

        if (eventId == -1)
        {
            Console.WriteLine("\n🔙 Returning to the main menu...");
            Console.ReadLine();
            return;
        }

        Console.WriteLine($"\n📌 Now scanning for Event {eventId}. Swipe or Tap a credential to check in/out.\n");

        // ✅ Reserve space for the static "Stop" message at the bottom
        int stopMessageLine = Console.CursorTop;
        Console.WriteLine("\n🛑 Type 'stop' and press Enter anytime to exit."); // ✅ Keep this message fixed
        Console.SetCursorPosition(0, stopMessageLine - 1); // ✅ Move cursor up so transactions appear above it

        while (true)
        {
            string? cardNumber = Console.ReadLine()?.Trim();

            // ✅ Clear previous input from console (prevents card number echoing)
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop);

            if (cardNumber?.ToLower() == "stop")
            {
                Console.SetCursorPosition(0, stopMessageLine); // ✅ Move cursor to static message line
                Console.WriteLine("\n🛑 **Session Ended!** Returning to main menu...");
                break;
            }

            if (string.IsNullOrEmpty(cardNumber))
            {
                continue; // ✅ Ignore empty inputs without printing anything
            }

            DateTime localTime = DateTime.Now;

            // ✅ Prevent duplicate scans within 30 seconds
            if (lastScanTime.ContainsKey(cardNumber))
            {
                TimeSpan timeSinceLastScan = localTime - lastScanTime[cardNumber];
                if (timeSinceLastScan.TotalSeconds < 30)
                {
                    Console.SetCursorPosition(0, stopMessageLine - 1);
                    Console.WriteLine($"\n⏳ Card was just scanned. Please wait {30 - timeSinceLastScan.Seconds} more seconds.  ");
                    Console.SetCursorPosition(0, stopMessageLine - 1); // ✅ Keep writing above the stop message
                    continue;
                }
            }

            // ✅ Update last scan time
            lastScanTime[cardNumber] = localTime;

            using (var connection = DatabaseHelper.GetConnection())
            {
                using (var checkCmd = new SqliteCommand("SELECT * FROM Transactions WHERE EventId = @eventId AND CardNumber = @cardNumber AND CheckOutTime IS NULL", connection))
                {
                    checkCmd.Parameters.AddWithValue("@eventId", eventId);
                    checkCmd.Parameters.AddWithValue("@cardNumber", cardNumber);

                    using (var reader = checkCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int transactionId = Convert.ToInt32(reader["Id"]);
                            using (var updateCmd = new SqliteCommand("UPDATE Transactions SET CheckOutTime = @localTime WHERE Id = @transactionId", connection))
                            {
                                updateCmd.Parameters.AddWithValue("@transactionId", transactionId);
                                updateCmd.Parameters.AddWithValue("@localTime", localTime);
                                updateCmd.ExecuteNonQuery();
                            }
                            Console.SetCursorPosition(0, stopMessageLine - 1);
                            Console.WriteLine($"\n🔄 Check-Out recorded at {localTime}.  ");
                            Console.SetCursorPosition(0, stopMessageLine - 1); // ✅ Keep writing above the stop message
                        }
                        else
                        {
                            using (var insertCmd = new SqliteCommand("INSERT INTO Transactions (EventId, CardNumber, CheckInTime) VALUES (@eventId, @cardNumber, @localTime)", connection))
                            {
                                insertCmd.Parameters.AddWithValue("@eventId", eventId);
                                insertCmd.Parameters.AddWithValue("@cardNumber", cardNumber);
                                insertCmd.Parameters.AddWithValue("@localTime", localTime);
                                insertCmd.ExecuteNonQuery();
                            }
                            Console.SetCursorPosition(0, stopMessageLine - 1);
                            Console.WriteLine($"\n✅ Check-In recorded at {localTime}.  ");
                            Console.SetCursorPosition(0, stopMessageLine - 1); // ✅ Keep writing above the stop message
                        }
                    }
                }
            }
        }

        Console.WriteLine("\n==============================================================");
        Console.WriteLine("🔙 Press Enter to return to the main menu...");
        Console.ReadLine();
    }

    static void ExportEventData()
    {
        Console.Clear();
        Console.WriteLine("📤 ==================== Export Event Data to CSV ==================== 📤\n");

        int eventId = SelectEvent();

        if (eventId == -1)
        {
            Console.WriteLine("\n🔙 Returning to the main menu...");
            Console.ReadLine();
            return;
        }

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string eventName = "";

        try
        {
            // ✅ Get the Event Name
            using (var connection = DatabaseHelper.GetConnection())
            using (var eventCmd = new SqliteCommand("SELECT Name FROM Events WHERE Id = @eventId", connection))
            {
                eventCmd.Parameters.AddWithValue("@eventId", eventId);
                using (var reader = eventCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        eventName = reader["Name"].ToString();
                    }
                    else
                    {
                        Console.WriteLine("⚠ No event found with this ID.");
                        Console.ReadLine();
                        return;
                    }
                }
            }

            // ✅ Sanitize Event Name for File Name (removes special characters)
            string safeEventName = string.Concat(eventName.Split(Path.GetInvalidFileNameChars()))
                                          .Replace(" ", "_");

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filePath = Path.Combine(desktopPath, $"{safeEventName}_Transactions_{timestamp}.csv");

            using (var writer = new StreamWriter(filePath))
            {
                // ✅ Write CSV Header
                writer.WriteLine("EventName,CardNumber,CheckInTime,CheckOutTime");

                using (var connection = DatabaseHelper.GetConnection())
                using (var cmd = new SqliteCommand("SELECT CardNumber, CheckInTime, CheckOutTime FROM Transactions WHERE EventId = @eventId", connection))
                {
                    cmd.Parameters.AddWithValue("@eventId", eventId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            Console.WriteLine("⚠ No transactions found for this event.");
                            Console.ReadLine();
                            return;
                        }

                        while (reader.Read())
                        {
                            string cardNumber = reader["CardNumber"].ToString();
                            string checkInTime = reader["CheckInTime"].ToString();
                            string checkOutTime = reader["CheckOutTime"]?.ToString() ?? "";

                            writer.WriteLine($"{eventName},{cardNumber},{checkInTime},{checkOutTime}");
                        }
                    }
                }
            }

            Console.WriteLine($"\n✅ Data exported successfully to: {filePath}");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("❌ Error: Permission denied. Try running as Administrator.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"❌ File Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected Error: {ex.Message}");
        }

        Console.WriteLine("\n==============================================================");
        Console.WriteLine("🔙 Press Enter to return to the main menu...");
        Console.ReadLine();
    }
}