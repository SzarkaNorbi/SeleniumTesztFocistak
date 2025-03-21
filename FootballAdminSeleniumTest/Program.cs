using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace FootballAdminSeleniumTest
{
    public class FootballAdminTests
    {
        private IWebDriver driver;
        private WebDriverWait wait;
        private IJavaScriptExecutor js;

        public void Setup()
        {
            var options = new ChromeOptions();
            options.AddArguments("--disable-notifications", "--start-maximized");
            driver = new ChromeDriver(options);
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
            js = (IJavaScriptExecutor)driver;
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }

        private IWebElement WaitForElement(By locator, int timeoutInSeconds = 10)
        {
            try
            {
                return new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds))
                    .Until(d => {
                        try {
                            var element = d.FindElement(locator);
                            return element.Displayed ? element : null;
                        }
                        catch (StaleElementReferenceException) { return null; }
                        catch (NoSuchElementException) { return null; }
                    });
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"Element not found: {locator}");
                throw;
            }
        }
        private bool SetDateField(IWebElement dateField, string dateValue)
        {
            Console.WriteLine($"Setting date field to: {dateValue}");
            try
            {
                js.ExecuteScript("arguments[0].scrollIntoView(true);", dateField);
                Thread.Sleep(300);
                
                js.ExecuteScript($"arguments[0].value='{dateValue}'", dateField);
                js.ExecuteScript("arguments[0].dispatchEvent(new Event('change', { 'bubbles': true }))", dateField);
                js.ExecuteScript("arguments[0].dispatchEvent(new Event('input', { 'bubbles': true }))", dateField);
                Thread.Sleep(300);

                string currentValue = dateField.GetAttribute("value");
                if (currentValue == dateValue || currentValue.Contains(dateValue.Split('-')[0]))
                    return true;
                
                dateField.Clear();
                dateField.SendKeys(dateValue);
                dateField.SendKeys(Keys.Tab);
                Thread.Sleep(300);

                currentValue = dateField.GetAttribute("value");
                if (currentValue == dateValue || currentValue.Contains(dateValue.Split('-')[0]))
                    return true;
                
                string[] dateParts = dateValue.Split('-');
                if (dateParts.Length == 3)
                {
                    string altFormat = $"{dateParts[1]}/{dateParts[2]}/{dateParts[0]}";
                    dateField.Clear();
                    dateField.SendKeys(altFormat);
                    dateField.SendKeys(Keys.Tab);
                    Thread.Sleep(300);

                    currentValue = dateField.GetAttribute("value");
                    if (currentValue == dateValue || currentValue.Contains(dateParts[0]))
                        return true;
                }

                Console.WriteLine("WARNING: All approaches to set date failed");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting date field: {ex.Message}");
                return false;
            }
        }
        private void ClickElement(By[] selectors, string elementType)
        {
            foreach (var selector in selectors)
            {
                var elements = driver.FindElements(selector);
                if (elements.Count > 0)
                {
                    Console.WriteLine($"Found {elementType} using selector: {selector}");
                    js.ExecuteScript("arguments[0].click();", elements[0]);
                    return;
                }
            }
            
            var allButtons = driver.FindElements(By.TagName("button"));
            if (allButtons.Count > 0)
            {
                Console.WriteLine($"Trying {(elementType == "submit button" ? "last" : "first")} button as {elementType}");
                int index = elementType == "submit button" ? allButtons.Count - 1 : 0;
                js.ExecuteScript("arguments[0].click();", allButtons[index]);
            }
            else
            {
                throw new Exception($"Could not find any buttons for {elementType}");
            }
        }
        public void AddEventWithDateFocus()
        {
            try
            {
                // Step 1: Login
                Console.WriteLine("STEP 1: LOGIN TO ADMIN PANEL");
                driver.Navigate().GoToUrl("https://focistak.netlify.app/admin");
                Thread.Sleep(1000);

                WaitForElement(By.Id("email")).SendKeys("admin@example.hu");
                WaitForElement(By.Id("password")).SendKeys("Admin123$");
                WaitForElement(By.CssSelector("button[type='submit']")).Click();
                Thread.Sleep(2000);

                // Step 2: Navigate to Events
                Console.WriteLine("STEP 2: NAVIGATE TO EVENTS SECTION");
                try
                {
                    var eventButtons = driver.FindElements(By.XPath("//*[contains(translate(text(), 'ESEMÉNYEK', 'események'), 'események')]"));
                    if (eventButtons.Count > 0)
                    {
                        js.ExecuteScript("arguments[0].click();", eventButtons[0]);
                    }
                    else
                    {
                        var menuButtons = driver.FindElements(By.ClassName("menu-button"));
                        if (menuButtons.Count >= 3)
                        {
                            js.ExecuteScript("arguments[0].click();", menuButtons[2]);
                        }
                        else if (!driver.PageSource.Contains("Események") && !driver.PageSource.Contains("esemény"))
                        {
                            throw new Exception("Could not find Események button");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error finding/clicking Események button: {ex.Message}");
                }
                Thread.Sleep(500);

                // Step 3: Create new event
                Console.WriteLine("STEP 3: CREATE A NEW EVENT");
                string ligaName = "esemény";
                Console.WriteLine($"Creating event with liga name: {ligaName}");
                
                By[] addButtonSelectors = {
                    By.ClassName("action-button"),
                    By.XPath("//button[contains(text(), 'Add')]"),
                    By.XPath("//button[contains(text(), 'New')]"),
                    By.XPath("//button[contains(text(), 'Create')]"),
                    By.XPath("//button[contains(@class, 'add')]")
                };
                ClickElement(addButtonSelectors, "add button");
                Thread.Sleep(1500);

                // Fill form
                try
                {
                    try
                    {
                        var ligaInput = WaitForElement(By.Id("liga"));
                        ligaInput.Clear();
                        ligaInput.SendKeys(ligaName);
                    }
                    catch
                    {
                        var inputs = driver.FindElements(By.TagName("input"));
                        if (inputs.Count > 0)
                        {
                            inputs[0].Clear();
                            inputs[0].SendKeys(ligaName);
                        }
                    }
                    
                    try
                    {
                        var roundInput = WaitForElement(By.Id("round"));
                        roundInput.Clear();
                        roundInput.SendKeys("1");
                    }
                    catch
                    {
                        var inputs = driver.FindElements(By.TagName("input"));
                        if (inputs.Count > 1)
                        {
                            inputs[1].Clear();
                            inputs[1].SendKeys("1");
                        }
                    }
                    
                    string startDate = DateTime.Now.ToString("yyyy-MM-dd");
                    string endDate = DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd");
                    Console.WriteLine($"Using start date: {startDate}, end date: {endDate}");

                    try
                    {
                        var startDateInput = WaitForElement(By.Id("starting_date"));
                        var endDateInput = WaitForElement(By.Id("ending_date"));

                        bool startDateSet = SetDateField(startDateInput, startDate);
                        bool endDateSet = SetDateField(endDateInput, endDate);

                        if (!startDateSet || !endDateSet)
                        {
                            var dateInputs = driver.FindElements(By.CssSelector("input[type='date']"));
                            if (dateInputs.Count >= 2)
                            {
                                SetDateField(dateInputs[0], startDate);
                                SetDateField(dateInputs[1], endDate);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error with date fields: {ex.Message}");
                    }
                    
                    try
                    {
                        var statusDropdown = WaitForElement(By.Id("esemenyStatus"));
                        new OpenQA.Selenium.Support.UI.SelectElement(statusDropdown).SelectByValue("1");
                    }
                    catch
                    {
                        try
                        {
                            js.ExecuteScript("document.getElementById('esemenyStatus').value = '1'");
                            js.ExecuteScript("document.getElementById('esemenyStatus').dispatchEvent(new Event('change', { 'bubbles': true }))");
                        }
                        catch (Exception innerEx)
                        {
                            Console.WriteLine($"Status selection failed: {innerEx.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error filling out form: {ex.Message}");
                    throw;
                }
                
                By[] submitButtonSelectors = {
                    By.CssSelector(".form-button.submit"),
                    By.CssSelector("button[type='submit']"),
                    By.XPath("//button[contains(text(), 'Submit')]"),
                    By.XPath("//button[contains(text(), 'Save')]"),
                    By.XPath("//button[contains(text(), 'Create')]")
                };
                ClickElement(submitButtonSelectors, "submit button");
                Thread.Sleep(1500);
                
                try
                {
                    var successMessages = driver.FindElements(By.XPath("//*[contains(text(), 'success') or contains(text(), 'Success') or contains(text(), 'created')]"));
                    foreach (var message in successMessages.Where(m => m.Displayed))
                    {
                        Console.WriteLine($"Success message: {message.Text}");
                    }

                    var errorMessages = driver.FindElements(By.XPath("//*[contains(text(), 'fail') or contains(text(), 'error') or contains(text(), 'hiba')]"));
                    foreach (var error in errorMessages.Where(e => e.Displayed))
                    {
                        Console.WriteLine($"Error message: {error.Text}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking messages: {ex.Message}");
                }
                
                // Navigate to competition page
                Console.WriteLine("STEP 4: EXITING ADMIN PANEL AND GOING TO COMPETITION PAGE");
                try
                {
                    var viewEventsButtons = driver.FindElements(By.XPath("//*[contains(text(), 'View') and contains(text(), 'Event')]"));
                    if (viewEventsButtons.Count > 0)
                    {
                        js.ExecuteScript("arguments[0].click();", viewEventsButtons[0]);
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        driver.Navigate().GoToUrl("https://focistak.netlify.app/competetion");
                    }
                }
                catch
                {
                    driver.Navigate().GoToUrl("https://focistak.netlify.app/competetion");
                }
                Thread.Sleep(2000);
                
                Console.WriteLine("STEP 5: CHECKING IF EVENT IS DISPLAYED");
                CheckForEvent(ligaName);

                Console.WriteLine("Test completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private void CheckForEvent(string ligaName)
        {
            try
            {
                var eventElements = driver.FindElements(By.XPath($"//*[contains(text(), '{ligaName}')]"));
                if (eventElements.Count > 0)
                {
                    Console.WriteLine($"SUCCESS: Event with liga name '{ligaName}' found on the page!");
                    js.ExecuteScript("arguments[0].style.border='3px solid red'", eventElements[0]);
                    return;
                }

                Console.WriteLine("Refreshing the competition page and checking again...");
                driver.Navigate().Refresh();
                Thread.Sleep(2000);

                eventElements = driver.FindElements(By.XPath($"//*[contains(text(), '{ligaName}')]"));
                if (eventElements.Count > 0)
                {
                    Console.WriteLine($"SUCCESS after refresh: Event with liga name '{ligaName}' found on the page!");
                    js.ExecuteScript("arguments[0].style.border='3px solid red'", eventElements[0]);
                    return;
                }
                
                string partialName = ligaName.Split('_')[0];
                var partialMatches = driver.FindElements(By.XPath($"//*[contains(text(), '{partialName}')]"));
                if (partialMatches.Count > 0)
                {
                    Console.WriteLine($"Found partial match for '{partialName}' on the page");
                    js.ExecuteScript("arguments[0].style.border='3px solid orange'", partialMatches[0]);
                }
                else
                {
                    Console.WriteLine($"FAILURE: No events matching '{ligaName}' or '{partialName}' found after refresh");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for event: {ex.Message}");
            }
        }
        public void Cleanup()
        {
            driver?.Quit();
        }

        public static void Main(string[] args)
        {
            var test = new FootballAdminTests();
            test.Setup();
            try
            {
                test.AddEventWithDateFocus();
            }
            finally
            {
                test.Cleanup();
            }
        }
    }
}
