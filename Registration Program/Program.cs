using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        #region Configuration Variables

        private static readonly bool IsTesting = 
            bool.Parse(ConfigurationManager.AppSettings.Get("IsTesting") ?? "True");
        private static readonly string LoginPageUrl = 
            ConfigurationManager.AppSettings.Get("LoginPageUrl");
        private static readonly string CoursesRegistrationPageUrl = 
            ConfigurationManager.AppSettings.Get("CoursesRegistrationPageUrl");
        private static readonly int LoadingTimoutInSeconds = 
            int.Parse(ConfigurationManager.AppSettings.Get("LoadingTimoutInSeconds") ?? string.Empty);
        private static readonly int TimeCheckCountdownInMilliSeconds = 
            int.Parse(ConfigurationManager.AppSettings.Get("TimeCheckCountdownInMilliSeconds") ?? string.Empty);
        private static readonly int CoursesToRegisterAtATime = 
            int.Parse(ConfigurationManager.AppSettings.Get("CoursesToRegisterAtATime") ?? string.Empty);
        private static readonly ImmutableList<string> CrnsInputBoxesIds = 
            ConfigurationManager.AppSettings.Get("CrnsInputBoxesIds")?.Split(',').ToImmutableList();

        #endregion

        #region User Data

        private static string _username;
        private static string _password;
        private static string _term;
        private static List<string> _coursesCrns;

        #endregion

        #region Drivers

        private static IWebDriver _webDriver;
        private static WebDriverWait _webDriverWait;

        #endregion

        #region Main

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

            if (!IsTesting) WaitTillMidnight();
            
            RegisterCourses(_coursesCrns);

            ConsoleOutput.OutputMessage("Successfuly registered courses");
            ConsoleOutput.OutputError("Press any key to quit");
            Console.ReadKey();
            ConsoleOutput.OutputError("\nQuitting...");
            
            try
            {
                _webDriver.Close();
                _webDriver.Quit();
            }
            catch (Exception) { /* Ignored */ }

            Environment.Exit(0);
        }

        #endregion

        #region Functions

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
            GetElement(By.Name(ConfigurationManager.AppSettings.Get("LoginUsernameInputBoxElement"))).SendKeys(_username);
            GetElement(By.Name(ConfigurationManager.AppSettings.Get("LoginPasswordInputBoxElement"))).SendKeys(_password);
            GetElement(By.Id(ConfigurationManager.AppSettings.Get("LoginButtonElement"))).Click();
        }

        private static bool IsSignedIn()
        {
            try
            {
                WaitUntilElementIsLoaded(By.Id(ConfigurationManager.AppSettings.Get("WelcomeMessageElement")));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ChooseTerm(string term)
        {
            IWebElement termSelection = GetElement(By.Name(ConfigurationManager.AppSettings.Get("TermSelectionDropDownElement")));

            termSelection.Click();
            termSelection.SendKeys(term);
            termSelection.SendKeys(Keys.Enter);
            termSelection.Submit();
        }

        private static void WaitTillMidnight()
        {
            // TODO: Wait till midnight; calculate time difference
            // TODO: Break condition
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
            WaitUntilElementIsLoaded(By.Id(ConfigurationManager.AppSettings.Get("SubmitRegistrationButtonElement")));

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
            GetElement(By.Id(ConfigurationManager.AppSettings.Get("SubmitRegistrationButtonElement"))).Click();
        }

        private static IWebElement GetElement(By element) => _webDriver.FindElement(element);
        private static void WaitUntilElementIsLoaded(By element) => _webDriverWait.Until(d => d.FindElement(element));

        #endregion
    }
}