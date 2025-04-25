using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
              response.Send(userId);
              
          
          

            }
          }
          else if (request.Path == "Login")
          {
            (string NickName, string Password) = request.GetBody<(string, string)>();

            
            var user = database.Users
  
              .FirstOrDefault(user => user.Username.ToLower()== NickName.ToLower() && user.Password == Password);

          

            if(user != null){
              response.Send(user?.Id);
              
            
            }
            else{
              response.Send("Error");
              response.SetStatusCode(401);
            }

           

          
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

 private static void FillQuestions(Database database){

  
    if (!database.Questions.Any())
    {
      database.Questions.Add(new Question("מהי בירת איטליה?", "מילאנו", "רומא", "נאפולי", "פירנצה", 2));

      database.Questions.Add(new Question("מי כתב את המחזה 'המלט'?", "ויליאם שייקספיר", "צ'ארלס דיקנס", "לאו טולסטוי", "מרק טוויין", 1));

      database.Questions.Add(new Question("כמה כוכבים יש בדגל ארצות הברית?", "48", "49", "50", "52", 3));

      database.Questions.Add(new Question("מהי השפה הכי מדוברת בעולם?", "אנגלית", "סינית מנדרינית", "ספרדית", "ערבית", 2));

      database.Questions.Add(new Question("כמה צדדים יש למשושה?", "5", "6", "7", "8", 2));


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
      public string Quest { get; set; } =  quest;
      public string Ans1 { get; set; } = ans1;
      public string Ans2 { get; set; } = ans2;
      public string Ans3 { get; set; } = ans3;
      public string Ans4{ get; set; } = ans4;
      public int AnsCorrect { get; set; } = ansCorrect;

}

class Game(string gameId, string userId, int gameScore, int totalAnswers, int totalCorrectAnswers)
{
  [Key] public string GameId { get; set; } = gameId;
  public string UserId { get; set; } = userId;
 [ForeignKey("UserId")] public User User { get; set; } = default!; 

  public int GameScore { get; set; } = gameScore;
  public int TotalAnswers { get; set; } = totalAnswers;
   public int TotalCorrectAnswers { get; set; } =  totalCorrectAnswers;


}





