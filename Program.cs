using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlTypes;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

class Program
{
  static void Main()
  {
    int port = 5000;

    var server = new Server(port);

    Console.WriteLine("The server is running");
    Console.WriteLine($"Main Page: http://localhost:{port}/website/pages/main.html");

    var database = new Database();
    database.Database.EnsureCreated();

    FillQuestions(database);

    while (true)
    {
      (var request, var response) = server.WaitForRequest();

      Console.WriteLine($"Recieved a request with the path: {request.Path}");

      if (File.Exists(request.Path))
      {
        var file = new File(request.Path);
        response.Send(file);
      }
      else if (request.ExpectsHtml())
      {
        var file = new File("website/pages/404.html");
        response.SetStatusCode(404);
        response.Send(file);
      }
      else
      {
        try
        {

          if (request.Path == "SignIn")
          {
            (string NickName, string Password) = request.GetBody<(string, string)>();
            bool is_exists = false;

            is_exists = database.Users.Any(user =>
              user.Username == NickName
            );

            if (is_exists == true)
            {
              response.Send("exists");

            }
            else if (Password.Length < 5)
            {
              response.Send("invalid");

            }

            else
            {
              var userId = Guid.NewGuid().ToString();
              database.Users.Add(new User(userId, NickName, Password));
              database.SaveChanges();
              response.Send(userId);




            }
          }
          else if (request.Path == "Login")
          {
            (string NickName, string Password) = request.GetBody<(string, string)>();


            var user = database.Users

              .FirstOrDefault(user => user.Username.ToLower() == NickName.ToLower() && user.Password == Password);



            if (user != null)
            {
              response.Send(user?.Id);


            }
            else
            {
              response.Send("Error");
              response.SetStatusCode(401);
            }




          }
          else if (request.Path == "StartGame")
          {

            string userId = request.GetBody<string>();


            var gameId = Guid.NewGuid().ToString();
            database.Games.Add(new Game(gameId, userId, 0, 0, 0, ""));
            database.SaveChanges();
            response.Send(gameId);




          }
          else if (request.Path == "GetQuestion")
          {
            string gameId = request.GetBody<string>();
            int currentQuestion;
            string choseGameId = "";

            var game = database.Games.Find(gameId)!;


            string gameUsedQuestions = game.UsedQuestions;
            List<int> usedQuestions = new List<int>();

            if (gameUsedQuestions != null && gameUsedQuestions != "")
            {
              usedQuestions = gameUsedQuestions
               .Split(',', StringSplitOptions.RemoveEmptyEntries)
               .Select(int.Parse)
               .ToList();

            }
            else
            {
              gameUsedQuestions = "";
            }


            List<Question> questions = new List<Question>();
            List<Question> availableQuestions = new List<Question>();


            questions = database.Questions.ToList();

            for (int i = 0; i < questions.Count; i++)
            {
              currentQuestion = questions[i].Id;

              if (!usedQuestions.Contains(currentQuestion))
              {
                availableQuestions.Add(questions[i]);
              }


            }

            int randomQuestion;

            Random random = new Random();
            randomQuestion = random.Next(0, availableQuestions.Count);
            choseGameId = availableQuestions[randomQuestion].Id.ToString();

            if (gameUsedQuestions != "")
            {
              gameUsedQuestions = gameUsedQuestions + ",";

            }
            gameUsedQuestions = gameUsedQuestions + choseGameId;
            game.UsedQuestions = gameUsedQuestions;
            database.SaveChanges();


            var questionToSend = new Question(
                availableQuestions[randomQuestion].Quest,
                availableQuestions[randomQuestion].Ans1,
                availableQuestions[randomQuestion].Ans2,
                availableQuestions[randomQuestion].Ans3,
                availableQuestions[randomQuestion].Ans4,
                -1
            );

            response.Send(questionToSend);


          }
          else if (request.Path == "CheckAnswer")
          {
            (string gameId, int userAns, int ansTime) = request.GetBody<(string, int, int)>();

            int ansScore = 0;
            int totalScore;
            bool ifCorrect = false;


            var game = database.Games.Find(gameId)!;
            totalScore = game.GameScore;

            string gameUsedQuestions = game.UsedQuestions;
            string[] arrUsedQuestions = gameUsedQuestions.Split(",");
            int lastQuestion = int.Parse(arrUsedQuestions[arrUsedQuestions.Length - 1]);

            var ans = database.Questions.Find(lastQuestion);
            int correctAns = ans!.AnsCorrect!;

            if (correctAns == userAns)
            {
              ifCorrect = true;

              if (ansTime < 3)
              {
                ansScore = 10;
              }
              else if (ansTime < 6 && ansTime > 3)
              {
                ansScore = 7;

              }
              else if (ansTime < 10 && ansTime > 6)
              {
                ansScore = 3;
              }
              else
              {
                ansScore = 1;
              }
            }

            totalScore = totalScore + ansScore;
            game.GameScore = totalScore;
            game.TotalAnswers++;

            if (ifCorrect)
            {
              game.TotalCorrectAnswers++;
            }

            database.SaveChanges();

            response.Send(correctAns);






          }
          else if (request.Path == "GetResult")
          {
            string gameId = request.GetBody<string>();

            var game = database.Games.Find(gameId);

            response.Send(game);





          }
          else if (request.Path == "GetTopScores")
          {
            int topCount = 5;


            var maxScoresByUser = database.Games
                .GroupBy(g => g.UserId)
                .Select(g => new
                {
                  UserId = g.Key,
                  MaxScore = g.Max(x => x.GameScore)
                });


            var topUsers = maxScoresByUser
                .OrderByDescending(g => g.MaxScore)
                .Take(topCount);

            var topUsernames = topUsers
                .Join(database.Users,
                      game => game.UserId,
                      user => user.Id,
                      (game, user) => new
                      {
                        Username = user.Username,
                        MaxScore = game.MaxScore
                      })
                .ToList();




            response.Send(topUsernames);
             
             


               


          }
        }
        catch (Exception exception)
        {
          Log.WriteException(exception);
        }
      }

      response.Close();
    }
  }

