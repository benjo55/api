using api.Dtos.TaxReceipts;
using api.Interfaces;
using Quartz;

namespace api.Services.Jobs
{
    public sealed class SendTaxReceiptEmailJob : IJob
    {
        public const string TaxReceiptIdKey = "taxReceiptId";
        public const string RecipientEmailKey = "recipientEmail";
        public const string SubjectKey = "subject";
        public const string BodyKey = "body";

        private readonly ITaxReceiptEmailService _emailService;

        public SendTaxReceiptEmailJob(ITaxReceiptEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var map = context.MergedJobDataMap;
            var taxReceiptId = map.GetInt(TaxReceiptIdKey);
            await _emailService.SendAsync(
                taxReceiptId,
                new SendTaxReceiptEmailDto(
                    map.GetString(RecipientEmailKey),
                    map.GetString(SubjectKey),
                    map.GetString(BodyKey)),
                context.FireInstanceId,
                context.CancellationToken);
        }
    }
}
