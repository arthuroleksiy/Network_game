using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyCaching.Core;
using Grpc.Core;

namespace Grpc_Service.Services
{
    public class GameService: Game.GameBase
    {
        private readonly IEasyCachingProvider _provider;
        private const string USERS = "users";

        public GameService(IEasyCachingProvider provider)
        {
            _provider = provider;

        }


        List<User> users = new List<User>();
        Question[] questions = new Question[] {
            new Question
            {
                Id = 1,
                Text = "What is the capital of France?",
                Options = { "Paris", "Berlin", "London", "Madrid" },
                CorrectAnswer = "A"
            },

            new Question
            {
                Id = 2,
                Text = "What is the capital of Spain?",
                Options = { "Paris", "Berlin", "London", "Madrid" },
                CorrectAnswer = "D"
            },


            new Question
            {
                Id = 3,
                Text = "What is the capital of Finland?",
                Options = { "Helsinki", "Warsaw", "London", "Madrid" },
                CorrectAnswer = "A"
            },

            new Question
            {
                Id = 4,
                Text = "What is the capital of Portugal?",
                Options = { "Warsaw", "Athens", "Rome", "Lisabon" },
                CorrectAnswer = "D"
            },

            new Question
            {
                Id = 5,
                Text = "What is the capital of Germany?",
                Options = { "Paris", "Berlin", "London", "Madrid" },
                CorrectAnswer = "B"
            },

            new Question
            {
                Id = 6,
                Text = "What is the capital of Greece?",
                Options = { "Lisabon", "Warsaw", "Athens", "Rome" },
                CorrectAnswer = "C"
            },


            new Question
            {
                Id = 7,
                Text = "What is the capital of the Chzech Republic?",
                Options = { "Paris", "Berlin", "London", "Prague" },
                CorrectAnswer = "D"
            },


            new Question
            {
                Id = 8,
                Text = "What is the capital of the United Kingdom?",
                Options = { "Paris", "Warsaw", "London", "Madrid" },
                CorrectAnswer = "C"
            },

            new Question
            {
                Id = 9,
                Text = "What is the capital of Bulgaria?",
                Options = { "Lisabon", "Sofia", "Athens", "Rome" },
                CorrectAnswer = "B"
            },

            new Question
            {
                Id = 10,
                Text = "What is the capital of Italy?",
                Options = { "Rome", "Warsaw", "Athens", "Budapest" },
                CorrectAnswer = "A"
            }
            };


        public override Task<GameStart> RegisterUser(User user, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                users = _provider.Get<List<User>> (USERS).Value ?? new List<User>();
                users.Add(user);
                _provider.Set(USERS, users, TimeSpan.FromHours(1));
                
               GameStart gameStart = new GameStart { Ready = users.Count() > 1 };

               return gameStart;
           });
            
        }

        public override async Task<Question> GetQuestion(QuestionRequest request, ServerCallContext context)
        {
            users = _provider.Get<List<User>>(USERS).Value;
            var question = users.Where(i => i.UserId == request.UserId).FirstOrDefault()?.CurrentQuestion ?? 0;
            return questions[question];
        }

        public override async Task<AnswerResponse> SubmitAnswer(Answer request, ServerCallContext context)
        {
            users = _provider.Get<List<User>>(USERS).Value;
            var user = users.Where(i => i.UserId == request.UserId).FirstOrDefault();

            var result = questions.Where(i => i.Id == request.QuestionId && i.CorrectAnswer == request.SelectedOption).Count() > 0;
            int point = user.CorrectAnswers;
            
            if (result)
                user.CorrectAnswers++;

            user.CurrentQuestion++;

            users.Where(c => c.UserId == request.UserId).Select(c => user).ToList();
            _provider.Set(USERS, users, TimeSpan.FromHours(1));

            return new AnswerResponse { Correct = result };

        }

        public override Task<GameStart> CheckForAnotherUser(GameStart request, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                users = _provider.Get<List<User>>(USERS).Value ?? new List<User>();
                GameStart gameStart = new GameStart { Ready = users.Count() > 1  };
                return gameStart;
            });
        }

        public override Task<GameEnd> CheckForWinCondition(User requestedUser, ServerCallContext context)
        {
            return Task.Run(() =>
            {
                users = _provider.Get<List<User>>(USERS).Value;
                var user = users.Where(i => i.UserId == requestedUser.UserId).FirstOrDefault();
                var enemy = users.Where(i => i.UserId != requestedUser.UserId).FirstOrDefault();
                if (user.CurrentQuestion > 9 && enemy.CurrentQuestion > 9 && user.CorrectAnswers > enemy.CorrectAnswers)
                {
                    var result = new GameEnd { Victory = true, User = user, Enemy = enemy };

                    return result;

                }
                else if (user.CurrentQuestion > 9 && enemy.CurrentQuestion > 9 && user.CorrectAnswers < enemy.CorrectAnswers)
                {
                    var result = new GameEnd { Defeat = true, User = user, Enemy = enemy };

                    return result;

                }
                else if (user.CurrentQuestion > 9 && enemy.CurrentQuestion > 9 && user.CorrectAnswers == enemy.CorrectAnswers)
                {
                    var result = new GameEnd { Draw = true, User = user, Enemy = enemy };

                    return result;
                }

                return new GameEnd();
            });
        }

        public override Task<CleanUsers> FinishGame(CleanUsers request, ServerCallContext context)
        {

            var delay = Task.Delay(5000).ContinueWith(_ =>
            {
                _provider.Remove(USERS);

                return new CleanUsers();
            });
            delay.Wait();

            return delay;




        }
    }
}