  private static void FillQuestions(Database database)
  {


    if (!database.Questions.Any())
    {
      database.Questions.Add(new Question("מהי בירת איטליה?", "מילאנו", "רומא", "נאפולי", "פירנצה", 2)); // ערים ומדינות
      database.Questions.Add(new Question("מהי העיר הכי דרומית בישראל?", "אילת", "אשקלון", "באר שבע ", "דימונה", 1));
      database.Questions.Add(new Question("איזו מבין הערים לא נמצאת באירופה?", "פריז", "ליסבון", "בנקוק", "אתונה", 3));
      database.Questions.Add(new Question("מהי בירת שומרון?", "ברקן", "אריאל", "עלי זהב", "כפר תפוח", 2));
      database.Questions.Add(new Question("איזו מבין הערים לא הייתה בברית המועצות?", "קייב", "קהיר", "קישינב", "טביליסי", 2));
      database.Questions.Add(new Question("מהי המדינה שעיר בירתה ביירות?", "תימן", "לבנון", "סוריה", "איראן", 2));
      database.Questions.Add(new Question("באיזו עיר התקיים אירוויזיון 2024?", "האג", "מאלמו", "סטוקהולם", "ברצלונה", 2)); 
      database.Questions.Add(new Question("מהי המדינה הגדולה ביותר בעולם?", "קנדה", "רוסיה", "סין", "ארה\"ב", 2));
      database.Questions.Add(new Question("באיזו יבשת נמצאת מצרים?", "אסיה", "אירופה", "אפריקה", "אמריקה", 3));
      database.Questions.Add(new Question("איזו מדינה ידועה כארץ השמש העולה?", "סין", "יפן", "קוריאה", "תאילנד", 2));
      database.Questions.Add(new Question("מהו הנהר הארוך בעולם?", "האמזונס", "הנילוס", "הדנובה", "הגנגס", 2));
      database.Questions.Add(new Question("כמה יבשות יש בעולם?", "5", "6", "7", "8", 3));
      database.Questions.Add(new Question("באיזו מדינה נמצא מגדל אייפל?", "גרמניה", "איטליה", "צרפת", "ספרד", 3));
      database.Questions.Add(new Question("איזו עיר מכונה 'התפוח הגדול'?", "לונדון", "ניו יורק", "טורונטו", "שיקגו", 2));
      database.Questions.Add(new Question("מהי בירת איטליה?", "מילאנו", "רומא", "ונציה", "נאפולי", 2));
      database.Questions.Add(new Question("היכן נמצאת העיר סידני?", "קנדה", "אוסטרליה", "אנגליה", "ארה\"ב", 2));
      database.Questions.Add(new Question("מהו ההר הגבוה בעולם?", "קילימנג'רו", "אוורסט", "הר אלברוס", "מונט בלאן", 2));

     database.Questions.Add(new Question("מי המציא את פייסבוק?", "סטיב ג'ובס", "אילון מאסק", "מארק צוקרברג", "ביל גייטס", 3)); // אנשים (אישיות)
     database.Questions.Add(new Question("מי שיחק את הדמות של 'הארי פוטר' בסדרת הסרטים?", "אלייז'ה ווד", "דניאל רדקליף", "רופרט גרינט", "טום הולנד", 2)); // אנשים (אישיות)
     database.Questions.Add(new Question("מי היה דובר צהל בשבעה באוקטובר?", "בנימין נתניהו", "בני גנץ ", "דניאל הגרי", "רוני גמזו", 3)); // אנשים (אישיות)
     database.Questions.Add(new Question("מי נחשב לראפר המצליח ביותר בכל הזמנים?", "אמינם", "קניה ווסט", "דרייק", "טופאק", 1)); 
     database.Questions.Add(new Question("מי היה ראש ממשלת ארצות הברית בשנת 2023", "ביידן", "אובמה", "טראמפ", "קלינטון", 1)); 
     database.Questions.Add(new Question("באיזה ערוץ משדרת יונית לוי?", "כאן 11", "ערוץ 13", "ערוץ 12", "ערוץ 14", 3));
     database.Questions.Add(new Question("מה שם הלהקה הבריטית המפורסמת שהורכבה מ-4 חברים: פול, ג'ון, ג'ורג' ורינגו?", "הביטלס", "הסטונס", "קווין", "הפלאטס", 1));
     database.Questions.Add(new Question("מי ייצג את ישראל באירוויזיון 2024?", "עדן גולן", "נועה קירל", "מאיה בוסקילה", "יונתן כהן", 1)); 
     database.Questions.Add(new Question("מי היה הראפר שהוציא את האלבום 'The Marshall Mathers LP'?", "אימינם", "אף תשובה אינה נכונה ", "טופאק שאקור", "קנדריק לאמאר", 1));
     database.Questions.Add(new Question("מי היה ממציא הנורה החשמלית?", "תומס אדיסון", "ניקולה טסלה", "אלכסנדר גרהם בל", "מייקל פאראדיי", 1));
     database.Questions.Add(new Question("מי היה השחקן הראשי בסרט 'איירון מן'?", "כריס אוונס", "רוברט דאוני ג'וניור", "כריס המסוורת'", "סקארלט ג'והנסון", 2));
     database.Questions.Add(new Question("מהו המקצוע של הדמות שירה בסדרה 'קופה ראשית'?", "קוסמטיקאית", "רואת חשבון", "מנהלת סופרמרקט", "מטפלת", 3));
     database.Questions.Add(new Question("איזה פיזיקאי נוסח את שלושת חוקי התנועה?", "ג'יימס קלרק מקסוול", "אייזק ניוטון", "אלברט איינשטין", "מארי קירי", 2));

     database.Questions.Add(new Question("איזה מותג מייצר את מכשירי הטלפון הנייד 'אייפון'?", "סמסונג", "נוקיה", "אפל", "LG", 3));
     database.Questions.Add(new Question("איזה מותג הוא המוביל בעולם בתחום המשקאות הקלים?", "פפסי", "קוקה קולה", "הובס", "7 אפ", 2));
     database.Questions.Add(new Question("איזה מותג מייצר את הדגם 'פוקוס'?", "פולקסווגן", "פיאט", "פיג'ו", "פורד", 4));
     database.Questions.Add(new Question("איזו חברה אינה קשורה להייטק?", "Intel", "IBM", "Fedex", "Microsoft", 3));
     database.Questions.Add(new Question("איזו חברת תוכנה מפתחת את מערכת ההפעלה Windows?", "Apple", "Google", "IBM", "Microsoft", 4));
     database.Questions.Add(new Question("איזה מותג מזוהה עם ספורט וציוד ספורטיבי?", "Lacoste", "Nike", "Gucci", "Ray-Ban", 2));
     database.Questions.Add(new Question("איזה מותג משווק בעיקר מוצרי חלב בישראל?", "תנובה", "אסם", "עלית", "יוטבתה", 1));
     database.Questions.Add(new Question("איזה מותג שייך לתחום הבשמים והקוסמטיקה?", "Chanel", "Samsung", "Canon", "Bosch", 1));
     database.Questions.Add(new Question("איזו חברה מייצרת את Xbox?", "Apple", "Sony", "Microsoft", "Google", 3));
     database.Questions.Add(new Question("מהו מותג האופנה המוזהה עם ראש תנין ירוק?", "Lacoste", "Nike", "Polo", "Fila", 1));
     database.Questions.Add(new Question("מהו מותג השוקולד המזוהה עם עטיפה סגולה?", "Milka", "Lindt", "Ferrero", "Nestlé", 1));
     database.Questions.Add(new Question("איזו חברה מייצרת את המשחקים Fortnite ו-Unreal Engine?", "Blizzard", "Epic Games", "Ubisoft", "Rockstar", 2));

    database.Questions.Add(new Question("כל התיכוניסטים לומדים מתמטיקה. יוסי תיכוניסט. מה נכון?", "יוסי לומד מתמטיקה", "יוסי לא לומד", "יוסי מלמד", "אין מספיק מידע", 1));
    database.Questions.Add(new Question("בתור עומדים: דנה לפני יוסי, יוסי אחרי לירן. מי ראשון?", "יוסי", "דנה", "לירן", "לא ניתן לדעת", 3));
    database.Questions.Add(new Question("אם 5 מחשבים צורכים 5 דקות לעיבוד, כמה זמן ל-1 מחשב?", "1 דקה", "5 דקות", "25 דקות", "לא ניתן לדעת", 2));
    database.Questions.Add(new Question("אם V > 0: ", "V < -1", "V > 4", "V = -3", "V < 5 - 12", 2));
    database.Questions.Add(new Question("המספר הבא בסדרה: 2, 4, 8, 16, ?", "18", "20", "32", "24", 3));
    database.Questions.Add(new Question("אם צ'פופו שותה כל יום קולה שמחירה 7 שקלים. כמה הוא משלם כסף על קולה בשבוע?", " $ 18", "45 שקלים", " $ 41", "49 שקלים", 4));
    database.Questions.Add(new Question("א", " $ 18", "45 שקלים", " $ 41", "49 שקלים", 4));
    database.Questions.Add(new Question("מה כבד יותר – קילוגרם ברזל או קילוגרם נוצות?", "ברזל", "נוצות", "שניהם שוקלים אותו דבר", "תלוי בלחות", 3));
    database.Questions.Add(new Question("אני גבוה כשאני צעיר ונמוך כשאני זקן. מה אני?", "אדם", "עץ", "נר", "כלב", 3));
    database.Questions.Add(new Question("מה עולה אך אף פעם לא יורד?", "טמפרטורה", "עץ", "גיל", "שעון", 3));
    database.Questions.Add(new Question("מה תמיד בא אך אף פעם לא מגיע?", "מחר", "אתמול", "רוח", "ענן", 1));
    database.Questions.Add(new Question("מהו השורש הריבועי של 81?", "8", "9", "7", "6", 2));
    database.Questions.Add(new Question("מהו סכם זוויות במשולש?", "180", "360", "270", "420", 2));
    database.Questions.Add(new Question("באיזה מרובע מבין הבאים, כל הצלעות שוות?", "דלתון", "מעויין", "מלבן", "טרפז", 2));


    database.Questions.Add(new Question("מהו יסוד כימי שמספרו האטומי הוא 1?", "חמצן", "מימן", "הליום", "פחמן", 2));
    database.Questions.Add(new Question("מהי צורת המולקולה של מים?", "H2O", "HO2", "OH", "H3O", 1));
    database.Questions.Add(new Question("איזו פלנטה קרובה ביותר לשמש?", "נוגה", "מרקורי", "מאדים", "כדור הארץ", 2));
    database.Questions.Add(new Question("איזה גז חיוני לנשימה של בני אדם?", "פחמן דו-חמצני", "חנקן", "חמצן", "הליום", 3));
    database.Questions.Add(new Question("מהו אברון התא שאחראי על הפקת אנרגיה?", "גרעין", "מיטוכונדריה", "ריבוזום", "ליזוזום", 2));
    database.Questions.Add(new Question("מהו מצב הצבירה של מים בטמפרטורת החדר?", "מוצק", "נוזל", "גז", "פלזמה", 2));
    database.Questions.Add(new Question("מהו כוכב הלכת הגדול ביותר במערכת השמש?", "שבתאי", "מאדים", "צדק", "נפטון", 3));
    database.Questions.Add(new Question("איזו יחידה מודדת זרם חשמלי?", "ואט", "וולט", "אמפר", "אוהם", 3));
    database.Questions.Add(new Question("מהו שם תהליך הפקת האנרגיה בצמחים?", "התאדות", "פוטוסינתזה", "נשימה תאית", "תסיסה", 2));
    database.Questions.Add(new Question("מהו השם של תהליך מעבר מנוזל לגז?", "עיבוי", "המראה", "קִפאון", "אידוי", 4));
    database.Questions.Add(new Question("איזה איבר שולט על מערכת העצבים?", "לב", "ריאות", "כבד", "מוח", 4));
    database.Questions.Add(new Question("איך נקרא השכבה החיצונית של כדור הארץ?", "מגמה", "קרום", "גרעין", "מעטפת", 2));
    database.Questions.Add(new Question("מהו כוח המשיכה על פני כדור הארץ בקירוב?", "1 מ'/ש²", "4.9 מ'/ש²", "9.8 מ'/ש²", "12 מ'/ש²", 3));
    database.Questions.Add(new Question("מה תפקיד תאי הדם הלבנים?", "נשיאת חמצן", "הגנה מפני מחלות", "קרישת דם", "הובלת סוכר", 2));
    database.Questions.Add(new Question("איזו מדידה משתמשת ביחידת ניוטון?", "מהירות", "מסה", "כוח", "לחץ", 3));
    database.Questions.Add(new Question("איזה סוג חומר אינו מוליך חשמל?", "מתכת", "פלסטיק", "מים", "אלומיניום", 2));
    database.Questions.Add(new Question("כמה קילוגרם יש בטון?", "100", "10", "1000", "500", 3));


    database.Questions.Add(new Question("כמה שחקנים יש בקבוצת כדורגל על המגרש?", "10", "11", "12", "9", 2));
    database.Questions.Add(new Question("מי נחשב לגדול שחקני הכדורגל בכל הזמנים?", "פלה", "מסי", "רונאלדו", "מראדונה", 2));
    database.Questions.Add(new Question("איזו מדינה אירחה את אולימפיאדת טוקיו 2020?", "סין", "יפן", "קוריאה", "גרמניה", 2));
    database.Questions.Add(new Question("כמה טבעות אולימפיות יש בלוגו?", "4", "5", "6", "7", 2));
    database.Questions.Add(new Question("מהו השיא העולמי בריצת 100 מטר?", "9.58 שניות", "10.01 שניות", "9.89 שניות", "9.63 שניות", 1));
    database.Questions.Add(new Question("איזה כדור משמש במשחק טניס?", "כדור אדום", "כדור צהוב", "כדור לבן", "כדור כחול", 2));
    database.Questions.Add(new Question("מהו שם המועדון של ליאו מסי ב-2023?", "ברצלונה", "פריז סן ז'רמן", "אינטר מיאמי", "יובנטוס", 3));

    database.Questions.Add(new Question("מאיזה מדינה מגיע הסושי?", "סין", "יפן", "קוריאה", "תאילנד", 2));
    database.Questions.Add(new Question("איזה מרכיב עיקרי יש בגואקמולי?", "עגבנייה", "מלפפון", "אבוקדו", "חציל", 3));
    database.Questions.Add(new Question("מהו המאכל הלאומי של איטליה?", "פיצה", "נקניקייה", "פלאפל", "בורגר", 1));
    database.Questions.Add(new Question("מהו תבלין הצהוב בקארי?", "כורכום", "קינמון", "זעתר", "כמון", 1));
    database.Questions.Add(new Question("איזה מרכיב עיקרי יש בחומוס?", "עדשים", "שעועית", "חומוס", "אורז", 3));
    database.Questions.Add(new Question("מהו משקה האנרגיה הנפוץ ביותר?", "רד בול", "קפה", " מונסטר", "מים מוגזים", 1));

    database.Questions.Add(new Question("מה הפירוש של המילה 'priority'?", "הזדמנות", "עדיפות", "בעיה", "פתרון", 2));
    database.Questions.Add(new Question("מה הפירוש של המילה 'opportunity'?", "המלצה", "הסבר", "הזדמנות", "עדות", 3));
    database.Questions.Add(new Question("מה הפירוש של המילה 'responsible'?", "אחראי", "חברתי", "חשוב", "מיוחד", 1));
    database.Questions.Add(new Question("מה הפירוש של המילה 'achievement'?", "השוואה", "כישלון", "הישג", "מטרה", 3));
    database.Questions.Add(new Question("מה הפירוש של המילה 'improve'?", "להעתיק", "לשפר", "להימנע", "להבטיח", 2));
    database.Questions.Add(new Question("מה הפירוש של המילה 'challenge'?", "מתנה", "מכשול", "אתגר", "רעיון", 3));
    database.Questions.Add(new Question("מה הפירוש של המילה 'solution'?", "תוצאה", "בעיה", "שאלה", "פתרון", 4));
    database.Questions.Add(new Question("מה הפירוש של המילה 'experience'?", "ניסיון", "חופשה", "בחירה", "תשובה", 1));
    database.Questions.Add(new Question("מה הפירוש של המילה 'require'?", "לדרוש", "לבחור", "להציע", "לבלבל", 1));
    database.Questions.Add(new Question("מה הפירוש של המילה 'increase'?", "לצמצם", "להכפיל", "להפחית", "להגדיל", 4));
    database.Questions.Add(new Question("מה פירוש המילה 'bonjour' בצרפתית?", "ביי", "תודה", "שלום", "לילה טוב", 3));
    database.Questions.Add(new Question("מה פירוש המילה 'gracias' בספרדית?", "שלום", "בבקשה", "תודה", "סליחה", 3));
    database.Questions.Add(new Question("מהי השפה הרשמית של יפן?", "קוריאנית", "סינית", "יפנית", "טיבטית", 3));
    database.Questions.Add(new Question("באיזו מדינה מדברים בעיקר גרמנית?", "שווייץ", "אוסטריה", "גרמניה", "כל התשובות נכונות", 4));
    database.Questions.Add(new Question("מה הפירוש של 'ciao' באיטלקית?", "שלום ולהתראות", "תודה", "מה נשמע", "אוכל", 1));
    database.Questions.Add(new Question("באיזו שפה המילה 'arigato' משמשת להבעת תודה?", "סינית", "קוריאנית", "יפנית", "הודית", 3));
    database.Questions.Add(new Question("מה פירוש המילה 'hallo' בגרמנית?", "שלום", "תודה", "ביי", "בבקשה", 1));

    database.Questions.Add(new Question("🐍 + 🍎 מה הסיפור שמאחורי האימוג'ים האלה?", "הבריאה", "אדם וחוה", "גן עדן", "נחש ותפוח", 2));
    database.Questions.Add(new Question("🐝 + 🍯 + 🍞 מסמל?", "דבורה אוכלת דבש", "ארוחת בוקר מתוקה", "דבש על לחם", "פרחים ודבש", 3));
    database.Questions.Add(new Question("🌧️ + 🌈 מייצג?", "גשם וסופה", "גשם וקשת בענן", "רוח סוערת", "שמש וחוף", 2));
    database.Questions.Add(new Question("👀 + 💡 משמעות?", "רעיונות חדשים", "להביט באור", "חשיבה", "תצפית", 1));
    database.Questions.Add(new Question("🛣️ + 🏃‍♂️ + ⏳ משמעות?", "ריצה בזמן", "מסע ארוך", "למה למהר", "מסלול מרוץ", 2));
    database.Questions.Add(new Question("🦁 + 👑 + 🐾 מה ההרמוניה בין האימוג'ים?", "מלך היער", "אריה עם כתר", "מלך החיות", "אריה צועד", 3));
    database.Questions.Add(new Question("🔒 + 🔑 + 🏠 מה זה?", "בית מאובטח", "מפתח לבית", "בטיחות", "פרטיות", 1));
    database.Questions.Add(new Question("🧠 + ⚡ + 💡 מייצגים?", "רעיונות מהירים", "חשיבה יצירתית", "מוח זוהר", "הארה", 2));
    database.Questions.Add(new Question("🍎 + 👨‍🏫 + 📚 מה זה?", "אוכל בבית ספר", "מחנך ואוכל", "בית ספר", "למידה", 4));
    database.Questions.Add(new Question("🌍 + 🌱 + 💧 מה מסמל?", "הגנה על הסביבה", "כדור הארץ", "מים וצמחים", "אקולוגיה", 1));
    database.Questions.Add(new Question("⌛ + 🚶‍♂️ + 🛤️ מה זה?", "זמן ללכת", "מסלול ארוך", "ריצה נגד הזמן", "מסע בזמן", 4));
    database.Questions.Add(new Question("🙊 + 💬 מה זה?", "לא מדבר", "שומר סוד", "שקט שווה זהב", "מילה אסורה", 2));

    database.Questions.Add(new Question("מהי העיר הצפונית ביותר בישראל?", "מטולה", "נהריה", "צפת", "חיפה", 1));
    database.Questions.Add(new Question("מהי חברת התעופה הלאומית של ישראל?", "ישראייר", "אל על", "ארקיע", "ריינאייר", 2));
    database.Questions.Add(new Question("באיזו חברה טסים באירופה לרוב בטיסות זולות?", "ישראייר", "ארקיע", "ריינאייר", "סוויס", 3));
    database.Questions.Add(new Question("באיזה אזור נמצא ים המלח?", "הגליל", "הנגב", "הבקעה", "הערבה", 4));
    database.Questions.Add(new Question("באיזה אזור נמצא החרמון?", "הרי ירושלים", "הגליל העליון", "רמת הגולן", "הרי יהודה", 3));
    database.Questions.Add(new Question("באיזה ים נמצאת העיר אילת?", "הים התיכון", "הים האדום", "ים המלח", "הים השחור", 2));
    database.Questions.Add(new Question("מהו המקום הנמוך ביותר בישראל?", "אילת", "הכנרת", "ים המלח", "הירדן", 3));
    database.Questions.Add(new Question("באיזה חבל ארץ נמצא הכנרת?", "הגליל התחתון", "הגליל העליון", "העמקים", "הגולן", 1));
    database.Questions.Add(new Question("מהו חבל הארץ הדרומי ביותר בישראל?", "ערבה", "נגב", "אילת", "בקעה", 2));
    database.Questions.Add(new Question("מהו חבל ארץ בה בה נמצאת אשקלון??", "ערבה", "נגב", "שומרון", "לכיש", 4));





  

















      database.SaveChanges();
    }

  }
}


