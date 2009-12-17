using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using RedBranch.Hammock.Design;

namespace RedBranch.Hammock
{
    public interface IRepository<TEntity> where TEntity : class
    {
        TEntity Get(string id);
        Document Save(TEntity entity);
        void Delete(TEntity entity);
        IPrimayOperator<TEntity, TKey> Where<TKey>(Expression<Func<TEntity, TKey>> xp);
        Query<TEntity>.Spec All();
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
                    Design = new DesignDocument();
                    Design.Language = "javascript";
                    Design.Views = new Dictionary<string, View>();
                    Session.Save(Design, GetDesignDocumentName());
                }
            }

            foreach (var v in Design.Views)
            {
                CreateQuery(v.Key, v.Value);
            }
        }

        private static string GetDesignDocumentName()
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
