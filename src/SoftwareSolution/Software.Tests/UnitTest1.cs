namespace Software.Tests;

public class UnitTest1
{
    [Fact]
    public void FivePlusSixteenIs21()
    {
        int a = 5, b = 16, answer;

        answer = a + b;

        Assert.Equal(21, answer);
    }

    [Theory]
    [InlineData(5,16, 21)]
    [InlineData(5,5,10)]
    [InlineData(2,2,4)]
    public void AddSomeIntegers(int a, int b, int expected)
    {
        var answer = a + b;
        Assert.Equal(expected, answer);
       
    }
}
