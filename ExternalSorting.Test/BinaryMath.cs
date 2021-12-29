using NUnit.Framework;

namespace ExternalSorting.Test;

public class BinaryMath
{
    [TestCase(512, 512)]
    [TestCase(513, 512)]
    [TestCase(1023, 512)]
    [TestCase(1024, 1024)]
    public void ToNearestPow2(int n, int excepted)
    {
        var actual = Domain.BinaryMath.ToNearestPow2(n);
        Assert.AreEqual(excepted, actual);
    }
}