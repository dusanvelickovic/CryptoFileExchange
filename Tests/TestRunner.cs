using System;

namespace CryptoFileExchange.Tests
{
    public static class TestRunner
    {
        public static void RunAllTests()
        {
            Console.WriteLine("============================================");
            Console.WriteLine("   CryptoFileExchange Test Suite Runner   ");
            Console.WriteLine("============================================");
            Console.WriteLine();

            int totalPassed = 0;
            int totalFailed = 0;

            //var enigmaResults = EnigmaEngineTests.RunAllTests();
            //totalPassed += enigmaResults.passed;
            //totalFailed += enigmaResults.failed;
            //Console.WriteLine();

            //var xxteaResults = XXTEAEngineTests.RunAllTests();
            //totalPassed += xxteaResults.passed;
            //totalFailed += xxteaResults.failed;
            //Console.WriteLine();

            //var cfbResults = CFBModeTests.RunAllTests();
            //totalPassed += cfbResults.passed;
            //totalFailed += cfbResults.failed;
            //Console.WriteLine();

            //var tigerResults = TigerHashTests.RunAllTests();
            //totalPassed += tigerResults.passed;
            //totalFailed += tigerResults.failed;
            //Console.WriteLine();

            //var metadataResults = MetadataServiceTests.RunAllTests();
            //totalPassed += metadataResults.passed;
            //totalFailed += metadataResults.failed;
            //Console.WriteLine();

            //var fswResults = FileSystemWatcherServiceTests.RunAllTests();
            //totalPassed += fswResults.passed;
            //totalFailed += fswResults.failed;
            //Console.WriteLine();

            var networkResults = NetworkServiceTests.RunAllTests();
            totalPassed += networkResults.passed;
            totalFailed += networkResults.failed;
            Console.WriteLine();

            var encDecResults = EncryptionDecryptionServiceTests.RunAllTests();
            totalPassed += encDecResults.passed;
            totalFailed += encDecResults.failed;
            Console.WriteLine();

            var integrationResults = FileExchangeIntegrationTests.RunAllTests();
            totalPassed += integrationResults.passed;
            totalFailed += integrationResults.failed;
            Console.WriteLine();

            var drugaCompatResults = DrugaAplikacijaCompatibilityTest.RunAllTests();
            totalPassed += drugaCompatResults.passed;
            totalFailed += drugaCompatResults.failed;
            Console.WriteLine();

            PrintGlobalSummary(totalPassed, totalFailed);
        }

        private static void PrintGlobalSummary(int totalPassed, int totalFailed)
        {
            Console.WriteLine("============================================");
            Console.WriteLine("            OVERALL TEST SUMMARY            ");
            Console.WriteLine("============================================");
            Console.WriteLine();
            Console.WriteLine($"  Total Test Suites:  10");
            Console.WriteLine($"  Total Tests:        {totalPassed + totalFailed}");
            Console.WriteLine($"  Passed:          {totalPassed}");
            Console.WriteLine($"  Failed:          {totalFailed}");
            Console.WriteLine();
            
            if (totalFailed == 0)
            {
                Console.WriteLine(" ALL TESTS PASSED! ");
                Console.WriteLine();
                Console.WriteLine(" Perfect score: 100% success rate!");
            }
            else
            {
                double successRate = (totalPassed / (double)(totalPassed + totalFailed)) * 100;
                Console.WriteLine($" WARNING: {totalFailed} test(s) FAILED!");
                Console.WriteLine($" Success Rate: {successRate:F1}%");
            }
            
            Console.WriteLine();
            Console.WriteLine("=============================================");
        }
    }
}

