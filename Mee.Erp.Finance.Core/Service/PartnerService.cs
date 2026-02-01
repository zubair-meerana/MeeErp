using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

public class PartnerService : IPartnerService
{
	private readonly FinanceDbContext _context;

	public PartnerService(FinanceDbContext context)
	{
		_context = context;
	}

	// --- Supplier Logic ---
	public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
	{
		// Basic duplicate check
		bool exists = await _context.Suppliers.AnyAsync(s => s.CompanyId == supplier.CompanyId && s.Name == supplier.Name);
		if (exists) throw new Exception("Supplier already exists.");

		_context.Suppliers.Add(supplier);
		await _context.SaveChangesAsync();
		return supplier;
	}

	public async Task<Supplier> UpdateSupplierAsync(Supplier supplier)
	{
		var existing = await _context.Suppliers.FindAsync(supplier.Id);
		if (existing == null) throw new Exception("Supplier not found");

		existing.Name = supplier.Name;
		existing.ContactEmail = supplier.ContactEmail;
		existing.ContactName = supplier.ContactName;
		existing.DefaultAccountsPayableAccountId = supplier.DefaultAccountsPayableAccountId;
		// existing.PaymentTermId = supplier.PaymentTermId; // If you added this

		await _context.SaveChangesAsync();
		return existing;
	}

	public async Task<Supplier?> GetSupplierByIdAsync(Guid id) => await _context.Suppliers.FindAsync(id);

	public async Task<List<Supplier>> GetAllSuppliersAsync(Guid companyId) =>
		await _context.Suppliers.Where(s => s.CompanyId == companyId).ToListAsync();

	// --- Customer Logic (Mirrors Supplier) ---
	public async Task<Customer> CreateCustomerAsync(Customer customer)
	{
		_context.Customers.Add(customer);
		await _context.SaveChangesAsync();
		return customer;
	}

	public async Task<Customer> UpdateCustomerAsync(Customer customer)
	{
		var existing = await _context.Customers.FindAsync(customer.Id);
		if (existing == null) throw new Exception("Customer not found");

		existing.Name = customer.Name;
		existing.ContactEmail = customer.ContactEmail;
		existing.DefaultAccountsReceivableAccountId = customer.DefaultAccountsReceivableAccountId;

		await _context.SaveChangesAsync();
		return existing;
	}

	public async Task<Customer?> GetCustomerByIdAsync(Guid id) => await _context.Customers.FindAsync(id);

	public async Task<List<Customer>> GetAllCustomersAsync(Guid companyId) =>
		await _context.Customers.Where(c => c.CompanyId == companyId).ToListAsync();
}