using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace MrGroom_KY_SL.Business.Services
{
    public class CustomerService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public IEnumerable<Customer> GetAll()
        {
            return _unitOfWork.CustomerRepository.GetAllQuery().Include("Addresses").AsNoTracking();
        }

        public Customer GetById(int id)
        {
            return _unitOfWork.CustomerRepository
                .GetAllQuery()
                .Include("Addresses")  
                .FirstOrDefault(c => c.CustomerId == id); 
        }

        public void Add(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            _unitOfWork.CustomerRepository.Insert(customer);
            _unitOfWork.Save();
        }

        public void Update(Customer customer)
        {
            var existing = _unitOfWork.CustomerRepository
                .GetAllQuery()
                .Include("Addresses")
                .FirstOrDefault(c => c.CustomerId == customer.CustomerId);

            if (existing == null)
                throw new KeyNotFoundException($"Customer with ID {customer.CustomerId} not found.");

            // Update main fields
            existing.FirstName = customer.FirstName;
            existing.LastName = customer.LastName;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;
            existing.NICNumber = customer.NICNumber;

            // Handle address
            var newAddress = customer.Addresses?.FirstOrDefault();
            if (newAddress != null)
            {
                var existingAddress = existing.Addresses?.FirstOrDefault(a => a.IsPrimary)
                                      ?? existing.Addresses?.FirstOrDefault();

                if (existingAddress != null)
                {
                    // Update existing entity, don't replace
                    existingAddress.AddressLine1 = newAddress.AddressLine1;
                    existingAddress.AddressLine2 = newAddress.AddressLine2;
                    existingAddress.AddressLine3 = newAddress.AddressLine3;
                    existingAddress.City = newAddress.City;
                    existingAddress.StateOrProvince = newAddress.StateOrProvince;
                    existingAddress.PostalCode = newAddress.PostalCode;
                    existingAddress.Country = newAddress.Country;
                    existingAddress.AddressType = newAddress.AddressType;
                    existingAddress.IsPrimary = true;
                    existingAddress.UpdatedDate = DateTime.Now;
                }
                else
                {
                    // Add new only if none exists
                    newAddress.CustomerId = existing.CustomerId;
                    newAddress.IsPrimary = true;
                    newAddress.CreatedDate = DateTime.Now;
                    existing.Addresses.Add(newAddress);
                }
            }

            _unitOfWork.Save();
        }

        public void Delete(int id)
        {
            var entity = _unitOfWork.CustomerRepository.GetById(id);
            if (entity == null)
                throw new KeyNotFoundException($"Customer with ID {id} not found.");

            _unitOfWork.CustomerRepository.Delete(entity);
            _unitOfWork.Save();
        }
    }
}
