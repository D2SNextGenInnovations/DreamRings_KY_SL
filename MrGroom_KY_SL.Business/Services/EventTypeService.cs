using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Business.Services
{
    public class EventTypeService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public IEnumerable<EventType> GetAll()
        {
            return _unitOfWork.EventTypeRepository.GetAll().Where(x => x.IsActive == true); //V3001
        }

        public EventType GetById(int id)
        {
            return _unitOfWork.EventTypeRepository.GetById(id);
        }

        public void Create(EventType eventType)
        {
            _unitOfWork.EventTypeRepository.Insert(eventType);
            _unitOfWork.Save();
        }

        public void Add(EventType entity)
        {
            _unitOfWork.EventTypeRepository.Insert(entity);
            _unitOfWork.Save();
        }

        public void Update(EventType eventType)
        {
            _unitOfWork.EventTypeRepository.Update(eventType);
            _unitOfWork.Save();
        }

        public void Delete(int id)
        {
            //V3001
            var entity = _unitOfWork.EventTypeRepository.GetById(id);

            if (entity == null)
                throw new KeyNotFoundException("EventType not found");

            entity.IsActive = false; 
            _unitOfWork.EventTypeRepository.Update(entity);
            _unitOfWork.Save();
            //V3001
        }
    }
}
