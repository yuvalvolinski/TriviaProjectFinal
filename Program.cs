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

            var game = database.Games.Find(gameId);
            if(game == null) {
              Console.WriteLine("game is null" );
            }
            else {
              Console.WriteLine("game" + game);
            }
 

            string gameUsedQuestions = game?.UsedQuestions!;
            List<int> usedQuestions = new List<int>();

            if(gameUsedQuestions != null && gameUsedQuestions != "") {
               usedQuestions = gameUsedQuestions
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();

            }
            else {
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
              

              var game = database.Games.Find(gameId);
               totalScore = game.GameScore;

              string gameUsedQuestions = game?.UsedQuestions!;
              string [] arrUsedQuestions = gameUsedQuestions.Split(",");
              int lastQuestion = int.Parse (arrUsedQuestions[arrUsedQuestions.Length-1]);

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

            if(ifCorrect){
              game.TotalCorrectAnswers++;
            }
           
            database.SaveChanges();     

            response.Send(correctAns);





            
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
      database.Questions.Add(new Question("באיזו עיר התקיים אירוויזיון 2024?", "האג", "מאלמו", "סטוקהולם", "ברצלונה", 2)); // אירוויזיון 2024

     database.Questions.Add(new Question("מי המציא את פייסבוק?", "סטיב ג'ובס", "אילון מאסק", "מארק צוקרברג", "ביל גייטס", 3)); // אנשים (אישיות)
     database.Questions.Add(new Question("מי שיחק את הדמות של 'הארי פוטר' בסדרת הסרטים?", "אלייז'ה ווד", "דניאל רדקליף", "רופרט גרינט", "טום הולנד", 2)); // אנשים (אישיות)
     database.Questions.Add(new Question("מי היה דובר צהל בשבעב באוקטובר?", "בנימין נתניהו", "בני גנץ ", "דניאל הגרי", "רוני גמזו", 3)); // אנשים (אישיות)
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





