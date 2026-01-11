using MrGroom_KY_SL.Common;
using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Business.Services
{
    public class AccountService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public User ValidateLogin(string username, string password)
        {
            try
            {
                var user = _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Username == username);

                if (user != null && PasswordHelper.VerifyPassword(password, user.Password))
                    return user;

                return null;
            }
            catch (ArgumentNullException ex)
            {
                // Handle null arguments gracefully
                throw new Exception("Invalid input: Username or password cannot be null.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception("An unexpected error occurred while validating login. Please try again.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while validating login credentials.", ex);
            }
        }

        public void Register(User user)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user), "User details cannot be null.");

                _unitOfWork.UserRepository.Insert(user);
                _unitOfWork.Save();
            }
            catch (ArgumentNullException ex)
            {
                throw new Exception("User registration failed: Missing required details.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception("An unexpected error occurred while saving user details.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while registering the user. Please try again.", ex);
            }
        }
    }
}
