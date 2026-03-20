using AccountContentService.Api.Contracts.Requests;
using AccountContentService.Application.Features.Payments.Commands.CreatePayment;
using AutoMapper;

namespace AccountContentService.Api.Mappings
{
    public class PaymentMappingProfile : Profile
    {
        public PaymentMappingProfile()
        {
            CreateMap<PaymentRequest, CreatePaymentCommand>();
        }
    }
}
