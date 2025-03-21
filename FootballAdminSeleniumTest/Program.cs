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

        private void TakeScreenshot(string name)
        {
            try
            {
                string fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{name}.png";
                Console.WriteLine($"Screenshot taken: {fileName}");
                // Uncomment to save: ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(fileName, ScreenshotImageFormat.Png);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to take screenshot {name}: {ex.Message}");
            }
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
                TakeScreenshot($"element_not_found_{locator.ToString().Replace('/', '_')}");
                throw;
            }
        }

        private bool SetDateField(IWebElement dateField, string dateValue)
        {
            Console.WriteLine($"Setting date field to: {dateValue}");
            try
            {
                // Scroll to element
                js.ExecuteScript("arguments[0].scrollIntoView(true);", dateField);
                Thread.Sleep(300);

                // Try JavaScript approach first
                js.ExecuteScript($"arguments[0].value='{dateValue}'", dateField);
                js.ExecuteScript("arguments[0].dispatchEvent(new Event('change', { 'bubbles': true }))", dateField);
                js.ExecuteScript("arguments[0].dispatchEvent(new Event('input', { 'bubbles': true }))", dateField);
                Thread.Sleep(500);

                string currentValue = dateField.GetAttribute("value");
                if (currentValue == dateValue || currentValue.Contains(dateValue.Split('-')[0]))
                    return true;

                // Try SendKeys approach
                dateField.Clear();
                dateField.SendKeys(dateValue);
                dateField.SendKeys(Keys.Tab);
                Thread.Sleep(500);

                currentValue = dateField.GetAttribute("value");
                if (currentValue == dateValue || currentValue.Contains(dateValue.Split('-')[0]))
                    return true;

                // Try alternative format
                string[] dateParts = dateValue.Split('-');
                if (dateParts.Length == 3)
                {
                    string altFormat = $"{dateParts[1]}/{dateParts[2]}/{dateParts[0]}";
                    dateField.Clear();
                    dateField.SendKeys(altFormat);
                    dateField.SendKeys(Keys.Tab);
                    Thread.Sleep(500);

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

            // Fallback to any button
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
                TakeScreenshot("1_login_page");
                Thread.Sleep(2000);

                WaitForElement(By.Id("email")).SendKeys("admin@example.hu");
                WaitForElement(By.Id("password")).SendKeys("Admin123$");
                WaitForElement(By.CssSelector("button[type='submit']")).Click();
                TakeScreenshot("2_after_login_click");
                Thread.Sleep(5000);
                TakeScreenshot("3_admin_panel");

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
                Thread.Sleep(900);
                TakeScreenshot("4_events_page");

                // Step 3: Create new event
                Console.WriteLine("STEP 3: CREATE A NEW EVENT");
                string ligaName = "esemény";
                Console.WriteLine($"Creating event with liga name: {ligaName}");

                // Click add button
                By[] addButtonSelectors = {
                    By.ClassName("action-button"),
                    By.XPath("//button[contains(text(), 'Add')]"),
                    By.XPath("//button[contains(text(), 'New')]"),
                    By.XPath("//button[contains(text(), 'Create')]"),
                    By.XPath("//button[contains(@class, 'add')]")
                };
                ClickElement(addButtonSelectors, "add button");
                Thread.Sleep(3000);
                TakeScreenshot("5_event_form");

                // Fill form
                try
                {
                    // Liga field
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

                    // Round field
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

                    // Date fields
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

                    // Status field
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

                    TakeScreenshot("6_form_filled");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error filling out form: {ex.Message}");
                    TakeScreenshot("form_fill_error");
                    throw;
                }

                // Submit form
                By[] submitButtonSelectors = {
                    By.CssSelector(".form-button.submit"),
                    By.CssSelector("button[type='submit']"),
                    By.XPath("//button[contains(text(), 'Submit')]"),
                    By.XPath("//button[contains(text(), 'Save')]"),
                    By.XPath("//button[contains(text(), 'Create')]")
                };
                ClickElement(submitButtonSelectors, "submit button");
                TakeScreenshot("7_form_submitted");
                Thread.Sleep(5000);

                // Check for success/error messages
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
                try
                {
                    var viewEventsButtons = driver.FindElements(By.XPath("//*[contains(text(), 'View') and contains(text(), 'Event')]"));
                    if (viewEventsButtons.Count > 0)
                    {
                        js.ExecuteScript("arguments[0].click();", viewEventsButtons[0]);
                        Thread.Sleep(3000);
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
                Thread.Sleep(5000);
                TakeScreenshot("8_competition_page");

                // Check if event is displayed
                Console.WriteLine("STEP 5: CHECKING IF EVENT IS DISPLAYED");
                CheckForEvent(ligaName);

                Console.WriteLine("Test completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TakeScreenshot("error");
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
                    TakeScreenshot("9_event_found");
                    return;
                }

                Console.WriteLine("Refreshing the competition page and checking again...");
                driver.Navigate().Refresh();
                Thread.Sleep(5000);

                eventElements = driver.FindElements(By.XPath($"//*[contains(text(), '{ligaName}')]"));
                if (eventElements.Count > 0)
                {
                    Console.WriteLine($"SUCCESS after refresh: Event with liga name '{ligaName}' found on the page!");
                    js.ExecuteScript("arguments[0].style.border='3px solid red'", eventElements[0]);
                    TakeScreenshot("10_event_found_after_refresh");
                    return;
                }

                // Try partial match
                string partialName = ligaName.Split('_')[0];
                var partialMatches = driver.FindElements(By.XPath($"//*[contains(text(), '{partialName}')]"));
                if (partialMatches.Count > 0)
                {
                    Console.WriteLine($"Found partial match for '{partialName}' on the page");
                    js.ExecuteScript("arguments[0].style.border='3px solid orange'", partialMatches[0]);
                    TakeScreenshot("11_partial_match_found");
                }
                else
                {
                    Console.WriteLine($"FAILURE: No events matching '{ligaName}' or '{partialName}' found after refresh");
                    TakeScreenshot("12_full_page_no_event_found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for event: {ex.Message}");
                TakeScreenshot("event_check_error");
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
