using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using FirebaseAdmin.Messaging;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace YurtMenu.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly FirestoreDb _firestore;
        private readonly ILogger<NotificationController> _logger;
        private static readonly string[] TurkishMonths =
        {
            "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
            "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
        };

        public NotificationController(FirestoreDb firestore, ILogger<NotificationController> logger)
        {
            _firestore = firestore;
            _logger = logger;
        }

        // POST: api/notifications/send-daily-meals/{mealType}
        [HttpPost("send-daily-meals/{mealType}")]
        public async Task<IActionResult> SendDailyMealNotifications(string mealType)
        {
            try
            {
                var validMealTypes = new[] { "breakfast", "dinner" };
                if (!validMealTypes.Contains(mealType.ToLower()))
                {
                    return BadRequest("Meal type must be 'breakfast' or 'dinner'");
                }

                var result = await ProcessDailyMealNotifications(mealType.ToLower());
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending daily meal notifications");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // POST: api/notifications/send-to-city/{city}/{mealType}
        [HttpPost("send-to-city/{city}/{mealType}")]
        public async Task<IActionResult> SendToCityNotifications(string city, string mealType)
        {
            try
            {
                var result = await ProcessCityMealNotifications(city.ToLower(), mealType.ToLower());
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending city meal notifications");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // GET: api/notifications/check-today-menu/{city}/{mealType}
        [HttpGet("check-today-menu/{city}/{mealType}")]
        public async Task<IActionResult> CheckTodayMenu(string city, string mealType)
        {
            try
            {
                var todayMenu = await GetTodayMealData(city.ToLower(), mealType.ToLower());

                var today = DateTime.Now;
                var monthName = TurkishMonths[today.Month - 1];
                var todayString = today.ToString("yyyy-MM-dd");
                var searchPath = $"{city.ToLower()}/{monthName}/{mealType.ToLower()}/{todayString}";

                return Ok(new
                {
                    hasMenu = todayMenu != null,
                    menu = todayMenu?.Main,
                    city = city,
                    mealType = mealType,
                    date = DateTime.Now.ToString("yyyy-MM-dd"),
                    debugInfo = new
                    {
                        searchPath,
                        monthName,
                        dateFormat = todayString
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking today's menu");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // GET: api/notifications/debug-menu/{city}/{mealType}
        [HttpGet("debug-menu/{city}/{mealType}")]
        public async Task<IActionResult> DebugMenuData(string city, string mealType)
        {
            try
            {
                var today = DateTime.Now;
                var monthName = TurkishMonths[today.Month - 1];
                var todayString = today.ToString("yyyy-MM-dd");
                var todayStringAlt = today.ToString("yyyy-M-d");

                var debugInfo = new
                {
                    searchPath = $"{city.ToLower()}/{monthName}/{mealType.ToLower()}/{todayString}",
                    alternativePath = $"{city.ToLower()}/{monthName}/{mealType.ToLower()}/{todayStringAlt}",
                    currentDate = today.ToString("yyyy-MM-dd HH:mm:ss"),
                    monthIndex = today.Month - 1,
                    monthName = monthName,
                    dateFormat1 = todayString,
                    dateFormat2 = todayStringAlt
                };

                var docRef1 = _firestore.Collection(city.ToLower())
                                       .Document(monthName)
                                       .Collection(mealType.ToLower())
                                       .Document(todayString);
                var snapshot1 = await docRef1.GetSnapshotAsync();

                var docRef2 = _firestore.Collection(city.ToLower())
                                       .Document(monthName)
                                       .Collection(mealType.ToLower())
                                       .Document(todayStringAlt);
                var snapshot2 = await docRef2.GetSnapshotAsync();

                var monthDoc = await _firestore.Collection(city.ToLower())
                                              .Document(monthName)
                                              .GetSnapshotAsync();

                var mealTypeCollection = await _firestore.Collection(city.ToLower())
                                                        .Document(monthName)
                                                        .Collection(mealType.ToLower())
                                                        .GetSnapshotAsync();

                return Ok(new
                {
                    debugInfo,
                    format1Result = new
                    {
                        path = $"{city.ToLower()}/{monthName}/{mealType.ToLower()}/{todayString}",
                        exists = snapshot1.Exists,
                        data = snapshot1.Exists ? snapshot1.ToDictionary() : null
                    },
                    format2Result = new
                    {
                        path = $"{city.ToLower()}/{monthName}/{mealType.ToLower()}/{todayStringAlt}",
                        exists = snapshot2.Exists,
                        data = snapshot2.Exists ? snapshot2.ToDictionary() : null
                    },
                    monthDocExists = monthDoc.Exists,
                    mealTypeDocCount = mealTypeCollection.Count,
                    mealTypeDocIds = mealTypeCollection.Documents.Select(d => d.Id).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error debugging menu data");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        private async Task<NotificationResult> ProcessDailyMealNotifications(string mealType)
        {
            var result = new NotificationResult();
            const int chunkSize = 500;

            try
            {
                var cityGroups = await GetUsersByCityAsync();
                result.TotalUsers = cityGroups.Values.Sum(users => users.Count);

                _logger.LogInformation($"Processing {result.TotalUsers} users for {mealType} notifications across {cityGroups.Count} cities");

                foreach (var cityGroup in cityGroups)
                {
                    var city = cityGroup.Key;
                    var userTokens = cityGroup.Value;

                    _logger.LogInformation($"Processing {userTokens.Count} users for city: {city}");

                    var todayMeal = await GetTodayMealData(city, mealType);
                    if (todayMeal == null || string.IsNullOrEmpty(todayMeal.Main))
                    {
                        _logger.LogInformation($"No meal data for {city}, skipping {userTokens.Count} users");
                        result.SkippedUsers += userTokens.Count;
                        continue;
                    }

                    var chunks = userTokens.Select((token, index) => new { token, index })
                                          .GroupBy(x => x.index / chunkSize)
                                          .Select(g => g.Select(x => x.token).ToList())
                                          .ToList();

                    foreach (var (chunk, chunkIndex) in chunks.Select((chunk, index) => (chunk, index)))
                    {
                        _logger.LogInformation($"Sending batch {chunkIndex + 1}/{chunks.Count} for city {city}");

                        var tokens = chunk.Select(u => u.FcmToken).ToList();

                        try
                        {
                            var batchResult = await SendBatchNotifications(tokens, todayMeal.Main, mealType);

                            result.SuccessUsers += batchResult.SuccessCount;
                            result.FailedUsers += batchResult.FailureCount;

                            _logger.LogInformation($"Batch completed: {batchResult.SuccessCount}/{batchResult.TotalCount} sent successfully");
                        }
                        catch (Exception ex)
                        {
                            result.FailedUsers += tokens.Count;
                            _logger.LogError(ex, $"Error sending batch for city {city}");
                        }

                        if (chunkIndex < chunks.Count - 1)
                        {
                            await Task.Delay(100);
                        }
                    }
                }

                _logger.LogInformation($"Notification process completed. Success: {result.SuccessUsers}, Failed: {result.FailedUsers}, Skipped: {result.SkippedUsers}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessDailyMealNotifications");
                throw;
            }

            return result;
        }

        private async Task<NotificationResult> ProcessCityMealNotifications(string city, string mealType)
        {
            var result = new NotificationResult();

            try
            {
                var todayMeal = await GetTodayMealData(city, mealType);
                if (todayMeal == null || string.IsNullOrEmpty(todayMeal.Main))
                {
                    return new NotificationResult
                    {
                        Success = false,
                        Message = $"No meal data found for {city} - {mealType} today"
                    };
                }

                var usersQuery = _firestore.Collection("users")
                                          .WhereEqualTo("sehir", city)
                                          .WhereEqualTo("notificationsEnabled", true);

                var usersSnapshot = await usersQuery.GetSnapshotAsync();
                result.TotalUsers = usersSnapshot.Count;

                var validTokens = new List<string>();

                foreach (var userDoc in usersSnapshot.Documents)
                {
                    var userData = userDoc.ToDictionary();
                    if (userData.TryGetValue("fcmToken", out var fcmTokenObj) &&
                        !string.IsNullOrEmpty(fcmTokenObj?.ToString()))
                    {
                        validTokens.Add(fcmTokenObj.ToString());
                    }
                    else
                    {
                        result.SkippedUsers++;
                    }
                }

                if (validTokens.Any())
                {
                    var batchResult = await SendBatchNotifications(validTokens, todayMeal.Main, mealType);
                    result.SuccessUsers = batchResult.SuccessCount;
                    result.FailedUsers = batchResult.FailureCount;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessCityMealNotifications");
                throw;
            }

            return result;
        }

        private async Task<Dictionary<string, List<UserTokenInfo>>> GetUsersByCityAsync()
        {
            var cityGroups = new Dictionary<string, List<UserTokenInfo>>();
            var skippedCount = 0;

            var usersSnapshot = await _firestore.Collection("users").GetSnapshotAsync();
            _logger.LogInformation($"Processing {usersSnapshot.Count} total users from Firestore");

            foreach (var userDoc in usersSnapshot.Documents)
            {
                try
                {
                    var userData = userDoc.ToDictionary();

                    if (!userData.TryGetValue("fcmToken", out var fcmTokenObj) ||
                        fcmTokenObj == null ||
                        string.IsNullOrWhiteSpace(fcmTokenObj.ToString()))
                    {
                        skippedCount++;
                        _logger.LogDebug($"Skipped user {userDoc.Id}: null/empty fcmToken");
                        continue;
                    }

                    string? city = null;

                    if (userData.TryGetValue("sehir", out var sehirObj) &&
                        sehirObj != null &&
                        !string.IsNullOrWhiteSpace(sehirObj.ToString()))
                    {
                        city = sehirObj.ToString()!.ToLower();
                    }
                    else if (userData.TryGetValue("city", out var cityObj) &&
                             cityObj != null &&
                             !string.IsNullOrWhiteSpace(cityObj.ToString()))
                    {
                        city = cityObj.ToString()!.ToLower();
                    }

                    if (city == null)
                    {
                        skippedCount++;
                        _logger.LogDebug($"Skipped user {userDoc.Id}: no sehir or city field");
                        continue;
                    }

                    var fcmToken = fcmTokenObj.ToString()!;

                    if (fcmToken.Length < 50)
                    {
                        skippedCount++;
                        _logger.LogDebug($"Skipped user {userDoc.Id}: invalid token format (too short)");
                        continue;
                    }

                    var notificationsEnabled = true;
                    if (userData.TryGetValue("notificationsEnabled", out var notifObj) && notifObj != null)
                    {
                        bool.TryParse(notifObj.ToString(), out notificationsEnabled);
                    }

                    if (!notificationsEnabled)
                    {
                        skippedCount++;
                        _logger.LogDebug($"Skipped user {userDoc.Id}: notifications disabled");
                        continue;
                    }

                    if (!cityGroups.ContainsKey(city))
                    {
                        cityGroups[city] = new List<UserTokenInfo>();
                    }

                    cityGroups[city].Add(new UserTokenInfo
                    {
                        FcmToken = fcmToken,
                        UserId = userDoc.Id
                    });
                }
                catch (Exception ex)
                {
                    skippedCount++;
                    _logger.LogError(ex, $"Error processing user {userDoc.Id}");
                }
            }

            var validUserCount = cityGroups.Values.Sum(users => users.Count);
            _logger.LogInformation($"User filtering completed: {validUserCount} valid users, {skippedCount} skipped");

            foreach (var cityGroup in cityGroups)
            {
                _logger.LogInformation($"City {cityGroup.Key}: {cityGroup.Value.Count} users");
            }

            return cityGroups;
        }

        private async Task<MealData?> GetTodayMealData(string city, string mealType)
        {
            try
            {
                var today = DateTime.Now;
                var monthName = TurkishMonths[today.Month - 1];
                
                // Firestore'da kayıtlı olan olası tüm formatları kontrol et
                var format1 = today.ToString("yyyy-MM-d");   // 2026-05-1 (Sizin formatınız)
                var format2 = today.ToString("yyyy-MM-dd");  // 2026-05-01
                var format3 = today.ToString("yyyy-M-d");    // 2026-5-1

                string[] paths = { format1, format2, format3 };

                foreach (var dateString in paths)
                {
                    var docRef = _firestore.Collection(city)
                                         .Document(monthName)
                                         .Collection(mealType)
                                         .Document(dateString);

                    var snapshot = await docRef.GetSnapshotAsync();

                    if (snapshot.Exists)
                    {
                        var data = snapshot.ToDictionary();
                        if (data.TryGetValue("main", out var mainValue))
                        {
                            _logger.LogInformation($"Meal data found using format: {dateString} for {city}");
                            return new MealData { Main = mainValue?.ToString() ?? "" };
                        }
                    }
                }

                _logger.LogWarning($"No meal data found for {city} - {mealType} with any known date format.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting meal data for {city} - {mealType}");
                return null;
            }
        }

        private async Task<BatchNotificationResult> SendBatchNotifications(
    List<string> fcmTokens,
    string message,
    string mealType)
        {
            var result = new BatchNotificationResult();

            Log("INFO", "========== BATCH NOTIFICATION START ==========");
            Log("INFO", $"[INPUT] Received {fcmTokens.Count} tokens for meal type: {mealType}");

            if (!fcmTokens.Any())
            {
                Log("WARN", "[ERROR] No tokens provided");
                return result;
            }

            Log("INFO", "[CHECK] Checking Firebase Admin SDK initialization...");
            if (FirebaseApp.DefaultInstance == null)
            {
                Log("ERROR", "[CRITICAL] Firebase Admin SDK is NOT initialized!");
                result.FailureCount = fcmTokens.Count;
                result.TotalCount = fcmTokens.Count;
                return result;
            }
            Log("INFO", $"[CHECK] Firebase initialized - Project ID: {FirebaseApp.DefaultInstance.Options.ProjectId}");

            Log("INFO", "[FILTER] Filtering tokens...");
            var validTokens = fcmTokens
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Where(token => token.Length > 10)
                .ToList();

            Log("INFO", $"[FILTER] Result: {fcmTokens.Count} input tokens -> {validTokens.Count} valid tokens");

            if (!validTokens.Any())
            {
                Log("WARN", "[ERROR] No valid FCM tokens after filtering!");
                result.FailureCount = fcmTokens.Count;
                result.TotalCount = fcmTokens.Count;
                return result;
            }

            var title = mealType switch
            {
                "breakfast" => "Kahvaltı",
                "dinner" => "Akşam Yemeği",
                _ => "Yemek Menüsü"
            };

            // YENİ YÖNTEMİ KULLAN: SendEachForMulticastAsync
            const int batchSize = 500;
            var batches = validTokens
                .Select((token, index) => new { token, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.token).ToList())
                .ToList();

            Log("INFO", $"[BATCH] Splitting {validTokens.Count} tokens into {batches.Count} batches of {batchSize}");

            foreach (var (batch, batchIndex) in batches.Select((b, i) => (b, i)))
            {
                Log("INFO", $"[BATCH {batchIndex + 1}/{batches.Count}] Sending to {batch.Count} devices...");

                var multicastMessage = new MulticastMessage()
                {
                    Tokens = batch,
                    Notification = new Notification()
                    {
                        Title = title,
                        Body = message
                    },
                    Data = new Dictionary<string, string>()
            {
                {"mealType", mealType},
                {"date", DateTime.Now.ToString("yyyy-MM-dd")}
            }
                };

                try
                {
                    // ESKİ: SendMulticastAsync (DEPRECATED)
                    // YENİ: SendEachForMulticastAsync
                    var batchResponse = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(multicastMessage);

                    result.SuccessCount += batchResponse.SuccessCount;
                    result.FailureCount += batchResponse.FailureCount;
                    result.TotalCount += batch.Count;

                    Log("INFO", $"[BATCH {batchIndex + 1}] Success: {batchResponse.SuccessCount}, Failed: {batchResponse.FailureCount}");

                    // Hataları logla
                    if (batchResponse.Responses != null && batchResponse.FailureCount > 0)
                    {
                        var errorStats = new Dictionary<string, int>();

                        for (int i = 0; i < batchResponse.Responses.Count; i++)
                        {
                            var response = batchResponse.Responses[i];
                            if (!response.IsSuccess && response.Exception != null)
                            {
                                var errorType = response.Exception.MessagingErrorCode?.ToString() ?? "Unknown";

                                if (!errorStats.ContainsKey(errorType))
                                    errorStats[errorType] = 0;
                                errorStats[errorType]++;
                            }
                        }

                        Log("WARN", $"[BATCH {batchIndex + 1}] Error types:");
                        foreach (var stat in errorStats)
                        {
                            Log("WARN", $"  - {stat.Key}: {stat.Value} occurrences");
                        }
                    }

                    // Batch'ler arası bekleme
                    if (batchIndex < batches.Count - 1)
                    {
                        await Task.Delay(500);
                    }
                }
                catch (Exception ex)
                {
                    Log("ERROR", $"[BATCH {batchIndex + 1}] Exception: {ex.Message}");
                    if (ex.InnerException != null)
                        Log("ERROR", $"[BATCH {batchIndex + 1}] Inner: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                    Log("ERROR", $"[BATCH {batchIndex + 1}] Stack: {ex.StackTrace}");
                    result.FailureCount += batch.Count;
                    result.TotalCount += batch.Count;
                }
            }

            Log("INFO", $"[RESULT] Total - Success: {result.SuccessCount}, Failed: {result.FailureCount}, Total: {result.TotalCount}");
            Log("INFO", "========== BATCH NOTIFICATION END ==========\n");

            return result;
        }

        private void Log(string level, string message)
        {
            var fullMessage = $"[{level}] {message}";

            // Console log
            switch (level)
            {
                case "INFO":
                    _logger.LogInformation(fullMessage);
                    break;
                case "WARN":
                    _logger.LogWarning(fullMessage);
                    break;
                case "ERROR":
                    _logger.LogError(fullMessage);
                    break;
            }

            // File log
            try
            {
                var logDir = @"C:\Logs";
                if (!System.IO.Directory.Exists(logDir))
                {
                    System.IO.Directory.CreateDirectory(logDir);
                }

                var logFile = System.IO.Path.Combine(logDir, $"yurtmenu-{DateTime.Now:yyyy-MM-dd}.txt");
                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {fullMessage}{Environment.NewLine}";

                System.IO.File.AppendAllText(logFile, logMessage);
            }
            catch
            {
                // Loglama başarısız olursa sessizce devam et
            }
        }

        private async Task SendSingleNotification(string fcmToken, string message, string mealType)
        {
            var title = mealType switch
            {
                "breakfast" => "Kahvaltı",
                "dinner" => "Akşam Yemeği",
                _ => "Yemek Menüsü"
            };

            var fcmMessage = new Message()
            {
                Token = fcmToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = message
                },
                Data = new Dictionary<string, string>()
                {
                    {"mealType", mealType},
                    {"date", DateTime.Now.ToString("yyyy-MM-dd")}
                }
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendAsync(fcmMessage);
                _logger.LogDebug($"Successfully sent message: {response}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification to token: {fcmToken}");
                throw;
            }
        }

        [HttpGet("firebase-status")]
        public IActionResult GetFirebaseStatus()
        {
            try
            {
                var firebaseApp = FirebaseApp.DefaultInstance;
                return Ok(new
                {
                    firebaseInitialized = firebaseApp != null,
                    projectId = firebaseApp?.Options.ProjectId,
                    hasCredential = firebaseApp?.Options.Credential != null,
                    firebaseMessagingAvailable = FirebaseMessaging.DefaultInstance != null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    firebaseInitialized = false,
                    error = ex.Message
                });
            }
        }

        [HttpPost("schedule-daily-notifications")]
        public async Task<IActionResult> ScheduleDailyNotifications()
        {
            try
            {
                var currentHour = DateTime.Now.Hour;

                if (currentHour == 7)
                {
                    var breakfastResult = await ProcessDailyMealNotifications("breakfast");
                    _logger.LogInformation($"Breakfast notifications sent: {breakfastResult}");
                }

                if (currentHour == 17)
                {
                    var dinnerResult = await ProcessDailyMealNotifications("dinner");
                    _logger.LogInformation($"Dinner notifications sent: {dinnerResult}");
                }

                return Ok(new { message = "Scheduled notifications processed", hour = currentHour });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduled notifications");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


        [HttpPost("test-single-notification")]
        public async Task<IActionResult> TestSingleNotification()
        {
            var message = new Message()
            {
                Token = "ebS3VbcIQR2Jr5JG5uLIwq:APA91bFjoqhnQaBSoizSn0t92Yy7RjgWQmbEfhnnIT78dVC4FJrLbE0S-U4KVlsO13D-y-zRuriPRU9mMRMYl-nulZAMTgFLXNtSmHBnYCuNG6MhZnCChzk"
,
                Notification = new Notification()
                {
                    Title = "Test",
                    Body = "Test mesajı"
                }
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                return Ok(new { success = true, messageId = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message,
                    inner = ex.InnerException?.Message,
                    type = ex.GetType().Name
                });
            }
        }

        // POST: api/notifications/send-custom-text
        [HttpPost("send-custom-text")]
        public async Task<IActionResult> SendCustomTextNotification([FromBody] CustomTextRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return BadRequest("Title is required");
                }

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest("Message is required");
                }

                var result = await ProcessCustomTextNotifications(request.Title, request.Message);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending custom text notifications");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        private async Task<NotificationResult> ProcessCustomTextNotifications(string title, string message)
        {
            var result = new NotificationResult();
            const int chunkSize = 500;

            try
            {
                var cityGroups = await GetUsersByCityAsync();
                result.TotalUsers = cityGroups.Values.Sum(users => users.Count);

                _logger.LogInformation($"Processing {result.TotalUsers} users for custom text notification across {cityGroups.Count} cities");

                foreach (var cityGroup in cityGroups)
                {
                    var city = cityGroup.Key;
                    var userTokens = cityGroup.Value;

                    _logger.LogInformation($"Processing {userTokens.Count} users for city: {city}");

                    // Bugünün menüsünü kontrol et (sabah veya akşam, hangisi varsa)
                    var todayBreakfast = await GetTodayMealData(city, "breakfast");
                    var todayDinner = await GetTodayMealData(city, "dinner");

                    // Hiç menü yoksa bu şehri atla
                    if ((todayBreakfast == null || string.IsNullOrEmpty(todayBreakfast.Main)) &&
                        (todayDinner == null || string.IsNullOrEmpty(todayDinner.Main)))
                    {
                        _logger.LogInformation($"No meal data for {city}, skipping {userTokens.Count} users");
                        result.SkippedUsers += userTokens.Count;
                        continue;
                    }

                    var chunks = userTokens.Select((token, index) => new { token, index })
                                          .GroupBy(x => x.index / chunkSize)
                                          .Select(g => g.Select(x => x.token).ToList())
                                          .ToList();

                    foreach (var (chunk, chunkIndex) in chunks.Select((chunk, index) => (chunk, index)))
                    {
                        _logger.LogInformation($"Sending batch {chunkIndex + 1}/{chunks.Count} for city {city}");

                        var tokens = chunk.Select(u => u.FcmToken).ToList();

                        try
                        {
                            var batchResult = await SendCustomBatchNotifications(tokens, title, message);

                            result.SuccessUsers += batchResult.SuccessCount;
                            result.FailedUsers += batchResult.FailureCount;

                            _logger.LogInformation($"Batch completed: {batchResult.SuccessCount}/{batchResult.TotalCount} sent successfully");
                        }
                        catch (Exception ex)
                        {
                            result.FailedUsers += tokens.Count;
                            _logger.LogError(ex, $"Error sending batch for city {city}");
                        }

                        if (chunkIndex < chunks.Count - 1)
                        {
                            await Task.Delay(100);
                        }
                    }
                }

                _logger.LogInformation($"Custom text notification process completed. Success: {result.SuccessUsers}, Failed: {result.FailedUsers}, Skipped: {result.SkippedUsers}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessCustomTextNotifications");
                throw;
            }

            return result;
        }

        private async Task<BatchNotificationResult> SendCustomBatchNotifications(
            List<string> fcmTokens,
            string title,
            string message)
        {
            var result = new BatchNotificationResult();

            Log("INFO", "========== CUSTOM TEXT NOTIFICATION START ==========");
            Log("INFO", $"[INPUT] Received {fcmTokens.Count} tokens");
            Log("INFO", $"[CONTENT] Title: {title}, Message: {message}");

            if (!fcmTokens.Any())
            {
                Log("WARN", "[ERROR] No tokens provided");
                return result;
            }

            Log("INFO", "[CHECK] Checking Firebase Admin SDK initialization...");
            if (FirebaseApp.DefaultInstance == null)
            {
                Log("ERROR", "[CRITICAL] Firebase Admin SDK is NOT initialized!");
                result.FailureCount = fcmTokens.Count;
                result.TotalCount = fcmTokens.Count;
                return result;
            }
            Log("INFO", $"[CHECK] Firebase initialized - Project ID: {FirebaseApp.DefaultInstance.Options.ProjectId}");

            Log("INFO", "[FILTER] Filtering tokens...");
            var validTokens = fcmTokens
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Where(token => token.Length > 10)
                .ToList();

            Log("INFO", $"[FILTER] Result: {fcmTokens.Count} input tokens -> {validTokens.Count} valid tokens");

            if (!validTokens.Any())
            {
                Log("WARN", "[ERROR] No valid FCM tokens after filtering!");
                result.FailureCount = fcmTokens.Count;
                result.TotalCount = fcmTokens.Count;
                return result;
            }

            const int batchSize = 500;
            var batches = validTokens
                .Select((token, index) => new { token, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.token).ToList())
                .ToList();

            Log("INFO", $"[BATCH] Splitting {validTokens.Count} tokens into {batches.Count} batches of {batchSize}");

            foreach (var (batch, batchIndex) in batches.Select((b, i) => (b, i)))
            {
                Log("INFO", $"[BATCH {batchIndex + 1}/{batches.Count}] Sending to {batch.Count} devices...");

                var multicastMessage = new MulticastMessage()
                {
                    Tokens = batch,
                    Notification = new Notification()
                    {
                        Title = title,
                        Body = message
                    },
                    Data = new Dictionary<string, string>()
                    {
                        {"type", "custom"},
                        {"date", DateTime.Now.ToString("yyyy-MM-dd")}
                    }
                };

                try
                {
                    var batchResponse = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(multicastMessage);

                    result.SuccessCount += batchResponse.SuccessCount;
                    result.FailureCount += batchResponse.FailureCount;
                    result.TotalCount += batch.Count;

                    Log("INFO", $"[BATCH {batchIndex + 1}] Success: {batchResponse.SuccessCount}, Failed: {batchResponse.FailureCount}");

                    if (batchResponse.Responses != null && batchResponse.FailureCount > 0)
                    {
                        var errorStats = new Dictionary<string, int>();

                        for (int i = 0; i < batchResponse.Responses.Count; i++)
                        {
                            var response = batchResponse.Responses[i];
                            if (!response.IsSuccess && response.Exception != null)
                            {
                                var errorType = response.Exception.MessagingErrorCode?.ToString() ?? "Unknown";

                                if (!errorStats.ContainsKey(errorType))
                                    errorStats[errorType] = 0;
                                errorStats[errorType]++;
                            }
                        }

                        Log("WARN", $"[BATCH {batchIndex + 1}] Error types:");
                        foreach (var stat in errorStats)
                        {
                            Log("WARN", $"  - {stat.Key}: {stat.Value} occurrences");
                        }
                    }

                    if (batchIndex < batches.Count - 1)
                    {
                        await Task.Delay(500);
                    }
                }
                catch (Exception ex)
                {
                    Log("ERROR", $"[BATCH {batchIndex + 1}] Exception: {ex.Message}");
                    if (ex.InnerException != null)
                        Log("ERROR", $"[BATCH {batchIndex + 1}] Inner: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                    result.FailureCount += batch.Count;
                    result.TotalCount += batch.Count;
                }
            }

            Log("INFO", $"[RESULT] Total - Success: {result.SuccessCount}, Failed: {result.FailureCount}, Total: {result.TotalCount}");
            Log("INFO", "========== CUSTOM TEXT NOTIFICATION END ==========\n");

            return result;
        }
    }

    public class NotificationResult
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = "";
        public int TotalUsers { get; set; }
        public int SuccessUsers { get; set; }
        public int FailedUsers { get; set; }
        public int SkippedUsers { get; set; }

        public override string ToString()
        {
            return $"Total: {TotalUsers}, Success: {SuccessUsers}, Failed: {FailedUsers}, Skipped: {SkippedUsers}";
        }
    }

    public class MealData
    {
        public string Main { get; set; } = "";
    }

    public class UserTokenInfo
    {
        public string FcmToken { get; set; } = "";
        public string UserId { get; set; } = "";
    }

    public class BatchNotificationResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int TotalCount { get; set; }
    }

    public class CustomTextRequest
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
    }



}