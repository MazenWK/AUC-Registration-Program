using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace Registration_Program
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    internal static class Program
    {
        private static readonly bool IsTesting = bool.Parse(ConfigurationManager.AppSettings.Get("Testing") ?? "True");

        private const string LoginPageUrl = "https://ssb-prod.ec.aucegypt.edu/PROD/twbkwbis.P_WWWLogin";
        private const string CoursesRegistrationPageUrl = "https://ssb-prod.ec.aucegypt.edu/PROD/bwskfreg.P_AltPin";
        private const int LoadingTimoutInSeconds = 15;
        private const int TimeCheckCountdownInMilliSeconds = 1000;
        private const int CoursesToRegisterAtATime = 2;

        private static string _username;
        private static string _password;
        private static string _term;
        private static List<string> _coursesCrns;

        private static IWebDriver _webDriver;
        private static WebDriverWait _webDriverWait;

        private static readonly List<string> CrnsInputBoxesIds = new() 
            {"crn_id1", "crn_id2", "crn_id3", "crn_id4", "crn_id5", "crn_id6", "crn_id7", "crn_id8", "crn_id9", "crn_id10"};

        private static void Main()
        {
            GetCredentials();
            GetRegistrationInfo();

            CreateWebDrivers();
            NavigateToUrl(LoginPageUrl);

            SignIn();
            while (!IsSignedIn())
            {
                ConsoleWindow.Focus();
                ConsoleOutput.OutputError("Unable to sign in - Invalid credentials");
                NavigateToUrl(LoginPageUrl);
                GetCredentials();
                SignIn();
            }

            NavigateToUrl(CoursesRegistrationPageUrl);
            ChooseTerm(_term);

            if (!IsTesting) 
                WaitTillMidnight();
            
            RegisterCourses(_coursesCrns);

            ConsoleOutput.OutputMessage($"Successfuly registered {_coursesCrns.Count} courses");
            ConsoleOutput.OutputError("Press any key to quit");
            Console.ReadKey();
            ConsoleOutput.OutputError("Quitting...");
            
            try
            {
                _webDriver.Close();
                _webDriver.Quit();
            }
            catch (Exception) { /* Ignored */ }

            Environment.Exit(0);
        }

        private static void GetCredentials()
        {
            Console.Write("Input username: ");
            _username = Console.ReadLine();

            Console.Write("Input password: ");
            _password = GetPassword();

            while (!(IsValid(_username) && IsValid(_password)))
            {
                ConsoleOutput.OutputError("Invalid input - Input credentials again");
                GetCredentials();
            }
        }

        private static string GetPassword()
        {
            string password = "";
            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key != ConsoleKey.Backspace)
                {
                    Console.Write("*");
                    password += info.KeyChar;
                }
                else if (info.Key == ConsoleKey.Backspace)
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        password = password.Substring(0, password.Length - 1);
                        
                        int pos = Console.CursorLeft;
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        Console.Write(" ");
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                    }
                }

                info = Console.ReadKey(true);
            }

            Console.WriteLine();
            return password;
        }

        private static bool IsValid(string s) => !(string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));

        private static void GetRegistrationInfo()
        {
            Console.Write("Input full semester name (as stated in AUC banner): ");
            _term = Console.ReadLine();

            Console.Write("Input courses CRNs (IN ORDER OF IMPORTANCE) with commas separating each CRN: ");
            _coursesCrns = Console.ReadLine()?.Replace(" ", string.Empty).Split(',').ToList();
        }

        private static void CreateWebDrivers()
        {
            _webDriver = new ChromeDriver();
            _webDriverWait = new WebDriverWait(_webDriver, new TimeSpan(0, 0, LoadingTimoutInSeconds));
        }

        private static void NavigateToUrl(string url)
        {
            _webDriver.Navigate().GoToUrl(url);
        }

        private static void SignIn()
        {
            GetElement(By.Name("sid")).SendKeys(_username);
            GetElement(By.Name("PIN")).SendKeys(_password);
            GetElement(By.Id("id____UID0")).Click();
        }

        private static bool IsSignedIn()
        {
            try
            {
                WaitUntilElementIsLoaded(By.Id("welcomemessage"));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ChooseTerm(string term)
        {
            IWebElement termSelection = GetElement(By.Name("term_in"));

            termSelection.Click();
            termSelection.SendKeys(term);
            termSelection.SendKeys(Keys.Enter);
            termSelection.Submit();
        }

        private static void WaitTillMidnight()
        {
            // TODO: Wait till midnight; calculate time difference
            int n = 0;
            while (DateTime.Now.Hour != 0)
            {
                if (n == 0) ConsoleWindow.Focus();
                if (n++ % 10 == 0) ConsoleOutput.OutputWarning("Awaiting 12:00 AM");
                Thread.Sleep(TimeCheckCountdownInMilliSeconds);
            }
        }

        private static void RegisterCourses(IReadOnlyList<string> courses)
        {
            WaitUntilElementIsLoaded(By.Id("id____UID5"));

            int coursesInputted = 0;
            for (int i = 0; i < courses.Count; i++)
            {
                coursesInputted++;
                GetElement(By.Id(CrnsInputBoxesIds[i])).SendKeys(courses[i]);
                if (!IsTesting && coursesInputted % CoursesToRegisterAtATime == 0) SubmitRegistration();
            }

            if (!IsTesting && coursesInputted % CoursesToRegisterAtATime != 0) SubmitRegistration();
        }

        private static void SubmitRegistration()
        {
            GetElement(By.Id("id____UID5")).Click();
        }

        private static IWebElement GetElement(By element) => _webDriver.FindElement(element);
        private static void WaitUntilElementIsLoaded(By element) => _webDriverWait.Until(d => d.FindElement(element));
    }
}