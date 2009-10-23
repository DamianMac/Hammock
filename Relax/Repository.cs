using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Relax.Design;

namespace Relax
{
    public interface IRepository<TEntity> where TEntity : class
    {
        TEntity Get(string id);
        Document Save(TEntity entity);
        void Delete(TEntity entity);
    }

    public partial class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        public Session Session { get; private set; }
        public DesignDocument Design { get; private set; }

        public IDictionary<string, Query<TEntity>> Queries { get; private set; }

        public Repository(Session sx) 
        {
            __init(sx, null);
        }

        public Repository(Session sx, DesignDocument design)
        {
            __init(sx, design);
        }

        private void __init(Session sx, DesignDocument design)
        {
            Queries = new Dictionary<string, Query<TEntity>>();
            Session = sx;

            Design = design;
            if (null == Design)
            {
                try
                {
                    Design = Session.Load<DesignDocument>(GetDesignDocumentName());
                }
                catch 
                {
                    // its not a fault to not have a design document
                }
            }

            if (null != design)
            {
                foreach (var v in design.Views)
                {
                    Queries.Add(
                        v.Key,
                        new Query<TEntity>(
                            Session,
                            typeof(TEntity).Name.ToLowerInvariant(),
                            v.Key,
                            !String.IsNullOrEmpty(v.Value.Reduce)  
                    ));
                }
            }
        }

        private string GetDesignDocumentName()
        {
            return "_design/" + typeof (TEntity).Name.ToLowerInvariant();
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
