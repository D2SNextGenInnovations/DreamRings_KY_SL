using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Data.Entity.Infrastructure;

namespace MrGroom_KY_SL.Data.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        //public IQueryable<T> GetAll() => _dbSet.AsNoTracking();
        public IQueryable<T> GetAll() => _dbSet;

        public virtual IEnumerable<T> GetAll(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = "")
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
                query = query.Where(filter);

            foreach (var includeProperty in includeProperties
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty.Trim());
            }

            if (orderBy != null)
                return orderBy(query).ToList();
            else
                return query.ToList();
        }

        public IQueryable<T> GetAllQuery() => _dbSet;

        /// Basic GetById (no includes)
        public virtual T GetById(object id)
        {
            return _dbSet.Find(id);
        }

        ///  Extended GetById supporting includeProperties for eager loading.
        public virtual T GetById(object id, string includeProperties = "")
        {
            if (string.IsNullOrWhiteSpace(includeProperties))
                return _dbSet.Find(id);

            IQueryable<T> query = _dbSet;

            foreach (var includeProperty in includeProperties
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty.Trim());
            }

            // Try to find the key property (Id or EntityNameId)
            var keyProperty = typeof(T).GetProperties()
                .FirstOrDefault(p =>
                    p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.Equals(typeof(T).Name + "Id", StringComparison.OrdinalIgnoreCase));

            if (keyProperty == null)
                throw new InvalidOperationException($"No key property found for entity {typeof(T).Name}");

            // Build x => x.Id == id expression dynamically
            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.Property(parameter, keyProperty);
            var equality = Expression.Equal(propertyAccess, Expression.Constant(id));
            var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);

            return query.FirstOrDefault(lambda);
        }

        public T GetFirstOrDefault(Expression<Func<T, bool>> predicate)
            => _dbSet.AsNoTracking().FirstOrDefault(predicate);

        public void Insert(T entity) => _dbSet.Add(entity);

        public void Update(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var entry = _context.Entry(entity);

            if (entry.State == EntityState.Detached)
            {
                var objectContext = ((IObjectContextAdapter)_context).ObjectContext;
                var objectSet = objectContext.CreateObjectSet<T>();
                var keyName = objectSet.EntitySet.ElementType.KeyMembers.First().Name;

                var key = entity.GetType().GetProperty(keyName)?.GetValue(entity, null);
                var existing = _dbSet.Find(key);

                if (existing != null)
                {
                    _context.Entry(existing).CurrentValues.SetValues(entity);
                    return;
                }

                _dbSet.Attach(entity);
                entry.State = EntityState.Modified;
            }
        }

        public virtual IQueryable<T> Get(
           Expression<Func<T, bool>> filter = null,
           Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
           string includeProperties = "")
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
                query = query.Where(filter);

            foreach (var includeProperty in includeProperties
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty.Trim());
            }

            return orderBy != null ? orderBy(query) : query;
        }

        public void Delete(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (_context.Entry(entity).State == EntityState.Detached)
                _dbSet.Attach(entity);

            _dbSet.Remove(entity);
        }
    }
}
