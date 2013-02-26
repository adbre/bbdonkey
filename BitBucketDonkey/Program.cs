using System;
using System.Collections.Generic;
using System.Linq;
using RestSharp;

namespace BitBucket
{
    static class Program
    {
        private static string _baseUri;
        private static string _username;
        private static string _password;

        private static IRestClient _client;
        private static string _ownerFilter;

        static int Main(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h") || args.Contains("/?") || args.Contains("help"))
            {
                PrintHelp();
                return -1;
            }

            return TryRunProgram(args);
        }

        private static int TryRunProgram(string[] args)
        {
            try
            {
                RunProgram(args);
                return 0;
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
                return 1;
            }
        }

        private static void RunProgram(string[] args)
        {
            SetupRestClient(args);

            Console.WriteLine("Obtaining a list of repositories from BitBucket...");
            var repositories = FetchRepositories();

            if (!string.IsNullOrWhiteSpace(_ownerFilter))
            {
                var originalCount = repositories.Length;
                repositories = repositories.Where(r => r.owner == _ownerFilter).ToArray();
                Console.WriteLine("{0} repostories found. {1} repostitory matched owner filter '{2}'", originalCount, repositories.Length, _ownerFilter);
            }
            else
                Console.WriteLine("{0} repostories found.", repositories.Length);

            foreach (var bitBucketRepository in repositories)
            {
                Console.WriteLine("{0}/{1}", bitBucketRepository.owner, bitBucketRepository.name);
                var followers = FetchFollowers(bitBucketRepository);
                foreach (var follower in followers)
                {
                    Console.WriteLine("\t{2}\t{0} {1}", follower.first_name, follower.last_name, follower.username);
                }
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("bbdonkey.exe USERNAME -p|PASSWORD [OWNERFILTER]");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("\tbbdonkey.exe myUser myPassword");
        }

        private static void SetupRestClient(string[] args)
        {
            _baseUri = "https://api.bitbucket.org/";
            _username = args.Length > 0 ? args[0] : ConsoleHelper.ReadLineUntilNotEmpty("Username");
            _password = args.Length > 1 ? args[1] : null;
            _ownerFilter = args.Length > 2 ? args[2] : null;

            if (string.IsNullOrWhiteSpace(_password) || _password == "-p")
                _password = ConsoleHelper.ReadPasswordLineUntilNotEmpty("Password");

            Console.WriteLine("Using URI: {0} and user {1}", _baseUri, _username);
            _client = new RestClient(_baseUri)
                {
                    Authenticator = new HttpBasicAuthenticator(_username, _password)
                };
        }

        private static BitBucketRepository[] FetchRepositories()
        {
            return Execute<List<BitBucketRepository>>("1.0/user/repositories", Method.GET).ToArray();
        }

        private static IEnumerable<Follower> FetchFollowers(BitBucketRepository repository)
        {
            var resource = string.Format("1.0/repositories/{0}/{1}/followers", repository.owner, repository.slug);
            var response = Execute<FollowerListWorkAroundBecauseBitBucketDoesntReturnATrueJsonArrayAtTheRootLevel>(resource, Method.GET).followers;
            return response ?? (IEnumerable<Follower>) new Follower[0];
        }

        private static T Execute<T>(string resource, Method method) where T : new()
        {
            var response = _client.Execute<T>(new RestRequest(resource, method));
            if (response.ResponseStatus != ResponseStatus.Completed)
                throw new Exception(string.Format("Unable to complete HTTP request - {0}", response.StatusCode));
            return response.Data;
        }
    }

    internal static class ConsoleHelper
    {
        public static string ReadLineUntilNotEmpty(string prefix)
        {
            return ReadWhile(prefix, Console.ReadLine, s => !String.IsNullOrWhiteSpace(s));
        }

        public static string ReadPasswordLineUntilNotEmpty(string prefix)
        {
            return ReadWhile(prefix, ReadPasswordLine, s => !String.IsNullOrWhiteSpace(s));
        }

        private static string ReadWhile(string prefix, Func<string> readLine, Func<string, bool> isValid)
        {
            while (true)
            {
                Console.Write("{0}: ", prefix);
                var value = readLine();
                if (isValid(value)) return value;
            }
        }

        private static string ReadPasswordLine()
        {
            var pass = String.Empty;
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    pass = pass.Substring(0, (pass.Length - 1));
                    Console.Write("\b \b");
                }
            }
                // Stops Receving Keys Once Enter is Pressed
            while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return pass;
        }
    }

    internal class FollowerListWorkAroundBecauseBitBucketDoesntReturnATrueJsonArrayAtTheRootLevel
    {
        // ReSharper disable InconsistentNaming
        public int count { get; set; }
        public List<Follower> followers { get; set; }
        // ReSharper restore InconsistentNaming
    }

    internal class Follower
    {
        // ReSharper disable InconsistentNaming
        public string username { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        // ReSharper restore InconsistentNaming
    }

    public class BitBucketRepository
    {
        // ReSharper disable InconsistentNaming
        public string scm { get; set; }
        public bool has_wiki { get; set; }
        public DateTime last_updated { get; set; }
        public string creator { get; set; }
        public DateTime created_on { get; set; }
        public string owner { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        // ReSharper restore InconsistentNaming
    }
}
