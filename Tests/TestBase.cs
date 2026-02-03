using System;

namespace CryptoFileExchange.Tests
{
    internal abstract class TestBase
    {
        protected static int _passedTests = 0;
        protected static int _failedTests = 0;

        /// <summary>
        /// Returns test suite name for usage in summary display
        /// </summary>
        protected abstract string GetTestSuiteName();

        /// <summary>
        /// Displays tests summary
        /// </summary>
        protected void PrintSummary()
        {
            Console.WriteLine($"\n=== {GetTestSuiteName()} Summary ===");
            Console.WriteLine($"Total Tests: {_passedTests + _failedTests}");
            Console.WriteLine($"Passed: {_passedTests}");
            Console.WriteLine($"Failed: {_failedTests}");
            
            if (_failedTests == 0)
            {
                Console.WriteLine("All tests PASSED!");
            }
            else
            {
                Console.WriteLine($" {_failedTests} test(s) FAILED!");
            }
        }

        /// <summary>
        /// Marks test as passed
        /// </summary>
        protected void Pass(string message)
        {
            _passedTests++;
            Console.WriteLine($"PASSED - {message}");
        }

        /// <summary>
        /// Marks test as failed
        /// </summary>
        protected void Fail(string message)
        {
            _failedTests++;
            Console.WriteLine($"FAILED - {message}");
        }

        /// <summary>
        /// Resets tests counter (called on start of RunAllTests)
        /// </summary>
        protected void ResetCounters()
        {
            _passedTests = 0;
            _failedTests = 0;
        }

        /// <summary>
        /// Returns current tests results
        /// </summary>
        protected (int passed, int failed) GetResults()
        {
            return (_passedTests, _failedTests);
        }
    }
}
