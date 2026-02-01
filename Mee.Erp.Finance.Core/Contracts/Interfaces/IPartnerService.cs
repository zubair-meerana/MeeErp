using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

public interface IPartnerService
{
	// Suppliers
	Task<Supplier> CreateSupplierAsync(Supplier supplier);
	Task<Supplier> UpdateSupplierAsync(Supplier supplier);
	Task<Supplier?> GetSupplierByIdAsync(Guid id);
	Task<List<Supplier>> GetAllSuppliersAsync(Guid companyId);

	// Customers
	Task<Customer> CreateCustomerAsync(Customer customer);
	Task<Customer> UpdateCustomerAsync(Customer customer);
	Task<Customer?> GetCustomerByIdAsync(Guid id);
	Task<List<Customer>> GetAllCustomersAsync(Guid companyId);
}