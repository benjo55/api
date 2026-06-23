using api.Services;
using Xunit;

namespace api.Tests;

public class FeePrecisionTests
{
    [Fact]
    public void DailyAccrual_KeepsSubCentPrecisionUntilPosting()
    {
        const decimal dailyFee = 0.0000046m;

        Assert.Equal(0.0000046m, OperationEngineService.NumericPolicy.RoundAmount(dailyFee));
        Assert.Equal(0m, OperationEngineService.NumericPolicy.RoundMoney(dailyFee));
    }
}
