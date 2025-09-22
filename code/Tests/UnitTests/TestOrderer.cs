using Xunit.Abstractions;
using Xunit.Sdk;
[assembly: TestCaseOrderer("Tests.UnitTests.RandomTestCaseOrderer", "Tests")]
namespace Tests.UnitTests;
public class RandomTestCaseOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        var random = new Random(DateTime.Now.Millisecond);
        return testCases.OrderBy(x => random.Next());
    }
}
