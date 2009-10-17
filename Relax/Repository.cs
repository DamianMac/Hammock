using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Relax
{
    public interface IRepository<TEntity> where TEntity : class
    {
        TEntity Get(string id);
        Document Save(TEntity entity);
        void Delete(TEntity entity);
    }

    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        public Session Session { get; private set; }

        public Repository(Session sx)
        {
            Session = sx;
        }

        public TEntity Get(string id)
        {
            return Session.Load<TEntity>(id);
        }

        public Document Save(TEntity entity)
        {
            return Session.Save(entity);
        }

        public void Delete(TEntity entity)
        {
            Session.Delete(entity);
        }
    }
}
