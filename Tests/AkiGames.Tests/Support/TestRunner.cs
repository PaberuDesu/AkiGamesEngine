namespace AkiGames.Tests.Support;

internal sealed record TestCase(string Name, Action Body);

internal static class TestRunner
{
    public static int Run(IEnumerable<TestCase> tests)
    {
        int passed = 0;
        int failed = 0;

        foreach (TestCase test in tests)
        {
            try
            {
                test.Body();
                passed++;
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine($"FAIL {test.Name}");
                Console.WriteLine($"     {ex.GetType().Name}: {ex.Message}");
                if (ex.StackTrace != null)
                    Console.WriteLine(ex.StackTrace);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Result: {passed} passed, {failed} failed");
        return failed;
    }
}
