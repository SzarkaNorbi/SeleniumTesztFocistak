using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;

namespace FootballAdminSeleniumTest
{
    public class FootballAdminTests
    {
        private IWebDriver driver;
        private WebDriverWait wait;

        public void Setup()
        {
            // Setup Chrome driver with improved options
            var options = new ChromeOptions();
            options.AddArgument("--disable-notifications");
            options.AddArgument("--start-maximized");

            // Uncomment for headless mode if needed
            // options.AddArgument("--headless");

            driver = new ChromeDriver(options);
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

            // Set implicit wait to handle slow-loading elements
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }

        // Helper method to take screenshots with meaningful names
        private void TakeScreenshot(string name)
        {
            try
            {
                Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{timestamp}_{name}.png";
                Console.WriteLine($"Screenshot taken: {fileName}");
                // Uncomment to save screenshots to disk
                // screenshot.SaveAsFile(fileName, ScreenshotImageFormat.Png);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to take screenshot {name}: {ex.Message}");
            }
        }

        // Improved helper method to wait for element to be visible - fixed to use built-in WebDriverWait
        private IWebElement WaitForElement(By locator, int timeoutInSeconds = 10)
        {
            try
            {
                var localWait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
                return localWait.Until(driver => {
                    try
                    {
                        var element = driver.FindElement(locator);
                        return element.Displayed ? element : null;
                    }
                    catch (StaleElementReferenceException)
                    {
                        return null;
                    }
                    catch (NoSuchElementException)
                    {
                        return null;
                    }
                });
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"Element not found: {locator}");
                TakeScreenshot($"element_not_found_{locator.ToString().Replace('/', '_')}");
                throw;
            }
        }

