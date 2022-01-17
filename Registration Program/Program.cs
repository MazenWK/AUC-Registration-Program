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
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    internal static class Program
    {
        private static readonly bool IsTesting = bool.Parse(ConfigurationManager.AppSettings.Get("Testing") ?? "True");
        
        private const string BannerUrl = "https://ssb-prod.ec.aucegypt.edu/PROD/twbkwbis.P_WWWLogin";
        private const string RegistrationUrl = "https://ssb-prod.ec.aucegypt.edu/PROD/bwskfreg.P_AltPin";
        private const int LoadingTimoutInSeconds = 15;
        private const int TimeCheckCountdownInMilliSeconds = 1000;

        private static string _username;
        private static string _password;
        private static string _semester;
        private static List<string> _coursesCrns;

        private static IWebDriver _webDriver;
        private static WebDriverWait _webDriverWait;

        private static readonly List<string> CrnsInputBoxesIds = new List<string>()
            {"crn_id1", "crn_id2", "crn_id3", "crn_id4", "crn_id5", "crn_id6", "crn_id7", "crn_id8", "crn_id9", "crn_id10"};

        private static void Main()
        {
            GetCredentials();
            GetRegistrationInfo();
            
            CreateDrivers();
            NavigateToUrl(BannerUrl);
            
            SignIn();
            while (!IsSignedIn())
            {
                Output.OutputError("Unable to sign in - Invalid credentials");
                NavigateToUrl(BannerUrl);
                GetCredentials();
                SignIn();
            }
            
            NavigateToUrl(RegistrationUrl);
            ChooseTerm(_semester);
            
            WaitTillMidnight();
            RegisterCourses(_coursesCrns);
            
            Output.OutputMessage($"Successfuly {_coursesCrns.Count} courses");
            Output.OutputError("Press any key to quit");
            Console.ReadKey();

            try
            {
                _webDriver.Close();
                _webDriver.Quit();
            }
            catch (Exception) { /* Ignored */}
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
                Output.OutputError("Invalid input - Input credentials again");
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
                        // remove one character from the list of password characters
                        password = password.Substring(0, password.Length - 1);
                        // get the location of the cursor
                        int pos = Console.CursorLeft;
                        // move the cursor to the left by one character
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        // replace it with space
                        Console.Write(" ");
                        // move the cursor to the left by one character again
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                    }
                }
                info = Console.ReadKey(true);
            }
            // add a new line because user pressed enter at the end of their password
            Console.WriteLine();
            return password;
        }
        private static bool IsValid(string s) => !(string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s));
        private static void GetRegistrationInfo()
        {
            Console.Write("Input full semester name (as stated in AUC banner): ");
            _semester = Console.ReadLine();
            
            Console.Write("Input courses CRNs (IN ORDER OF IMPORTANCE) with commas separating each CRN: ");
            _coursesCrns = Console.ReadLine()?.Replace(" ", string.Empty).Split(',').ToList();
        }

        private static void CreateDrivers()
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
            int n = 0;
            while (DateTime.Now.Hour != 0)
            {
                if (n == 0) Window.Focus();
                if (n++ % 10 == 0) Output.OutputWarning("Awaiting 12:00 AM");
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
                if (!IsTesting && coursesInputted % 3 == 0) SubmitRegistration();
            }
            if (!IsTesting && coursesInputted % 3 != 0) SubmitRegistration();

        }
        private static void SubmitRegistration()
        {
            GetElement(By.Id("id____UID5")).Click();
        }

        private static IWebElement GetElement(By element) => _webDriver.FindElement(element);
        private static void WaitUntilElementIsLoaded(By element) => _webDriverWait.Until(d => d.FindElement(element));
    }
}