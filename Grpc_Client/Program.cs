using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grpc_Service;
using Grpc_Service.Services;

namespace Grpc_Client
{
    class Program
    {
        public static readonly Random random = new Random();

        static async Task Main()
        {
            var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new Game.GameClient(channel);
            
            User user = new User();
            user.UserId = random.Next(1,100000);

            GameStart gameStart = await client.RegisterUserAsync(user);
            Console.WriteLine("Waiting for another user to register...");
            while(gameStart.Ready ==false)
            {
              
                var t = Task.Run(async delegate
                {
                    await Task.Delay(1000);
                    gameStart = client.CheckForAnotherUser(gameStart);
                });
                t.Wait();
            }


            Console.WriteLine("Game starts, user " + user.UserId);


            
            int score = 0;
            for (int i = 0; i< 10; i++)
            {
                var question = client.GetQuestion(new QuestionRequest { UserId = user.UserId });
                Console.WriteLine($"Question {question.Id}: {question.Text}");
                for (int j = 0; j<question.Options.Count; j++)
                {
                    Console.WriteLine($"{(char)('A' + j)}. {question.Options[j]}");
                }
                
                Console.Write("Your answer (A/B/C/D): ");
               string selectedOption = Console.ReadLine();
                
                
                var answer = client.SubmitAnswer(new Answer { QuestionId = question.Id, UserId = user.UserId, SelectedOption = selectedOption });
                if (answer.Correct)
                {
                    Console.WriteLine("Correct!");
                    score++;
                }
                else
                {
                    Console.WriteLine("Incorrect!");
                }
            }
            var condition = client.CheckForWinCondition(user);
            while (!condition.Defeat && !condition.Victory && !condition.Draw)
            {
                var delay = Task.Delay(1000).ContinueWith(_ =>
                {
                    condition = client.CheckForWinCondition(user);

                });
                delay.Wait();
            }
            Console.WriteLine("Game over! ");

            Console.WriteLine(condition.Draw ? "Draw" : condition.Victory ? "Victory" : "Defeat");
            Console.WriteLine($"Final score: {condition.User.CorrectAnswers}");
            Console.WriteLine($"Your enemy: {condition.Enemy.CorrectAnswers}");
            client.FinishGame(new CleanUsers());
            Console.ReadLine();


        }

    }
}