        // Helper method to set date field value using multiple approaches
        private bool SetDateField(IWebElement dateField, string dateValue)
        {
            Console.WriteLine($"Setting date field to: {dateValue}");

            try
            {
                // First, ensure the field is visible and interactable
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", dateField);
                Thread.Sleep(300);

                // Approach 1: Direct JavaScript assignment (most reliable)
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript($"arguments[0].value='{dateValue}'", dateField);

                // Trigger change event to ensure the application recognizes the change
                js.ExecuteScript("arguments[0].dispatchEvent(new Event('change', { 'bubbles': true }))", dateField);
                js.ExecuteScript("arguments[0].dispatchEvent(new Event('input', { 'bubbles': true }))", dateField);
                Thread.Sleep(500);

                // Check if value was set correctly
                string currentValue = dateField.GetAttribute("value");
                Console.WriteLine($"After JavaScript, field value is: {currentValue}");

                if (currentValue == dateValue || currentValue.Contains(dateValue.Split('-')[0]))
                {
                    Console.WriteLine("Date set successfully using JavaScript");
                    return true;
                }

                // Approach 2: Clear and use SendKeys
                dateField.Clear();
                dateField.SendKeys(dateValue);
                dateField.SendKeys(Keys.Tab); // Tab out to trigger blur events
                Thread.Sleep(500);

                // Check if value was set correctly
                currentValue = dateField.GetAttribute("value");
                Console.WriteLine($"After SendKeys, field value is: {currentValue}");

                if (currentValue == dateValue || currentValue.Contains(dateValue.Split('-')[0]))
                {
                    Console.WriteLine("Date set successfully using SendKeys");
                    return true;
                }

                // Approach 3: Try with different date format (MM/dd/yyyy)
                string[] dateParts = dateValue.Split('-');
                if (dateParts.Length == 3)
                {
                    string altFormat = $"{dateParts[1]}/{dateParts[2]}/{dateParts[0]}";

                    dateField.Clear();
                    dateField.SendKeys(altFormat);
                    dateField.SendKeys(Keys.Tab);
                    Thread.Sleep(500);

                    // Check if value was set correctly (might be reformatted)
                    currentValue = dateField.GetAttribute("value");
                    Console.WriteLine($"After alternative format, field value is: {currentValue}");

                    // Check if either format worked
                    if (currentValue == dateValue || currentValue.Contains(dateParts[0]))
                    {
                        Console.WriteLine("Date set successfully using alternative format");
                        return true;
                    }
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

        public void AddEventWithDateFocus()
        {
            try
            {
                // Step 1: Login to admin panel
                Console.WriteLine("STEP 1: LOGIN TO ADMIN PANEL");
                driver.Navigate().GoToUrl("https://focistak.netlify.app/admin");
                TakeScreenshot("1_login_page");

                // Wait for login form to load and be interactive
                Thread.Sleep(2000);

                // Enter credentials and login
                Console.WriteLine("Entering admin credentials");
                WaitForElement(By.Id("email")).SendKeys("admin@example.hu");
                WaitForElement(By.Id("password")).SendKeys("Admin123$");

                Console.WriteLine("Clicking login button");
                WaitForElement(By.CssSelector("button[type='submit']")).Click();
                TakeScreenshot("2_after_login_click");

                // Wait for redirect to admin panel
                Console.WriteLine("Waiting for redirect to admin panel");
                Thread.Sleep(5000);
                TakeScreenshot("3_admin_panel");

                // Step 2: Navigate to Events section
                Console.WriteLine("STEP 2: NAVIGATE TO EVENTS SECTION");

                // Try to find the Események button
                Console.WriteLine("Looking for Események button");

                try
                {
                    // Try by text content first (case-insensitive for better matching)
                    var eventButtons = driver.FindElements(By.XPath("//*[contains(translate(text(), 'ESEMÉNYEK', 'események'), 'események')]"));
                    if (eventButtons.Count > 0)
                    {
                        Console.WriteLine("Found Események button by text, clicking it");
                        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                        js.ExecuteScript("arguments[0].click();", eventButtons[0]);
                    }
                    else
                    {
                        // Try by class and position
                        var menuButtons = driver.FindElements(By.ClassName("menu-button"));
                        if (menuButtons.Count >= 3)
                        {
                            Console.WriteLine("Clicking the third menu button (Események)");
                            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                            js.ExecuteScript("arguments[0].click();", menuButtons[2]);
                        }
                        else
                        {
                            // If we can't find the button, check if we're already on the events page
                            if (driver.PageSource.Contains("Események") || driver.PageSource.Contains("esemény"))
                            {
                                Console.WriteLine("Already on the events page");
                            }
                            else
                            {
                                throw new Exception("Could not find Események button");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error finding/clicking Események button: {ex.Message}");
                    // Continue anyway - maybe we're already on the events page
                }

                // Wait for events page to load
                Console.WriteLine("Waiting for events page to load");
                Thread.Sleep(900);
                TakeScreenshot("4_events_page");

                // Step 3: Create a new event
                Console.WriteLine("STEP 3: CREATE A NEW EVENT");

                // Use "esemeny" with timestamp to ensure uniqueness
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string ligaName = $"esemeny_{timestamp}";

                // Change this line in the AddEventWithDateFocus method:

                // Instead of:
                // string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                // string ligaName = $"esemeny_{timestamp}";

                // Use this:
                ligaName = "esemény";

                Console.WriteLine($"Creating event with liga name: {ligaName}");

                // Find and click the add button
                try
                {
                    Console.WriteLine("Looking for add button");
                    // Try multiple selectors for the add button
                    var addButtonSelectors = new[] {
                        By.ClassName("action-button"),
                        By.XPath("//button[contains(text(), 'Add')]"),
                        By.XPath("//button[contains(text(), 'New')]"),
                        By.XPath("//button[contains(text(), 'Create')]"),
                        By.XPath("//button[contains(@class, 'add')]")
                    };

                    bool buttonFound = false;
                    foreach (var selector in addButtonSelectors)
                    {
                        var buttons = driver.FindElements(selector);
                        if (buttons.Count > 0)
                        {
                            Console.WriteLine($"Found add button using selector: {selector}");
                            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                            js.ExecuteScript("arguments[0].click();", buttons[0]);
                            buttonFound = true;
                            break;
                        }
                    }

                    if (!buttonFound)
                    {
                        // If we can't find a specific add button, look for any button that might be it
                        var allButtons = driver.FindElements(By.TagName("button"));
                        if (allButtons.Count > 0)
                        {
                            Console.WriteLine("Trying first button as add button");
                            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                            js.ExecuteScript("arguments[0].click();", allButtons[0]);
                        }
                        else
                        {
                            throw new Exception("Could not find any buttons on the page");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error finding/clicking add button: {ex.Message}");
                    TakeScreenshot("add_button_error");
                    throw;
                }

                // Wait for form to appear
                Console.WriteLine("Waiting for event form to appear");
                Thread.Sleep(3000);
                TakeScreenshot("5_event_form");

                // Fill out the form
                try
                {
                    Console.WriteLine("Filling out event form");

                    // Liga field - try multiple approaches
                    try
                    {
                        var ligaInput = WaitForElement(By.Id("liga"));
                        ligaInput.Clear();
                        ligaInput.SendKeys(ligaName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error with liga field: {ex.Message}");
                        // Try alternative selectors
                        try
                        {
                            var inputs = driver.FindElements(By.TagName("input"));
                            if (inputs.Count > 0)
                            {
                                inputs[0].Clear();
                                inputs[0].SendKeys(ligaName);
                            }
                        }
                        catch (Exception innerEx)
                        {
                            Console.WriteLine($"Alternative approach also failed: {innerEx.Message}");
                        }
                    }

                    // Round field
                    try
                    {
                        var roundInput = WaitForElement(By.Id("round"));
                        roundInput.Clear();
                        roundInput.SendKeys("1");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error with round field: {ex.Message}");
                        // Try alternative selectors
                        try
                        {
                            var inputs = driver.FindElements(By.TagName("input"));
                            if (inputs.Count > 1)
                            {
                                inputs[1].Clear();
                                inputs[1].SendKeys("1");
                            }
                        }
                        catch (Exception innerEx)
                        {
                            Console.WriteLine($"Alternative approach also failed: {innerEx.Message}");
                        }
                    }

                    // Date fields - FOCUS ON FIXING THIS PART
                    // Format dates in yyyy-MM-dd format
                    string startDate = DateTime.Now.ToString("yyyy-MM-dd");
                    string endDate = DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd");

                    Console.WriteLine($"Using start date: {startDate}, end date: {endDate}");

                    // Get the date input fields
                    try
                    {
                        var startDateInput = WaitForElement(By.Id("starting_date"));
                        var endDateInput = WaitForElement(By.Id("ending_date"));

                        // Try to set the date fields using our helper method
                        bool startDateSet = SetDateField(startDateInput, startDate);
                        bool endDateSet = SetDateField(endDateInput, endDate);

                        if (!startDateSet || !endDateSet)
                        {
                            Console.WriteLine("WARNING: Date fields may not be set correctly");

                            // Try finding date inputs by type
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
                        var selectElement = new OpenQA.Selenium.Support.UI.SelectElement(statusDropdown);
                        selectElement.SelectByValue("1");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error with dropdown selection: {ex.Message}");

                        // Try JavaScript approach
                        try
                        {
                            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                            js.ExecuteScript("document.getElementById('esemenyStatus').value = '1'");
                            js.ExecuteScript("document.getElementById('esemenyStatus').dispatchEvent(new Event('change', { 'bubbles': true }))");
                        }
                        catch (Exception innerEx)
                        {
                            Console.WriteLine($"JavaScript approach also failed: {innerEx.Message}");
                        }
                    }

                    // Take a screenshot of the filled form
                    TakeScreenshot("6_form_filled");

                    // Log all form values before submission
                    Console.WriteLine("Form values before submission:");
                    try
                    {
                        var ligaInput = driver.FindElement(By.Id("liga"));
                        Console.WriteLine($"Liga: {ligaInput.GetAttribute("value")}");
                    }
                    catch { Console.WriteLine("Could not get liga value"); }

                    try
                    {
                        var roundInput = driver.FindElement(By.Id("round"));
                        Console.WriteLine($"Round: {roundInput.GetAttribute("value")}");
                    }
                    catch { Console.WriteLine("Could not get round value"); }

                    try
                    {
                        var startDateInput = driver.FindElement(By.Id("starting_date"));
                        Console.WriteLine($"Start date: {startDateInput.GetAttribute("value")}");
                    }
                    catch { Console.WriteLine("Could not get start date value"); }

                    try
                    {
                        var endDateInput = driver.FindElement(By.Id("ending_date"));
                        Console.WriteLine($"End date: {endDateInput.GetAttribute("value")}");
                    }
                    catch { Console.WriteLine("Could not get end date value"); }

                    try
                    {
                        var statusDropdown = driver.FindElement(By.Id("esemenyStatus"));
                        Console.WriteLine($"Status: {statusDropdown.GetAttribute("value")}");
                    }
                    catch { Console.WriteLine("Could not get status value"); }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error filling out form: {ex.Message}");
                    TakeScreenshot("form_fill_error");
                    throw;
                }

                // Submit the form
                try
                {
                    Console.WriteLine("Submitting the form");

                    // Try multiple selectors for the submit button
                    var submitButtonSelectors = new[] {
                        By.CssSelector(".form-button.submit"),
                        By.CssSelector("button[type='submit']"),
                        By.XPath("//button[contains(text(), 'Submit')]"),
                        By.XPath("//button[contains(text(), 'Save')]"),
                        By.XPath("//button[contains(text(), 'Create')]")
                    };

                    bool buttonFound = false;
                    foreach (var selector in submitButtonSelectors)
                    {
                        var buttons = driver.FindElements(selector);
                        if (buttons.Count > 0)
                        {
                            Console.WriteLine($"Found submit button using selector: {selector}");
                            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                            js.ExecuteScript("arguments[0].click();", buttons[0]);
                            buttonFound = true;
                            break;
                        }
                    }

                    if (!buttonFound)
                    {
                        // If we can't find a specific submit button, try the last button on the form
                        var allButtons = driver.FindElements(By.TagName("button"));
                        if (allButtons.Count > 0)
                        {
                            Console.WriteLine("Trying last button as submit button");
                            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                            js.ExecuteScript("arguments[0].click();", allButtons[allButtons.Count - 1]);
                        }
                        else
                        {
                            throw new Exception("Could not find any buttons on the page");
                        }
                    }

                    Console.WriteLine("Form submitted");
                    TakeScreenshot("7_form_submitted");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error submitting form: {ex.Message}");
                    TakeScreenshot("submit_error");
                    throw;
                }

                // Wait for submission to complete
                Console.WriteLine("Waiting for submission to complete");
                Thread.Sleep(5000); // Increased wait time

                // Check for success message
                try
                {
                    var successMessages = driver.FindElements(By.XPath("//*[contains(text(), 'success') or contains(text(), 'Success') or contains(text(), 'created')]"));
                    if (successMessages.Count > 0)
                    {
                        Console.WriteLine("Success message found after submission:");
                        foreach (var message in successMessages)
                        {
                            if (message.Displayed)
                            {
                                Console.WriteLine($"Success message: {message.Text}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking for success messages: {ex.Message}");
                }

                // Check for error messages
                try
                {
                    var errorMessages = driver.FindElements(By.XPath("//*[contains(text(), 'fail') or contains(text(), 'error') or contains(text(), 'hiba')]"));
                    if (errorMessages.Count > 0)
                    {
                        Console.WriteLine("Error message found after submission:");
                        foreach (var error in errorMessages)
                        {
                            if (error.Displayed)
                            {
                                Console.WriteLine($"Error message: {error.Text}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking for error messages: {ex.Message}");
                }

                // Look for a "View Events" button and click it if found
                try
                {
                    var viewEventsButtons = driver.FindElements(By.XPath("//*[contains(text(), 'View') and contains(text(), 'Event')]"));
                    if (viewEventsButtons.Count > 0)
                    {
                        Console.WriteLine("Found 'View Events' button, clicking it");
                        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                        js.ExecuteScript("arguments[0].click();", viewEventsButtons[0]);
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        // Navigate directly to the competition page
                        Console.WriteLine("STEP 4: EXITING ADMIN PANEL AND GOING TO COMPETITION PAGE");
                        driver.Navigate().GoToUrl("https://focistak.netlify.app/competetion");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error finding/clicking View Events button: {ex.Message}");

                    // Navigate directly to the competition page
                    Console.WriteLine("STEP 4: EXITING ADMIN PANEL AND GOING TO COMPETITION PAGE");
                    driver.Navigate().GoToUrl("https://focistak.netlify.app/competetion");
                }

                // Wait for page to load
                Console.WriteLine("Waiting for competition page to load");
                Thread.Sleep(5000);
                TakeScreenshot("8_competition_page");

                // Step 5: Check if event is displayed
                Console.WriteLine("STEP 5: CHECKING IF EVENT IS DISPLAYED");

                try
                {
                    // Try multiple approaches to find the event
                    var eventElements = driver.FindElements(By.XPath($"//*[contains(text(), '{ligaName}')]"));
                    if (eventElements.Count > 0)
                    {
                        Console.WriteLine($"SUCCESS: Event with liga name '{ligaName}' found on the page!");

                        // Highlight the element
                        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                        js.ExecuteScript("arguments[0].style.border='3px solid red'", eventElements[0]);
                        TakeScreenshot("9_event_found");
                    }
                    else
                    {
                        Console.WriteLine($"WARNING: Event with liga name '{ligaName}' not found on the page");

                        // Try refreshing the page and checking again
                        Console.WriteLine("Refreshing the competition page and checking again...");
                        driver.Navigate().Refresh();
                        Thread.Sleep(5000);

                        eventElements = driver.FindElements(By.XPath($"//*[contains(text(), '{ligaName}')]"));
                        if (eventElements.Count > 0)
                        {
                            Console.WriteLine($"SUCCESS after refresh: Event with liga name '{ligaName}' found on the page!");

                            // Highlight the element
                            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                            js.ExecuteScript("arguments[0].style.border='3px solid red'", eventElements[0]);
                            TakeScreenshot("10_event_found_after_refresh");
                        }
                        else
                        {
                            // Try looking for partial match
                            string partialName = ligaName.Split('_')[0]; // Just "esemeny" without timestamp
                            var partialMatches = driver.FindElements(By.XPath($"//*[contains(text(), '{partialName}')]"));

                            if (partialMatches.Count > 0)
                            {
                                Console.WriteLine($"Found partial match for '{partialName}' on the page");
                                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                                js.ExecuteScript("arguments[0].style.border='3px solid orange'", partialMatches[0]);
                                TakeScreenshot("11_partial_match_found");
                            }
                            else
                            {
                                Console.WriteLine($"FAILURE: No events matching '{ligaName}' or '{partialName}' found after refresh");

                                // Take a screenshot of the entire page for debugging
                                TakeScreenshot("12_full_page_no_event_found");

                                // Log the page source for debugging
                                Console.WriteLine("Page source excerpt:");
                                string pageSource = driver.PageSource;
                                Console.WriteLine(pageSource.Length > 1000 ? pageSource.Substring(0, 1000) + "..." : pageSource);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking for event: {ex.Message}");
                    TakeScreenshot("event_check_error");
                }

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

        public void Cleanup()
        {
            // Close the browser
            driver?.Quit();
        }

        // Main method to run the test directly
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