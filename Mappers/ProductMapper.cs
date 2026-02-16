using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Product;
using api.Models;
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
                UpdatedDate = productModel.UpdatedDate
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
                UpdatedDate = productDto.UpdatedDate
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

            };
        }
    }
}