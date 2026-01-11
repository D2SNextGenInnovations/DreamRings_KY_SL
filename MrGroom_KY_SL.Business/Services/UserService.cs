using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Business.Services
{
    public class UserService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public IEnumerable<User> GetAll() => _unitOfWork.UserRepository.GetAll();

        public User GetById(int id) => _unitOfWork.UserRepository.GetById(id);

        public User GetByUsername(string username) =>
            _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Username == username);

        public void Create(User user)
        {
            _unitOfWork.UserRepository.Insert(user);
            _unitOfWork.Save();
        }

        public void Update(User user)
        {
            _unitOfWork.UserRepository.Update(user);
            _unitOfWork.Save();
        }

        public void Delete(int id)
        {
            var u = _unitOfWork.UserRepository.GetById(id);
            if (u != null)
            {
                _unitOfWork.UserRepository.Delete(u);
                _unitOfWork.Save();
            }
        }
    }
}