class Database() : DbBase("database")
{
  public DbSet<User> Users { get; set; } = default!;
  public DbSet<Question> Questions { get; set; } = default!;
  public DbSet<Game> Games { get; set; } = default!;

}

class User(string id, string username, string password)
{
  [Key] public string Id { get; set; } = id;
  public string Username { get; set; } = username;
  public string Password { get; set; } = password;
}


class Question(string quest, string ans1, string ans2, string ans3, string ans4, int ansCorrect)
{
  [Key] public int Id { get; set; } = default!;
  public string Quest { get; set; } = quest;
  public string Ans1 { get; set; } = ans1;
  public string Ans2 { get; set; } = ans2;
  public string Ans3 { get; set; } = ans3;
  public string Ans4 { get; set; } = ans4;
  public int AnsCorrect { get; set; } = ansCorrect;

}

class Game(string gameId, string userId, int gameScore, int totalAnswers, int totalCorrectAnswers, string usedQuestions)
{
  [Key] public string GameId { get; set; } = gameId;
  public string UserId { get; set; } = userId;
  [ForeignKey("UserId")] public User User { get; set; } = default!;

  public int GameScore { get; set; } = gameScore;
  public int TotalAnswers { get; set; } = totalAnswers;
  public int TotalCorrectAnswers { get; set; } = totalCorrectAnswers;
  public string UsedQuestions { get; set; } = usedQuestions;



}





