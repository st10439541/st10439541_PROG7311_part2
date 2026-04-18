namespace St10439541_PROG7311_P2.Services
{
    public interface ICurrencyExchangeService
    {
        Task<decimal> GetUsdToZarRateAsync();
    }
}
