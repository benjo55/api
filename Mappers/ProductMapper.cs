using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Product;
using api.Models;
using api.Models.Enum;
using Product = api.Models.Product;

namespace api.Mappers
{
    public static class ProductMapper
    {
        public static ProductDto ToProductDto(this Product productModel)
        {
            return new ProductDto
            {
                Id = productModel.Id,
                ProductCode = productModel.ProductCode,
                ProductName = productModel.ProductName,
                InsurerId = productModel.InsurerId,
                CreatedDate = productModel.CreatedDate,
                UpdatedDate = productModel.UpdatedDate,
                ContractFamily = productModel.ContractFamily,
                ContractFamilyLabel = productModel.ContractFamily.HasValue
                    ? productModel.ContractFamily.Value.ToLabel()
                    : null,
                ProductTypeId = productModel.ProductTypeId,
                TaxProfileId = productModel.TaxProfileId,
                DefaultManagementFeeRate = productModel.DefaultManagementFeeRate,
                DefaultManagementFeeFrequency = Enum.TryParse<ManagementFeeFrequency>(productModel.DefaultManagementFeeFrequency, out var frequency)
                    ? frequency
                    : null,
                DefaultManagementFeeProrataMethod = Enum.TryParse<ManagementFeeProrataMethod>(productModel.DefaultManagementFeeProrataMethod, out var prorataMethod)
                    ? prorataMethod
                    : null,
                DefaultManagementFeePostingMode = Enum.TryParse<ManagementFeePostingMode>(productModel.DefaultManagementFeePostingMode, out var postingMode)
                    ? postingMode
                    : null,
                DefaultManagementFeeEffectiveDate = productModel.DefaultManagementFeeEffectiveDate,
                DefaultManagementFeeEndDate = productModel.DefaultManagementFeeEndDate,
                DefaultManagementFeeIsEnabled = productModel.DefaultManagementFeeIsEnabled,
            };
        }
        public static Product ToProductFromCreateDto(this CreateProductRequestDto productDto)
        {
            return new Product
            {
                ProductCode = productDto.ProductCode,
                ProductName = productDto.ProductName,
                InsurerId = productDto.InsurerId,
                CreatedDate = productDto.CreatedDate,
                UpdatedDate = productDto.UpdatedDate,
                ContractFamily = productDto.ContractFamily,
                ProductTypeId = productDto.ProductTypeId,
                TaxProfileId = productDto.TaxProfileId,
                DefaultManagementFeeRate = productDto.DefaultManagementFeeRate,
                DefaultManagementFeeFrequency = productDto.DefaultManagementFeeFrequency?.ToString(),
                DefaultManagementFeeProrataMethod = productDto.DefaultManagementFeeProrataMethod?.ToString(),
                DefaultManagementFeePostingMode = productDto.DefaultManagementFeePostingMode?.ToString(),
                DefaultManagementFeeEffectiveDate = productDto.DefaultManagementFeeEffectiveDate,
                DefaultManagementFeeEndDate = productDto.DefaultManagementFeeEndDate,
                DefaultManagementFeeIsEnabled = productDto.DefaultManagementFeeIsEnabled,
            };
        }
        public static Product ToProductFromUpdateDto(this UpdateProductRequestDto productDto)
        {
            return new Product
            {
                ProductCode = productDto.ProductCode,
                ProductName = productDto.ProductName,
                InsurerId = productDto.InsurerId,
                CreatedDate = productDto.CreatedDate,
                UpdatedDate = productDto.UpdatedDate,
                ContractFamily = productDto.ContractFamily,
                ProductTypeId = productDto.ProductTypeId,
                TaxProfileId = productDto.TaxProfileId,
                DefaultManagementFeeRate = productDto.DefaultManagementFeeRate,
                DefaultManagementFeeFrequency = productDto.DefaultManagementFeeFrequency?.ToString(),
                DefaultManagementFeeProrataMethod = productDto.DefaultManagementFeeProrataMethod?.ToString(),
                DefaultManagementFeePostingMode = productDto.DefaultManagementFeePostingMode?.ToString(),
                DefaultManagementFeeEffectiveDate = productDto.DefaultManagementFeeEffectiveDate,
                DefaultManagementFeeEndDate = productDto.DefaultManagementFeeEndDate,
                DefaultManagementFeeIsEnabled = productDto.DefaultManagementFeeIsEnabled,
            };
        }
        public static UpdateProductRequestDto ToUpdateProductRequestDto(this Product product)
        {
            return new UpdateProductRequestDto
            {
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                InsurerId = product.InsurerId ?? 0,
                CreatedDate = product.CreatedDate,
                UpdatedDate = product.UpdatedDate,
                ContractFamily = product.ContractFamily,
                ProductTypeId = product.ProductTypeId,
                TaxProfileId = product.TaxProfileId,
                DefaultManagementFeeRate = product.DefaultManagementFeeRate,
                DefaultManagementFeeFrequency = Enum.TryParse<ManagementFeeFrequency>(product.DefaultManagementFeeFrequency, out var frequency)
                    ? frequency
                    : null,
                DefaultManagementFeeProrataMethod = Enum.TryParse<ManagementFeeProrataMethod>(product.DefaultManagementFeeProrataMethod, out var prorataMethod)
                    ? prorataMethod
                    : null,
                DefaultManagementFeePostingMode = Enum.TryParse<ManagementFeePostingMode>(product.DefaultManagementFeePostingMode, out var postingMode)
                    ? postingMode
                    : null,
                DefaultManagementFeeEffectiveDate = product.DefaultManagementFeeEffectiveDate,
                DefaultManagementFeeEndDate = product.DefaultManagementFeeEndDate,
                DefaultManagementFeeIsEnabled = product.DefaultManagementFeeIsEnabled,
            };
        }
    }
}